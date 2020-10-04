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
using Mitternacht.Database;
using Mitternacht.Database.Models;

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
				await ReplyErrorLocalized("birthdayset_set_before").ConfigureAwait(false);
				return;
			}

			uow.BirthDates.SetBirthDate(Context.User.Id, bd);
			await uow.SaveChangesAsync(false).ConfigureAwait(false);

			await ReplyConfirmLocalized("birthdayset_set", bd.ToString()).ConfigureAwait(false);

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
				await ConfirmLocalized("birthdayset_set", bd.ToString()).ConfigureAwait(false);
			else
				await ConfirmLocalized("birthdayset_set_owner", user.ToString(), bd.ToString()).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[OwnerOnly]
		public async Task BirthdayRemove(IUser user) {
			var success = uow.BirthDates.DeleteBirthDate(user.Id);
			if(success)
				await ConfirmLocalized("birthdayremove_removed", user.ToString()).ConfigureAwait(false);
			else
				await ErrorLocalized("birthdayremove_remove_failed", user.ToString()).ConfigureAwait(false);
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
					await ReplyErrorLocalized("birthdayget_self_none").ConfigureAwait(false);
				else if(bdm.Year == null)
					await ReplyConfirmLocalized("birthdayget_self", bdm.ToString()).ConfigureAwait(false);
				else
					await ReplyConfirmLocalized("birthdayget_self_age", bdm.ToString(), age).ConfigureAwait(false);
			else if(bdm == null)
				await ReplyErrorLocalized("birthdayget_user_none", user.ToString()).ConfigureAwait(false);
			else if(bdm.Year == null)
				await ReplyConfirmLocalized("birthdayget_user", user.ToString(), bdm.ToString()).ConfigureAwait(false);
			else
				await ReplyConfirmLocalized("birthdayget_user_age", user.ToString(), bdm.ToString(), age).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task Birthdays(IBirthDate bd = null) {
			bd ??= BirthDate.TodayWithoutYear;
			var birthdates = uow.BirthDates.GetBirthdays(bd, bd.Year.HasValue).ToList();

			if(!birthdates.Any())
				await ConfirmLocalized("birthdays_none_date", bd.ToString()).ConfigureAwait(false);
			else {
				var eb = new EmbedBuilder()
					.WithOkColor()
					.WithTitle(GetText("birthdays_list_title", bd.ToString()))
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
				await ErrorLocalized("birthdaysall_none").ConfigureAwait(false);
				return;
			}

			const int elementsPerPage = 10;
			var pageCount = (int)Math.Ceiling(birthdates.Count * 1d / elementsPerPage);
			page = page > pageCount ? pageCount : page;

			await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page - 1,
				currentPage => new EmbedBuilder().WithOkColor().WithTitle(GetText("birthdaysall_all_title")).WithDescription(string.Join("\n",
					birthdates.Skip(elementsPerPage * currentPage).Take(elementsPerPage).Select(BdmToString))),
				pageCount, reactUsers: new[] { Context.User as IGuildUser }).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task BirthdayRole() {
			var bdayroleid = uow.GuildConfigs.For(Context.Guild.Id).BirthdayRoleId;
			var bdayrole = bdayroleid.HasValue ? Context.Guild.GetRole(bdayroleid.Value) : null;
			if(!bdayroleid.HasValue)
				await ErrorLocalized("birthdayrole_role", GetText("birthdayrole_role_not_set")).ConfigureAwait(false);
			else if(bdayrole == null)
				await ErrorLocalized("birthdayrole_role", GetText("birthdayrole_role_not_existing")).ConfigureAwait(false);
			else
				await ConfirmLocalized("birthdayrole_role", bdayrole.Name).ConfigureAwait(false);
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
			await ConfirmLocalized("birthdayrole_role_set", oldrole?.Name ?? oldroleid?.ToString() ?? Format.Italics("null"), role.Name).ConfigureAwait(false);
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
			await ConfirmLocalized("birthdayroleremove_role_set", oldrole?.Name ?? oldroleid?.ToString() ?? Format.Italics("null"), Format.Italics("null")).ConfigureAwait(false);
		}


		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task BirthdayMessageChannel() {
			var bdayMsgChId = uow.GuildConfigs.For(Context.Guild.Id).BirthdayMessageChannelId;
			var bdayMsgCh = bdayMsgChId.HasValue ? await Context.Guild.GetTextChannelAsync(bdayMsgChId.Value).ConfigureAwait(false) : null;
			if(!bdayMsgChId.HasValue)
				await ErrorLocalized("birthdaymessagechannel_msgch", GetText("birthdaymessagechannel_msgch_not_set")).ConfigureAwait(false);
			else if(bdayMsgCh == null)
				await ErrorLocalized("birthdaymessagechannel_msgch", GetText("birthdaymessagechannel_msgch_not_existing")).ConfigureAwait(false);
			else
				await ConfirmLocalized("birthdaymessagechannel_msgch", bdayMsgCh.Name).ConfigureAwait(false);
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
			await ConfirmLocalized("birthdaymessagechannel_msgch_set", oldch?.Name ?? oldChId?.ToString() ?? Format.Italics("null"), channel.Name).ConfigureAwait(false);
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
			await ConfirmLocalized("birthdaymessagechannelremove_msgch_set", oldCh?.Name ?? oldChId?.ToString() ?? Format.Italics("null"), Format.Italics("null")).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOnly]
		[Priority(1)]
		public async Task BirthdayMessage() {
			await ConfirmLocalized("birthdaymessage_msg", uow.GuildConfigs.For(Context.Guild.Id).BirthdayMessage).ConfigureAwait(false);
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
			await ConfirmLocalized("birthdaymessage_msg_changed", oldmsg ?? "null", msg);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOnly]
		public async Task BirthdayReactions(bool enable) {
			var gc = uow.GuildConfigs.For(Context.Guild.Id);
			if(gc.BirthdaysEnabled == enable) {
				await ErrorLocalized("birthdayreactions_enable_same", GetText(enable ? "birthdayreactions_enabled" : "birthdayreactions_disabled")).ConfigureAwait(false);
				return;
			}

			gc.BirthdaysEnabled = enable;
			uow.GuildConfigs.Update(gc);
			await uow.SaveChangesAsync(false).ConfigureAwait(false);
			await ConfirmLocalized("birthdayreactions_enable", GetText(enable ? "birthdayreactions_enabled" : "birthdayreactions_disabled")).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOnly]
		public async Task BirthdayMoney(long money) {
			var gc = uow.GuildConfigs.For(Context.Guild.Id);
			if(gc.BirthdayMoney == money) {
				await ReplyErrorLocalized("birthdaymoney_money_already_set").ConfigureAwait(false);
				return;
			}
			var oldMoney = gc.BirthdayMoney ?? 0;
			gc.BirthdayMoney = money;
			uow.GuildConfigs.Update(gc);
			await ReplyConfirmLocalized("birthdaymoney_money_set", _botConf.BotConfig.CurrencySign, oldMoney, money).ConfigureAwait(false);

			await uow.SaveChangesAsync(false).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task BirthdayMoney() {
			var gc = uow.GuildConfigs.For(Context.Guild.Id);
			var money = gc.BirthdayMoney ?? 0;
			await ReplyConfirmLocalized("birthdaymoney_money", _botConf.BotConfig.CurrencySign, money).ConfigureAwait(false);
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
				await ReplyConfirmLocalized($"messageevent_show", GetText(bdm.BirthdayMessageEnabled ? "birthdaymessageevent_enabled" : "birthdaymessageevent_disabled")).ConfigureAwait(false);
			else {
				if(bdm.BirthdayMessageEnabled == enable.Value)
					await ReplyErrorLocalized($"messageevent_already_set", GetText(bdm.BirthdayMessageEnabled ? "birthdaymessageevent_enabled" : "birthdaymessageevent_disabled")).ConfigureAwait(false);
				else {
					bdm.BirthdayMessageEnabled = enable.Value;
					uow.BirthDates.Update(bdm);
					await uow.SaveChangesAsync(false).ConfigureAwait(false);
					await ReplyConfirmLocalized($"messageevent_changed", GetText(enable.Value ? "birthdaymessageevent_enabled" : "birthdaymessageevent_disabled")).ConfigureAwait(false);
				}
			}
		}


		private string BdmToString(BirthDateModel bdm)
			=> $"- {Context.Client.GetUserAsync(bdm.UserId).GetAwaiter().GetResult()?.ToString() ?? bdm.UserId.ToString()} - **{bdm}**";
	}
}
