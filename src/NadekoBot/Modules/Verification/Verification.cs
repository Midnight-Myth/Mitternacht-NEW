using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using GommeHDnetForumAPI.DataModels.Entities;
using GommeHDnetForumAPI.DataModels.Exceptions;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Verification.Services;
using Mitternacht.Services;
using Mitternacht.Services.Impl;

namespace Mitternacht.Modules.Verification
{
    public class Verification : NadekoTopLevelModule<VerificationService>
    {
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

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        [RequireNoBot]
        public async Task IdentityValidationDmKey(long forumUserId) {
            if (!_fs.LoggedIn) {
                (await ReplyErrorLocalized("disabled").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }

            UserInfo uinfo = null;
            try
            {
                uinfo = await _fs.Forum.GetUserInfo(forumUserId);
            }
            catch (UserProfileAccessException)
            {
                (await ReplyErrorLocalized("forum_user_not_seeable").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }
            catch (Exception) { /*ignore*/ }

            if (uinfo == null)
            {
                (await ReplyErrorLocalized("forum_user_not_existing").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }

            if (!Service.CanVerifyForumAccount(Context.Guild.Id, Context.User.Id, forumUserId)) {
                (await ReplyErrorLocalized("already_verified").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }
            if (Service.ValidationKeys.Any(k => k.GuildId == Context.Guild.Id && k.ForumUserId == forumUserId && k.DiscordUserId == Context.User.Id)) {
                (await ReplyErrorLocalized("key_existing").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }

            var oldkey = Service.ValidationKeys.FirstOrDefault(k => k.GuildId == Context.Guild.Id && k.KeyScope == VerificationService.KeyScope.Forum && k.DiscordUserId == Context.User.Id);
            if (oldkey != null) {
                Service.ValidationKeys.TryRemove(oldkey);
            }

            var key = Service.GenerateKey(VerificationService.KeyScope.Forum, forumUserId, Context.User.Id, Context.Guild.Id);
            var ch = await Context.User.GetOrCreateDMChannelAsync().ConfigureAwait(false);
            var users = Service.GetAdditionalVerificationUsers(Context.Guild.Id);
            var msg = await ch.SendConfirmAsync(GetText("message_title", 1), GetText("message_dm_forum_key", key.Key, Context.User.ToString(), Context.Guild.Name, $"{uinfo.Username} (ID {uinfo.Id})", _ch.GetPrefix(Context.Guild), _fs.Forum.GetConversationCreationUrl(users.Prepend(_fs.Forum.SelfUser.Username).ToArray())) + (oldkey != null ? "\n\n" + GetText("key_replaced", oldkey.Key) : "")).ConfigureAwait(false);
            if (msg == null) {
                (await ReplyErrorLocalized("conversation_failed").ConfigureAwait(false)).DeleteAfter(60);
                Service.ValidationKeys.TryRemove(key);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        [RequireNoBot]
        public async Task IdentityValidationDmKey(string forumUsername) {
            if (!_fs.LoggedIn)
            {
                (await ReplyErrorLocalized("disabled").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }
            UserInfo uinfo = null;
            try {
                uinfo = await _fs.Forum.GetUserInfo(forumUsername);
            }
            catch (UserProfileAccessException) {
                (await ReplyErrorLocalized("forum_user_not_seeable").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }
            catch (Exception) { /*ignore*/ }

            if (uinfo == null) {
                (await ReplyErrorLocalized("forum_user_not_existing").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }
            await IdentityValidationDmKey(uinfo.Id).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        [RequireNoBot]
        public async Task IdentityValidationSubmitkey(long forumUserId) {
            if (!_fs.LoggedIn)
            {
                (await ReplyErrorLocalized("disabled").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }

            UserInfo uinfo = null;
            try
            {
                uinfo = await _fs.Forum.GetUserInfo(forumUserId);
            }
            catch (UserProfileAccessException)
            {
                (await ReplyErrorLocalized("forum_user_not_seeable").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }
            catch (Exception) { /*ignore*/ }

            if (uinfo == null)
            {
                (await ReplyErrorLocalized("forum_user_not_existing").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }

            if (!Service.CanVerifyForumAccount(Context.Guild.Id, Context.User.Id, forumUserId))
            {
                (await ReplyErrorLocalized("already_verified").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }

            var forumkey = Service.ValidationKeys.FirstOrDefault(vk => vk.KeyScope == VerificationService.KeyScope.Forum && vk.ForumUserId == forumUserId && vk.DiscordUserId == Context.User.Id && vk.GuildId == Context.Guild.Id);
            if (forumkey == null) {
                (await ReplyErrorLocalized("no_valid_key").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }
            var conversations = await _fs.Forum.GetConversations().ConfigureAwait(false);
            var con = conversations.FirstOrDefault(ci => (string.IsNullOrWhiteSpace(Service.GetVerifyString(Context.Guild.Id)) || ci.Title.Trim().Equals(Service.GetVerifyString(Context.Guild.Id))) && ci.Author.Id == forumUserId);
            if (con == null) {
                (await ReplyErrorLocalized("no_valid_conversation").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }
            if (con.Author.Username.Length > 16) {
                (await ReplyErrorLocalized("forum_acc_not_connected").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }

            await con.DownloadMessagesAsync().ConfigureAwait(false);
            var messageparts = con.Messages[0].Content.Split('\n');
            if (!(messageparts.Any(mp => mp.Trim().Contains(Context.User.Id.ToString())) 
                    || messageparts.Any(mp => mp.Trim().Contains(Context.User.ToString()))) 
                  || !messageparts.Any(mp => mp.Trim().Contains(forumkey.Key))) {
                (await ReplyErrorLocalized("no_valid_conversation").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }

            Service.ValidationKeys.TryRemove(forumkey);
            var success = await con.Reply(Service.GenerateKey(VerificationService.KeyScope.Discord, forumUserId, Context.User.Id, Context.Guild.Id).Key).ConfigureAwait(false);

            if (!success) {
                (await ReplyErrorLocalized("forum_conversation_reply_failure").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }
            var ch = await Context.User.GetOrCreateDMChannelAsync().ConfigureAwait(false);
            await ch.SendConfirmAsync(GetText("message_title", 2), GetText("message_forum_discord_key", Context.Guild.Name, _ch.GetPrefix(Context.Guild))).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        [RequireNoBot]
        public async Task IdentityValidationSubmitkey(string forumUsername) {
            if (!_fs.LoggedIn)
            {
                (await ReplyErrorLocalized("disabled").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }

            UserInfo uinfo = null;
            try
            {
                uinfo = await _fs.Forum.GetUserInfo(forumUsername);
            }
            catch (UserProfileAccessException)
            {
                (await ReplyErrorLocalized("forum_user_not_seeable").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }
            catch (Exception) { /*ignore*/ }

            if (uinfo == null)
            {
                (await ReplyErrorLocalized("forum_user_not_existing").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }
            await IdentityValidationSubmitkey(uinfo.Id).ConfigureAwait(false);
        }



        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireNoBot]
        public async Task IdentityValidationSubmit([Remainder]string discordkey) {
            if (!_fs.LoggedIn)
            {
                (await ReplyErrorLocalized("disabled").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }
            
            var key = Service.ValidationKeys.FirstOrDefault(vk => vk.KeyScope == VerificationService.KeyScope.Discord && vk.DiscordUserId == Context.User.Id && vk.GuildId == Context.Guild.Id && vk.Key == discordkey);
            if (key == null) {
                (await ReplyErrorLocalized("no_valid_key").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }
            await Service.SetVerified(Context.Guild, Context.User as IGuildUser, key.ForumUserId).ConfigureAwait(false);
            Service.ValidationKeys.TryRemove(key);
            UserInfo uinfo = null;
            try
            {
                uinfo = await _fs.Forum.GetUserInfo(key.ForumUserId);
            }
            catch (UserProfileAccessException)
            {
                (await ReplyErrorLocalized("forum_user_not_seeable").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }
            catch (Exception) { /*ignore*/ }

            if (uinfo == null)
            {
                (await ReplyErrorLocalized("forum_user_not_existing").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }

            var ch = await Context.User.GetOrCreateDMChannelAsync().ConfigureAwait(false);
            await ch.SendConfirmAsync(GetText("message_title", 3), GetText("verification_completed", Context.Guild.Name, uinfo.Username)).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task AddVerification(IGuildUser user, long forumUserId) {
            if (!Service.CanVerifyForumAccount(Context.Guild.Id, Context.User.Id, forumUserId))
            {
                (await ReplyErrorLocalized("already_verified").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }
            await Service.SetVerified(Context.Guild, user, forumUserId);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(2)]
        [OwnerOnly]
        public async Task RemoveVerification(IUser user) {
            if (user == null) return;
            using (var uow = _db.UnitOfWork)
                await (uow.VerifiedUsers.RemoveVerification(Context.Guild.Id, user.Id) 
                    ? ConfirmLocalized("removed_discord", user.ToString()) 
                    : ErrorLocalized("removed_discord_fail", user.ToString()))
                    .ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        [OwnerOnly]
        public async Task RemoveVerification(long forumUserId) {
            using (var uow = _db.UnitOfWork)
                (await (uow.VerifiedUsers.RemoveVerification(Context.Guild.Id, forumUserId) 
                    ? ConfirmLocalized("removed_forum", forumUserId) 
                    : ErrorLocalized("removed_forum_fail", forumUserId))
                    .ConfigureAwait(false)).DeleteAfter(60);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        [OwnerOnly]
        public async Task RemoveVerification(string forumUsername)
        {
            UserInfo uinfo = null;
            try {
                uinfo = await _fs.Forum.GetUserInfo(forumUsername).ConfigureAwait(false);
            }
            catch (Exception) {/*ignore*/}
            if (uinfo == null) {
                (await ErrorLocalized("forum_user_not_existing").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }
            using (var uow = _db.UnitOfWork)
                (await (uow.VerifiedUsers.RemoveVerification(Context.Guild.Id, uinfo.Id)
                        ? ConfirmLocalized("removed_forum", uinfo.Username)
                        : ErrorLocalized("removed_forum_fail", uinfo.Username))
                    .ConfigureAwait(false)).DeleteAfter(60);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        [OwnerOnly]
        public async Task VerifiedRole()
        {
            var roleid = Service.GetVerifiedRoleId(Context.Guild.Id);
            Task<IUserMessage> msgtask;
            if (roleid == null) msgtask = ConfirmLocalized("role_set_null");
            else
            {
                var role = Context.Guild.GetRole(roleid.Value);
                msgtask = role == null ? ErrorLocalized("role_not_found", roleid.Value) : ConfirmLocalized("role", role.Name);
            }
            await msgtask.ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        [OwnerOnly]
        public async Task VerifiedRole(IRole role) {
            var roleid = Service.GetVerifiedRoleId(Context.Guild.Id);
            if (roleid == role?.Id) return;
            await Service.SetVerifiedRole(Context.Guild.Id, role?.Id);
            await (role == null 
                ? ConfirmLocalized("role_set_null") 
                : ConfirmLocalized("role_set_not_null", role.Name))
                .ConfigureAwait(false);
        }
        

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        [OwnerOnly]
        public async Task VerifyString() 
            => await (string.IsNullOrWhiteSpace(Service.GetVerifyString(Context.Guild.Id)) 
                ? ConfirmLocalized("verifystring_void") 
                : ConfirmLocalized("verifystring", Service.GetVerifyString(Context.Guild.Id)))
            .ConfigureAwait(false);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        [OwnerOnly]
        public async Task VerifyString([Remainder]string verifystring) {
            var old = Service.GetVerifyString(Context.Guild.Id);
            verifystring = string.Equals(verifystring, "null", StringComparison.OrdinalIgnoreCase) ? null : verifystring.Trim();
            await Service.SetVerifyString(Context.Guild.Id, verifystring).ConfigureAwait(false);
            await ConfirmLocalized("verifystring_new", old ?? "null", verifystring ?? "null").ConfigureAwait(false);
        }


        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task VerificationKeys(int page = 1) {
            if (page < 1) page = 1;
            if (Service.ValidationKeys.Count <= 0) {
                await ConfirmLocalized("no_keys_present").ConfigureAwait(false);
                return;
            }

            const int keycount = 10;
            var pagecount = Service.ValidationKeys.Count / keycount;
            if (page > pagecount) page = pagecount;

            await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page - 1, async p => {
                var keys = Service.ValidationKeys.Where(k => k.GuildId == Context.Guild.Id).Skip(p * keycount).Take(keycount).ToList();
                var embed = new EmbedBuilder().WithOkColor().WithTitle(GetText("verification_keys"));
                foreach (var key in keys) {
                    var user = await Context.Guild.GetUserAsync(key.DiscordUserId).ConfigureAwait(false);
                    var discordname = string.IsNullOrWhiteSpace(user.Nickname) ? string.IsNullOrWhiteSpace(user.Username) ? user.Id.ToString() : user.Username : user.Nickname;
                    embed.AddInlineField(key.Key, GetText("verification_keys_field", discordname, key.ForumUserId, key.KeyScope));
                }
                return embed;
            }, pagecount).ConfigureAwait(false);
        }


        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task VerifiedUsers(int page = 1) {
            if (page < 1) page = 1;
            if (Service.GetVerifiedUserCount(Context.Guild.Id) <= 0) {
                await ReplyConfirmLocalized("no_users_verified").ConfigureAwait(false);
                return;
            }

            const int usercount = 20;
            var pagecount = Service.GetVerifiedUserCount(Context.Guild.Id) / usercount;
            if (page > pagecount) page = pagecount;

            await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page - 1, async p => {
                var vus = Service.GetVerifiedUsers(Context.Guild.Id).Skip(p * usercount).Take(usercount).ToList();
                var embed = new EmbedBuilder().WithOkColor().WithTitle(GetText("verified_users", Service.GetVerifiedUserCount(Context.Guild.Id)));
                foreach (var vu in vus) {
                    var user = await Context.Guild.GetUserAsync(vu.UserId).ConfigureAwait(false);
                    embed.AddInlineField((user?.ToString() ?? vu.UserId.ToString()).TrimTo(24, true), vu.ForumUserId);
                }
                return embed;
            }, pagecount).ConfigureAwait(false);
        }


        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task HowToVerify(bool dm = true, bool delete = true) {
            delete = !_creds.IsOwner(Context.User) || delete;
            var text = Service.GetVerificationTutorialText(Context.Guild.Id);
            if (string.IsNullOrWhiteSpace(text)) {
                (await ReplyErrorLocalized("tutorial_not_set").ConfigureAwait(false)).DeleteAfter(60);
                return;
            }
            var ch = dm ? await Context.User.GetOrCreateDMChannelAsync().ConfigureAwait(false) : Context.Channel;
            var msg = await ch.SendConfirmAsync(GetText("tutorial"), text).ConfigureAwait(false);
            if (delete && !dm) msg.DeleteAfter(120);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task SetVerifyTutorialText([Remainder] string text) {
            await Service.SetVerificationTutorialText(Context.Guild.Id, text).ConfigureAwait(false);
            (await ConfirmLocalized("tutorial_now_set").ConfigureAwait(false)).DeleteAfter(60);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [OwnerOnly]
        public async Task ReinitForum() {
            _fs.InitForumInstance();
            await ConfirmLocalized("reinit_forum").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [OwnerOnly]
        public async Task AdditionalVerificationUsers() {
            var users = Service.GetAdditionalVerificationUsers(Context.Guild.Id);
            if (users.Any())
                await Context.Channel.SendConfirmAsync(GetText("additional_verification_users_title"), users.Aggregate("", (s, u) => $"{s}- {u}\n", s => s.Substring(0, s.Length - 1))).ConfigureAwait(false);
            else
                await ErrorLocalized("additional_verification_users_not_set").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [OwnerOnly]
        public async Task SetAdditionalVerificationUsers([Remainder] string names = null) {
            var namesarray = string.IsNullOrWhiteSpace(names) ? new string[0] : names.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            await Service.SetAdditionalVerificationUsers(Context.Guild.Id, namesarray);
            await (namesarray.Any() ? ConfirmLocalized("additional_verification_users_set") : ConfirmLocalized("additional_verification_users_set_void"));
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task ConversationLink() {
            var users = Service.GetAdditionalVerificationUsers(Context.Guild.Id);
            if (_fs.LoggedIn)
                await ConfirmLocalized("conversation_start_link", _fs.Forum.GetConversationCreationUrl(users.Prepend(_fs.Forum.SelfUser.Username).ToArray())).ConfigureAwait(false);
            else {
                var msg = await ErrorLocalized("disabled").ConfigureAwait(false);
                msg.DeleteAfter(60);
            }
        }
    }
}