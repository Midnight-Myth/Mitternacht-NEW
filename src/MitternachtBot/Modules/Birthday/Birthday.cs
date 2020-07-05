using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Birthday.Models;
using Mitternacht.Modules.Birthday.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Birthday {
	public class Birthday : MitternachtTopLevelModule<BirthdayService> {
		private readonly IUnitOfWork uow;
		private readonly IBotCredentials _botCreds;
		private readonly IBotConfigProvider _botConf;

		public Birthday(IUnitOfWork uow, IBotCredentials botCreds, IBotConfigProvider botConf) {
			this.uow = uow;
			_botCreds = botCreds;
			_botConf = botConf;
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task BirthdaySet(IBirthDate bd) {
			var bdm = uow.BirthDates.GetUserBirthDate(Context.User.Id);
			if(!_botCreds.IsOwner(Context.User) && (bdm?.Year != null || bdm != null && bdm.Year == null && (bdm.Day != bd.Day || bdm.Month != bd.Month))) {
				await ReplyErrorLocalized("set_before").ConfigureAwait(false);
				return;
			}

			uow.BirthDates.SetBirthDate(Context.User.Id, bd);
			await uow.SaveChangesAsync(false).ConfigureAwait(false);

			await ReplyConfirmLocalized("set", bd.ToString()).ConfigureAwait(false);

			if(bd.IsBirthday(DateTime.Now) && (bdm == null || !bdm.IsBirthday(DateTime.Now))) {
				var gc = uow.GuildConfigs.For(Context.Guild.Id);
				if(!gc.BirthdaysEnabled)
					return;
				var bdmChId = gc.BirthdayMessageChannelId;
				if(bdmChId != null) {
					var bdmCh = await Context.Guild.GetTextChannelAsync(bdmChId.Value).ConfigureAwait(false);
					if(bdmCh != null)
						await bdmCh.SendMessageAsync(string.Format(gc.BirthdayMessage, Context.User.Mention)).ConfigureAwait(false);
				}

				var roleId = gc.BirthdayRoleId;
				if(roleId != null) {
					var role = Context.Guild.GetRole(roleId.Value);
					if(role != null) {
						await ((SocketGuildUser)Context.User).AddRoleAsync(role).ConfigureAwait(false);
					}
				}
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[OwnerOnly]
		public async Task BirthdaySet(IBirthDate bd, IUser user) {
			uow.BirthDates.SetBirthDate(user.Id, bd);
			await uow.SaveChangesAsync(false).ConfigureAwait(false);

			if(user == Context.User)
				await ConfirmLocalized("set", bd.ToString()).ConfigureAwait(false);
			else
				await ConfirmLocalized("set_owner", user.ToString(), bd.ToString()).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[OwnerOnly]
		public async Task BirthdayRemove(IUser user) {
			var success = uow.BirthDates.DeleteBirthDate(user.Id);
			if(success)
				await ConfirmLocalized("removed", user.ToString()).ConfigureAwait(false);
			else
				await ErrorLocalized("remove_failed", user.ToString()).ConfigureAwait(false);
			await uow.SaveChangesAsync(false).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task BirthdayGet(IUser user = null) {
			user ??= Context.User;
			var bdm = uow.BirthDates.GetUserBirthDate(user.Id);
			var age = bdm?.Year != null ? (int)Math.Floor((DateTime.Now - new DateTime(bdm.Year.Value, bdm.Month, bdm.Day)).TotalDays / 365.25) : 0;
			if(user == Context.User)
				if(bdm == null)
					await ReplyErrorLocalized("self_none").ConfigureAwait(false);
				else if(bdm.Year == null)
					await ReplyConfirmLocalized("self", bdm.ToString()).ConfigureAwait(false);
				else
					await ReplyConfirmLocalized("self_age", bdm.ToString(), age).ConfigureAwait(false);
			else if(bdm == null)
				await ReplyErrorLocalized("user_none", user.ToString()).ConfigureAwait(false);
			else if(bdm.Year == null)
				await ReplyConfirmLocalized("user", user.ToString(), bdm.ToString()).ConfigureAwait(false);
			else
				await ReplyConfirmLocalized("user_age", user.ToString(), bdm.ToString(), age).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task Birthdays(IBirthDate bd = null) {
			bd ??= BirthDate.TodayWithoutYear;
			var birthdates = uow.BirthDates.GetBirthdays(bd, bd.Year.HasValue).ToList();

			if(!birthdates.Any())
				await ConfirmLocalized("none_date", bd.ToString()).ConfigureAwait(false);
			else {
				var eb = new EmbedBuilder()
					.WithOkColor()
					.WithTitle(GetText("list_title", bd.ToString()))
					.WithDescription(string.Join("\n", birthdates.Select(bdm => $"- {Context.Client.GetUserAsync(bdm.UserId).GetAwaiter().GetResult()?.ToString() ?? bdm.UserId.ToString()}{(bdm.Year.HasValue && !bd.Year.HasValue ? $"{BirthDate.Today.Year - bdm.Year}" : "")}")));
				await Context.Channel.EmbedAsync(eb).ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task BirthdaysAll(int page = 1) {
			if(page < 1)
				page = 1;
			var birthdates = uow.BirthDates.GetAll().OrderBy(bdm => bdm.Month).ThenBy(bdm => bdm.Day).ToList();

			if(!birthdates.Any()) {
				await ErrorLocalized("none").ConfigureAwait(false);
				return;
			}

			const int itemcount = 10;
			var pagecount = (int)Math.Ceiling(birthdates.Count / (itemcount * 1d));
			page = page > pagecount ? pagecount : page;

			await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page - 1,
				p => new EmbedBuilder().WithOkColor().WithTitle(GetText("all_title")).WithDescription(string.Join("\n",
					birthdates.Skip(itemcount * p).Take(itemcount).Select(BdmToString))),
				pagecount - 1, reactUsers: new[] { Context.User as IGuildUser }).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task BirthdayRole() {
			var bdayroleid = uow.GuildConfigs.For(Context.Guild.Id).BirthdayRoleId;
			var bdayrole = bdayroleid.HasValue ? Context.Guild.GetRole(bdayroleid.Value) : null;
			if(!bdayroleid.HasValue)
				await ErrorLocalized("role", GetText("role_not_set")).ConfigureAwait(false);
			else if(bdayrole == null)
				await ErrorLocalized("role", GetText("role_not_existing")).ConfigureAwait(false);
			else
				await ConfirmLocalized("role", bdayrole.Name).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOnly]
		public async Task BirthdayRole(IRole role) {
			var gc = uow.GuildConfigs.For(Context.Guild.Id);
			var oldroleid = gc.BirthdayRoleId;
			var oldrole = oldroleid.HasValue ? Context.Guild.GetRole(oldroleid.Value) : null;
			gc.BirthdayRoleId = role.Id;
			uow.GuildConfigs.Update(gc);
			await uow.SaveChangesAsync(false).ConfigureAwait(false);
			await ConfirmLocalized("role_set", oldrole?.Name ?? oldroleid?.ToString() ?? Format.Italics("null"), role.Name).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOnly]
		public async Task BirthdayRoleRemove() {
			var gc = uow.GuildConfigs.For(Context.Guild.Id);
			var oldroleid = gc.BirthdayRoleId;
			var oldrole = oldroleid.HasValue ? Context.Guild.GetRole(oldroleid.Value) : null;
			gc.BirthdayRoleId = null;
			uow.GuildConfigs.Update(gc);
			await uow.SaveChangesAsync(false).ConfigureAwait(false);
			await ConfirmLocalized("role_set", oldrole?.Name ?? oldroleid?.ToString() ?? Format.Italics("null"), Format.Italics("null")).ConfigureAwait(false);
		}


		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task BirthdayMessageChannel() {
			var bdayMsgChId = uow.GuildConfigs.For(Context.Guild.Id).BirthdayMessageChannelId;
			var bdayMsgCh = bdayMsgChId.HasValue ? await Context.Guild.GetTextChannelAsync(bdayMsgChId.Value).ConfigureAwait(false) : null;
			if(!bdayMsgChId.HasValue)
				await ErrorLocalized("msgch", GetText("msgch_not_set")).ConfigureAwait(false);
			else if(bdayMsgCh == null)
				await ErrorLocalized("msgch", GetText("msgch_not_existing")).ConfigureAwait(false);
			else
				await ConfirmLocalized("msgch", bdayMsgCh.Name).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOnly]
		public async Task BirthdayMessageChannel(ITextChannel channel) {
			var gc = uow.GuildConfigs.For(Context.Guild.Id);
			var oldChId = gc.BirthdayMessageChannelId;
			var oldch = oldChId.HasValue ? await Context.Guild.GetTextChannelAsync(oldChId.Value).ConfigureAwait(false) : null;
			gc.BirthdayMessageChannelId = channel.Id;
			uow.GuildConfigs.Update(gc);
			await uow.SaveChangesAsync(false).ConfigureAwait(false);
			await ConfirmLocalized("msgch_set", oldch?.Name ?? oldChId?.ToString() ?? Format.Italics("null"), channel.Name).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOnly]
		public async Task BirthdayMessageChannelRemove() {
			var gc = uow.GuildConfigs.For(Context.Guild.Id);
			var oldChId = gc.BirthdayMessageChannelId;
			var oldCh = oldChId.HasValue ? await Context.Guild.GetTextChannelAsync(oldChId.Value).ConfigureAwait(false) : null;
			gc.BirthdayMessageChannelId = null;
			uow.GuildConfigs.Update(gc);
			await uow.SaveChangesAsync(false).ConfigureAwait(false);
			await ConfirmLocalized("msgch_set", oldCh?.Name ?? oldChId?.ToString() ?? Format.Italics("null"), Format.Italics("null")).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOnly]
		[Priority(1)]
		public async Task BirthdayMessage() {
			await ConfirmLocalized("msg", uow.GuildConfigs.For(Context.Guild.Id).BirthdayMessage).ConfigureAwait(false);
			await uow.SaveChangesAsync(false).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOnly]
		[Priority(0)]
		public async Task BirthdayMessage([Remainder] string msg) {
			var gc = uow.GuildConfigs.For(Context.Guild.Id);
			var oldmsg = gc.BirthdayMessage;
			gc.BirthdayMessage = msg;
			uow.GuildConfigs.Update(gc);
			await uow.SaveChangesAsync(false).ConfigureAwait(false);
			await ConfirmLocalized("msg_changed", oldmsg ?? "null", msg);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOnly]
		public async Task BirthdayReactions(bool enable) {
			var gc = uow.GuildConfigs.For(Context.Guild.Id);
			if(gc.BirthdaysEnabled == enable) {
				await ErrorLocalized("enable_same", GetEnabledText(enable)).ConfigureAwait(false);
				return;
			}

			gc.BirthdaysEnabled = enable;
			uow.GuildConfigs.Update(gc);
			await uow.SaveChangesAsync(false).ConfigureAwait(false);
			await ConfirmLocalized("enable", GetEnabledText(enable)).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOnly]
		public async Task BirthdayMoney(long money) {
			var gc = uow.GuildConfigs.For(Context.Guild.Id);
			if(gc.BirthdayMoney == money) {
				await ReplyErrorLocalized("money_already_set").ConfigureAwait(false);
				return;
			}
			var oldMoney = gc.BirthdayMoney ?? 0;
			gc.BirthdayMoney = money;
			uow.GuildConfigs.Update(gc);
			await ReplyConfirmLocalized("money_set", _botConf.BotConfig.CurrencySign, oldMoney, money).ConfigureAwait(false);

			await uow.SaveChangesAsync(false).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task BirthdayMoney() {
			var gc = uow.GuildConfigs.For(Context.Guild.Id);
			var money = gc.BirthdayMoney ?? 0;
			await ReplyConfirmLocalized("money", _botConf.BotConfig.CurrencySign, money).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task BirthdayMessageEvent(bool? enable = null) {
			var hasBirthdate = uow.BirthDates.HasBirthDate(Context.User.Id);
			if(!hasBirthdate) {
				await ReplyErrorLocalized("messageevent_birthday_not_set").ConfigureAwait(false);
				return;
			}

			var bdm = uow.BirthDates.GetUserBirthDate(Context.User.Id);
			if(!enable.HasValue)
				await ReplyConfirmLocalized($"messageevent_show", GetEnabledText(bdm.BirthdayMessageEnabled)).ConfigureAwait(false);
			else {
				if(bdm.BirthdayMessageEnabled == enable.Value)
					await ReplyErrorLocalized($"messageevent_already_set", GetEnabledText(bdm.BirthdayMessageEnabled)).ConfigureAwait(false);
				else {
					bdm.BirthdayMessageEnabled = enable.Value;
					uow.BirthDates.Update(bdm);
					await uow.SaveChangesAsync(false).ConfigureAwait(false);
					await ReplyConfirmLocalized($"messageevent_changed", GetEnabledText(enable.Value)).ConfigureAwait(false);
				}
			}
		}


		private string BdmToString(BirthDateModel bdm)
			=> $"- {Context.Client.GetUserAsync(bdm.UserId).GetAwaiter().GetResult()?.ToString() ?? bdm.UserId.ToString()} - **{bdm}**";

		private string GetEnabledText(bool enabled)
			=> GetText(enabled ? "enabled" : "disabled");
	}
}
