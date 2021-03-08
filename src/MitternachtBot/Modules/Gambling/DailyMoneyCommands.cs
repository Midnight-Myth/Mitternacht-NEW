using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Database;
using Mitternacht.Services.Impl;
using Mitternacht.Modules.Administration.Services;

namespace Mitternacht.Modules.Gambling {
	public partial class Gambling {
		[Group]
		public class DailyMoneyCommands : MitternachtSubmodule {
			private readonly IBotConfigProvider _bc;
			private readonly IUnitOfWork uow;
			private readonly CurrencyService _currency;
			private readonly GuildTimezoneService _gts;

			private string CurrencyName => _bc.BotConfig.CurrencyName;
			private string CurrencySign => _bc.BotConfig.CurrencySign;

			public DailyMoneyCommands(IBotConfigProvider bc, IUnitOfWork uow, CurrencyService currency, GuildTimezoneService gts) {
				_bc = bc;
				this.uow = uow;
				_currency = currency;
				_gts = gts;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task DailyMoney() {
				var guildUser = (IGuildUser)Context.User;

				var canReceiveDailyMoney = uow.DailyMoney.CanReceive(guildUser.GuildId, guildUser.Id, _gts.GetTimeZoneOrUtc(Context.Guild.Id));

				if(canReceiveDailyMoney) {
					var userRolesAll = guildUser.GetRoles().OrderBy(r => -r.Position);
					var roleMoneysAll = uow.RoleMoney.GetAll();
					var userRoles = userRolesAll.OrderBy(r => -r.Position).AsEnumerable().Where(r => roleMoneysAll.FirstOrDefault(m => m.RoleId == r.Id) != null).ToList();
					var roleMoneys = roleMoneysAll.OrderByDescending(m => m.Priority).AsEnumerable().Where(m => userRolesAll.FirstOrDefault(r => r.Id == m.RoleId) != null).ToList();

					if(!roleMoneys.Any())
						await ReplyLocalized("dm_no_role").ConfigureAwait(false);
					else {
						var rm = roleMoneys
								.Where(m => m.Priority == roleMoneys.First().Priority)
								.OrderBy(m => -userRoles.First(r => r.Id == m.RoleId).Position)
								.First();
						var role = userRoles.First(r => r.Id == rm.RoleId);
						var time = uow.DailyMoney.UpdateState(guildUser.GuildId, guildUser.Id);
						await _currency.AddAsync(guildUser, $"Daily Reward ({role.Name})", rm.Money, uow).ConfigureAwait(false);
						uow.DailyMoneyStats.Add(guildUser.GuildId, guildUser.Id, time, rm.Money);

						await uow.SaveChangesAsync(false).ConfigureAwait(false);

						await ReplyLocalized("dm_received", role.Name, rm.Money, CurrencySign).ConfigureAwait(false);
					}
				} else
					await ReplyLocalized("dm_already_received").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOrGuildPermission(GuildPermission.Administrator)]
			public async Task SetRoleMoney(IRole role, long money, int priority) {
				uow.RoleMoney.SetMoney(Context.Guild.Id, role.Id, money, priority);
				await uow.SaveChangesAsync(false).ConfigureAwait(false);
				
				await MessageLocalized("dm_role_set", role.Name, money, CurrencySign, priority).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOrGuildPermission(GuildPermission.Administrator)]
			public async Task SetRoleMoney(IRole role, long money)
				=> await SetRoleMoney(role, money, 0).ConfigureAwait(false);

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOrGuildPermission(GuildPermission.Administrator)]
			public async Task ResetDailyMoney([Remainder] IGuildUser user = null) {
				user ??= (IGuildUser)Context.User;
				uow.DailyMoney.ResetLastTimeReceived(Context.Guild.Id, user.Id, _gts.GetTimeZoneOrUtc(Context.Guild.Id));

				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				await MessageLocalized("dm_again", user.ToString()).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOrGuildPermission(GuildPermission.Administrator)]
			public async Task ResetDailyMoneyGuild() {
				uow.DailyMoney.ResetLastTimeReceivedForGuild(Context.Guild.Id, _gts.GetTimeZoneOrUtc(Context.Guild.Id));

				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				await ConfirmLocalized("dailymoney_resetdailymoneyguild_success").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOrGuildPermission(GuildPermission.Administrator)]
			public async Task RemoveRoleMoney(IRole role) {
				var removed = uow.RoleMoney.Remove(Context.Guild.Id, role.Id);
				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				await MessageLocalized(removed ? "dm_role_removed" : "dm_role_not_set", role.Name).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOrGuildPermission(GuildPermission.Administrator)]
			public async Task SetRoleMoneyPriority(IRole role, int priority) {
				var exists = uow.RoleMoney.MoneyForRoleIsDefined(Context.Guild.Id, role.Id);

				if(exists) {
					uow.RoleMoney.SetMoney(Context.Guild.Id, role.Id, null, priority);
				}
				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				await MessageLocalized(exists ? "dm_role_priority_set" : "dm_role_not_set", role.Name, priority).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task Payroll(int count = 20, int position = 1) {
				const int elementsPerList = 20;
				
				var roleMoneys = uow.RoleMoney
					.GetAll()
					.Where(rm => rm.GuildId == Context.Guild.Id)
					.ToList()
					.OrderByDescending(rm => rm.Priority)
					.ThenByDescending(rm => Context.Guild.GetRole(rm.RoleId)?.Position ?? 0)
					.Skip(position - 1 <= 0 ? 0 : position - 1)
					.Take(count)
					.ToList();

				if(!roleMoneys.Any())
					return;

				var groupedRms = roleMoneys
					.GroupBy(rm => (int) Math.Floor(roleMoneys.IndexOf(rm) * 1d / elementsPerList))
					.ToList();

				var listStrings = new List<string>();
				var sb = new StringBuilder();
				var listheader = GetText("payroll_list_header", CurrencyName);
				var headerdivider = string.Join("|", listheader.Split("|").Select(s => string.Join("", Enumerable.Repeat("-", s.Length))));
				sb.AppendLine(GetText("payroll_header"));

				foreach(var grm in groupedRms) {
					if(!grm.Any())
						continue;
					var listNumber = grm.Key + 1;
					sb.Append($"```{GetText("payroll_list_number", listNumber)}\n{listheader}\n{headerdivider}");
					foreach(var rm in grm) {
						var role = Context.Guild.GetRole(rm.RoleId);
						sb.Append($"\n{position + roleMoneys.IndexOf(rm),3}. | {role?.Name ?? rm.RoleId.ToString(),-20} | {rm.Money,-4}{string.Join("", Enumerable.Repeat(" ", CurrencyName.Length - 4))} | {rm.Priority}");
					}

					sb.Append("```");
					listStrings.Add(sb.ToString());
					sb.Clear();
				}

				var channel = count <= 20
					? Context.Channel
					: await Context.User.GetOrCreateDMChannelAsync().ConfigureAwait(false);

				foreach(var s in listStrings) {
					await channel.SendMessageAsync(s).ConfigureAwait(false);
					Thread.Sleep(250);
				}
			}
		}
	}
}
