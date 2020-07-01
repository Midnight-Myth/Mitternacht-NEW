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
using Mitternacht.Services.Database.Models;
using Newtonsoft.Json;

namespace Mitternacht.Modules.Gambling {
	public partial class Gambling {
		[Group]
		public class DailyMoneyCommands : MitternachtSubmodule {
			private readonly IBotConfigProvider _bc;
			private readonly DbService _db;
			private readonly CurrencyService _currency;

			private string CurrencyName => _bc.BotConfig.CurrencyName;
			private string CurrencySign => _bc.BotConfig.CurrencySign;

			public DailyMoneyCommands(IBotConfigProvider bc, DbService db, CurrencyService currency) {
				_bc = bc;
				_db = db;
				_currency = currency;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task DailyMoney() {
				var user = (IGuildUser)Context.User;

				using(var uow = _db.UnitOfWork) {
					var canReceiveDailyMoney = uow.DailyMoney.CanReceive(user.Id);

					if(canReceiveDailyMoney) {
						var userRolesAll = user.GetRoles().OrderBy(r => -r.Position).ToList();
						var roleMoneysAll = uow.RoleMoney.GetAll();
						var userRoles = userRolesAll.Where(r => roleMoneysAll.FirstOrDefault(m => m.RoleId == r.Id) != null).OrderBy(r => -r.Position);
						var roleMoneys = roleMoneysAll.Where(m => userRolesAll.FirstOrDefault(r => r.Id == m.RoleId) != null).OrderByDescending(m => m.Priority).ToList();

						if(!roleMoneys.Any())
							await ReplyLocalized("dm_no_role").ConfigureAwait(false);
						else {
							var rm = roleMoneys
								.Where(m => m.Priority == roleMoneys.First().Priority)
								.OrderBy(m => -userRoles.First(r => r.Id == m.RoleId).Position)
								.First();
							var role = userRoles.First(r => r.Id == rm.RoleId);
							uow.DailyMoney.TryUpdateState(user.Id);
							await _currency.AddAsync(user, $"Daily Reward ({role.Name})", rm.Money, false).ConfigureAwait(false);
							uow.DailyMoneyStats.Add(user.Id, uow.DailyMoney.GetUserDate(user.Id), rm.Money);

							await uow.CompleteAsync().ConfigureAwait(false);

							await ReplyLocalized("dm_received", role.Name, rm.Money, CurrencySign).ConfigureAwait(false);
						}
					} else
						await ReplyLocalized("dm_already_received").ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOnly]
			public async Task SetRoleMoney(IRole role, long money, int priority) {
				using(var uow = _db.UnitOfWork) {
					uow.RoleMoney.SetMoney(role.Id, money);
					uow.RoleMoney.SetPriority(role.Id, priority);
					await uow.CompleteAsync().ConfigureAwait(false);
				}
				await MessageLocalized("dm_role_set", role.Name, money, CurrencySign, priority).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOnly]
			public async Task SetRoleMoney(IRole role, long money)
				=> await SetRoleMoney(role, money, 0).ConfigureAwait(false);

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOnly]
			public async Task ResetDailyMoney([Remainder] IGuildUser user = null) {
				user = user ?? (IGuildUser)Context.User;
				bool wasReset;
				using(var uow = _db.UnitOfWork) {
					wasReset = uow.DailyMoney.TryResetReceived(user.Id);
					await uow.CompleteAsync().ConfigureAwait(false);
				}
				await MessageLocalized(wasReset ? "dm_again" : "dm_not_received", user.ToString()).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOnly]
			public async Task RemoveRoleMoney(IRole role) {
				bool removed;
				using(var uow = _db.UnitOfWork) {
					removed = uow.RoleMoney.Remove(role.Id);
					await uow.CompleteAsync().ConfigureAwait(false);
				}
				await MessageLocalized(removed ? "dm_role_removed" : "dm_role_not_set", role.Name).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOnly]
			public async Task SetRoleMoneyPriority(IRole role, int priority) {
				bool exists;
				using(var uow = _db.UnitOfWork) {
					exists = uow.RoleMoney.Exists(role.Id);
					if(exists)
						uow.RoleMoney.SetPriority(role.Id, priority);
					await uow.CompleteAsync().ConfigureAwait(false);
				}
				await MessageLocalized(exists ? "dm_role_priority_set" : "dm_role_not_set", role.Name, priority).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task Payroll(int count = 20, int position = 1) {
				const int elementsPerList = 20;

				List<RoleMoney> roleMoneys;
				using(var uow = _db.UnitOfWork) {
					roleMoneys = uow.RoleMoney
						.GetAll()
						.OrderByDescending(rm => rm.Priority)
						.ThenByDescending(rm => Context.Guild.GetRole(rm.RoleId)?.Position ?? 0)
						.Skip(position - 1 <= 0 ? 0 : position - 1)
						.Take(count)
						.ToList();
					await uow.CompleteAsync().ConfigureAwait(false);
				}

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

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[Priority(0)]
			public async Task DailyMoneyStats() {
				if(Context.User is IGuildUser user) {
					await DailyMoneyStats(user).ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOrGuildPermission(GuildPermission.Administrator)]
			[Priority(1)]
			public async Task DailyMoneyStats(params IGuildUser[] users) {
				users = users.Length == 0 ? new[] { (IGuildUser)Context.User } : users;

				using(var uow = _db.UnitOfWork) {
					var stats = uow.DailyMoneyStats
						.GetAllUser(users.Select(gu => gu.Id).ToArray())
						.GroupBy(dms => dms.UserId)
						.ToDictionary(g => g.Key, g => g.Select(dms => new {date = dms.TimeReceived.ToUnixTimestamp(), money = dms.MoneyReceived}).ToArray());
					await Context.User.SendFileAsync(await JsonConvert.SerializeObject(stats).ToStream().ConfigureAwait(false), $"{DateTime.Now:yyyy-MM-dd_hh-mm-ss}_dailymoney-stats.json").ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOrGuildPermission(GuildPermission.Administrator)]
			public async Task DailyMoneyStatsAll() {
				using(var uow = _db.UnitOfWork) {
					var stats = uow.DailyMoneyStats
						.GetAll()
						.GroupBy(dms => dms.UserId)
						.ToDictionary(g => g.Key, g => g.Select(dms => new {date = dms.TimeReceived.ToUnixTimestamp(), money = dms.MoneyReceived}).ToArray());
					await Context.User.SendFileAsync(await JsonConvert.SerializeObject(stats).ToStream().ConfigureAwait(false), $"{DateTime.Now:yyyy-MM-dd_hh-mm-ss}_dailymoney-stats.json").ConfigureAwait(false);
				}
			}
		}
	}
}
