using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using GommeHDnetForumAPI.Models.Entities;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Forum.Services;
using Mitternacht.Modules.Verification.Common;
using Mitternacht.Modules.Verification.Exceptions;
using Mitternacht.Modules.Verification.Services;
using Mitternacht.Services;

namespace Mitternacht.Modules.Verification {
	public partial class Verification : MitternachtTopLevelModule<VerificationService> {
		private readonly DbService _db;
		private readonly IBotCredentials _creds;
		private readonly CommandHandler _ch;
		private readonly ForumService _fs;

		public Verification(DbService db, IBotCredentials creds, CommandHandler ch, ForumService fs) {
			_db = db;
			_creds = creds;
			_ch = ch;
			_fs = fs;
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task Verify() {
			if(_fs.LoggedIn) {
				try {
					await Service.StartVerification(Context.User as IGuildUser).ConfigureAwait(false);
				} catch(UserAlreadyVerifyingException) {
					await ReplyErrorLocalized("already_started").ConfigureAwait(false);
				} catch(UserAlreadyVerifiedException) {
					await ReplyErrorLocalized("already_verified").ConfigureAwait(false);
				} catch(HttpException e) {
					if(e.HttpCode == System.Net.HttpStatusCode.Forbidden)
						await ReplyErrorLocalized("unable_sending_dm").ConfigureAwait(false);
					else
						await ReplyErrorLocalized("unknown_http_exception", e.HttpCode).ConfigureAwait(false);
				} catch(Exception) {
					await ReplyErrorLocalized("unknown_error").ConfigureAwait(false);
				}
			} else {
				await ReplyErrorLocalized("forum_not_logged_in").ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOnly]
		public async Task AddVerification(IGuildUser user, long forumUserId) {
			using var uow = _db.UnitOfWork;
			var forumUserName = forumUserId.ToString();
			try {
				var forumUser = await _fs.Forum.GetUserInfo(forumUserId).ConfigureAwait(false);
				if(!string.IsNullOrWhiteSpace(forumUser.Username))
					forumUserName = forumUser.Username;
			} catch(Exception) { /* ignore, any exception here is irrelevant to the execution of this method */ }

			if(uow.VerifiedUsers.IsDiscordUserVerified(user.GuildId, user.Id))
				await ErrorLocalized("already_verified_discord", user.ToString()).ConfigureAwait(false);
			else if(uow.VerifiedUsers.IsForumUserVerified(user.GuildId, forumUserId))
				await ErrorLocalized("already_verified_forum", forumUserName).ConfigureAwait(false);
			else {
				await Service.SetVerified(user, forumUserId).ConfigureAwait(false);
				await ConfirmLocalized("add_manually_success", user.ToString(), forumUserName).ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[Priority(1)]
		[OwnerOnly]
		public async Task RemoveVerificationDiscord(IGuildUser guildUser) {
			using var uow = _db.UnitOfWork;
			if(uow.VerifiedUsers.RemoveVerification(guildUser.GuildId, guildUser.Id))
				await ConfirmLocalized("removed_discord", guildUser.ToString()).ConfigureAwait(false);
			else
				await ErrorLocalized("removed_discord_error", guildUser.ToString()).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[Priority(0)]
		[OwnerOnly]
		public async Task RemoveVerificationDiscord(ulong userId) {
			var guildUser = await Context.Guild.GetUserAsync(userId).ConfigureAwait(false);

			if(guildUser is null) {
				using var uow = _db.UnitOfWork;
				if(uow.VerifiedUsers.RemoveVerification(Context.Guild.Id, userId))
					await ConfirmLocalized("removed_discord", userId.ToString()).ConfigureAwait(false);
				else
					await ErrorLocalized("removed_discord_error", userId.ToString()).ConfigureAwait(false);
			} else {
				await RemoveVerificationDiscord(guildUser).ConfigureAwait(false);
			}

		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[Priority(1)]
		[OwnerOnly]
		public async Task RemoveVerificationForum(long forumUserId) {
			using var uow = _db.UnitOfWork;
			if(uow.VerifiedUsers.RemoveVerification(Context.Guild.Id, forumUserId))
				await ConfirmLocalized("removed_forum", forumUserId).ConfigureAwait(false);
			else
				await ErrorLocalized("removed_forum_error", forumUserId).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[Priority(0)]
		[OwnerOnly]
		public async Task RemoveVerificationForum(string forumUsername) {
			UserInfo uinfo = null;
			try {
				uinfo = await _fs.Forum.GetUserInfo(forumUsername).ConfigureAwait(false);
			} catch(Exception) {/*ignore*/}

			if(uinfo == null) {
				await ErrorLocalized("forumaccount_not_existing", forumUsername).ConfigureAwait(false);
			} else {
				using var uow = _db.UnitOfWork;
				if(uow.VerifiedUsers.RemoveVerification(Context.Guild.Id, uinfo.Id))
					await ConfirmLocalized("removed_forum", uinfo.Username).ConfigureAwait(false);
				else
					await ErrorLocalized("removed_forum_error", uinfo.Username).ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOnly]
		public async Task VerifiedRole() {
			var roleid = Service.GetVerifiedRoleId(Context.Guild.Id);

			if(roleid == null) {
				await ConfirmLocalized("role_current_not_set").ConfigureAwait(false);
			} else {
				var role = Context.Guild.GetRole(roleid.Value);
				if(role == null)
					await ErrorLocalized("role_not_found", roleid.Value).ConfigureAwait(false);
				else
					await ConfirmLocalized("role_current", role.Name).ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOnly]
		public async Task VerifiedRole(IRole role) {
			var roleid = Service.GetVerifiedRoleId(Context.Guild.Id);
			if(roleid == role.Id) {
				await ErrorLocalized("new_identical", role.Name).ConfigureAwait(false);
			} else {
				Service.SetVerifiedRole(Context.Guild.Id, role?.Id);
				await ConfirmLocalized("role_new", role.Name).ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOnly]
		public async Task VerifiedRoleDelete() {
			Service.SetVerifiedRole(Context.Guild.Id, null);
			await ConfirmLocalized("role_deleted").ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[Priority(1)]
		[OwnerOnly]
		public async Task VerifyPassword() {
			if(string.IsNullOrWhiteSpace(Service.GetVerifyString(Context.Guild.Id)))
				await ConfirmLocalized("verifypassword_not_checked").ConfigureAwait(false);
			else
				await ConfirmLocalized("verifypassword_current", Service.GetVerifyString(Context.Guild.Id)).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[Priority(0)]
		[OwnerOnly]
		public async Task VerifyPassword([Remainder]string password) {
			var oldPassword = Service.GetVerifyString(Context.Guild.Id);
			password = password.Equals("null", StringComparison.OrdinalIgnoreCase) ? null : password.Trim();

			Service.SetVerifyString(Context.Guild.Id, password);

			await ConfirmLocalized("verifypassword_new", oldPassword ?? "null", password ?? "null").ConfigureAwait(false);
		}


		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOnly]
		public async Task VerificationKeys(int page = 1) {
			if(page < 1)
				page = 1;
			if(!VerificationKeyManager.VerificationKeys.Any()) {
				await ConfirmLocalized("no_keys_present").ConfigureAwait(false);
				return;
			}

			const int keycount = 10;
			var pagecount = (int)Math.Ceiling(VerificationKeyManager.VerificationKeys.Count / (keycount * 1d));
			if(page > pagecount)
				page = pagecount;

			await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page - 1, async p => {
				var keys = VerificationKeyManager.VerificationKeys.Where(k => k.GuildId == Context.Guild.Id).Skip(p * keycount).Take(keycount).ToList();
				var embed = new EmbedBuilder().WithOkColor().WithTitle(GetText("verification_keys"));
				foreach(var key in keys) {
					var user = await Context.Guild.GetUserAsync(key.DiscordUserId).ConfigureAwait(false);
					var discordname = string.IsNullOrWhiteSpace(user.Nickname) ? string.IsNullOrWhiteSpace(user.Username) ? user.Id.ToString() : user.Username : user.Nickname;
					embed.AddField(key.Key, GetText("verification_keys_field", discordname, key.ForumUserId, key.KeyScope), true);
				}
				return embed;
			}, pagecount - 1, true, null, gp => gp.Administrator).ConfigureAwait(false);
		}


		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task VerifiedUsers(int page = 1) {
			if(page < 1)
				page = 1;
			if(Service.GetVerifiedUserCount(Context.Guild.Id) <= 0) {
				await ReplyConfirmLocalized("no_users_verified").ConfigureAwait(false);
				return;
			}

			const int usercount = 20;
			var pagecount = (int)Math.Ceiling(Service.GetVerifiedUserCount(Context.Guild.Id) / (usercount * 1d));
			if(page > pagecount)
				page = pagecount;

			await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page - 1, async p => {
				var vus = Service.GetVerifiedUsers(Context.Guild.Id).Skip(p * usercount).Take(usercount).ToList();
				var embed = new EmbedBuilder().WithOkColor().WithTitle(GetText("verified_users", Service.GetVerifiedUserCount(Context.Guild.Id)));
				foreach(var vu in vus) {
					var user = await Context.Guild.GetUserAsync(vu.UserId).ConfigureAwait(false);
					embed.AddField((user?.ToString() ?? vu.UserId.ToString()).TrimTo(24, true), vu.ForumUserId, true);
				}
				return embed;
			}, pagecount - 1, true, new[] { Context.User as IGuildUser }).ConfigureAwait(false);
		}


		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task HowToVerify(bool dm = true) {
			var text = Service.GetVerificationTutorialText(Context.Guild.Id);
			if(string.IsNullOrWhiteSpace(text)) {
				await ReplyErrorLocalized("tutorial_not_set").ConfigureAwait(false);
				return;
			}
			var ch = dm ? await Context.User.GetOrCreateDMChannelAsync().ConfigureAwait(false) : Context.Channel;
			await ch.SendConfirmAsync(GetText("tutorial"), text).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[Priority(1)]
		[OwnerOnly]
		public async Task VerifyTutorialText() {
			var text = Service.GetVerificationTutorialText(Context.Guild.Id);
			await ConfirmLocalized("tutorial_current_text", text).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[Priority(0)]
		[OwnerOnly]
		public async Task VerifyTutorialText([Remainder] string text) {
			Service.SetVerificationTutorialText(Context.Guild.Id, text);
			await ConfirmLocalized("tutorial_now_set").ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[OwnerOnly]
		public async Task AdditionalVerificationUsers() {
			var users = Service.GetAdditionalVerificationUsers(Context.Guild.Id);
			if(users.Any())
				await Context.Channel.SendConfirmAsync(GetText("additional_verification_users_title"), users.Aggregate("", (s, u) => $"{s}- {u}\n", s => s.Substring(0, s.Length - 1))).ConfigureAwait(false);
			else
				await ConfirmLocalized("additional_verification_users_not_set").ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[OwnerOnly]
		public async Task SetAdditionalVerificationUsers([Remainder] string names = null) {
			var namesarray = string.IsNullOrWhiteSpace(names) ? new string[0] : names.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			Service.SetAdditionalVerificationUsers(Context.Guild.Id, namesarray);

			if(namesarray.Any()) {
				await ConfirmLocalized("additional_verification_users_set").ConfigureAwait(false);
			} else {
				await ConfirmLocalized("additional_verification_users_set_void").ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		public async Task ConversationLink() {
			var users = Service.GetAdditionalVerificationUsers(Context.Guild.Id);

			await ConfirmLocalized("conversation_start_link", _fs.Forum.GetConversationCreationUrl(users.Prepend(_fs.Forum.SelfUser.Username).ToArray())).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOnly]
		public async Task VerificationPasswordChannel() {
			using var uow = _db.UnitOfWork;
			var channelId = uow.GuildConfigs.For(Context.Guild.Id).VerificationPasswordChannelId;

			if(channelId.HasValue) {
				await ConfirmLocalized("passwordchannel_current", MentionUtils.MentionChannel(channelId.Value)).ConfigureAwait(false);
			} else {
				await ConfirmLocalized("passwordchannel_current_not_set").ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOnly]
		public async Task VerificationPasswordChannel(ITextChannel channel) {
			using var uow = _db.UnitOfWork;
			var gc = uow.GuildConfigs.For(Context.Guild.Id);
			if(channel.Id == gc.VerificationPasswordChannelId) {
				await ErrorLocalized("passwordchannel_new_identical", channel.Mention).ConfigureAwait(false);
			} else {
				var oldPasswordChannelId = gc.VerificationPasswordChannelId;

				gc.VerificationPasswordChannelId = channel.Id;

				uow.GuildConfigs.Update(gc);
				await uow.CompleteAsync().ConfigureAwait(false);
				await ConfirmLocalized("passwordchannel_new", oldPasswordChannelId.HasValue ? MentionUtils.MentionChannel(oldPasswordChannelId.Value) : "null", channel.Mention).ConfigureAwait(false);
			}
		}
	}
}