using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Verification.Services;
using NadekoBot.Services;

namespace NadekoBot.Modules.Verification
{
    public class Verification : NadekoTopLevelModule<VerificationService>
    {
        private readonly DbService _db;

        public Verification(DbService db) {
            _db = db;
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireNoBot]
        public async Task IdentityValidationDmKey(long forumuserid) {
            if (!_service.Enabled) {
                var msg = await ReplyErrorLocalized("disabled");
                msg.DeleteAfter(60);
                return;
            }
            if (_service.CanVerifyForumAccount(Context.Guild.Id, Context.User.Id, forumuserid)) {
                var msg = await ReplyErrorLocalized("already_verified");
                msg.DeleteAfter(60);
                return;
            }

            var key = _service.GetKey(VerificationService.KeyScope.Forum, forumuserid, Context.User.Id, Context.Guild.Id);
            var ch = await Context.User.GetOrCreateDMChannelAsync();
            await ch.SendMessageAsync("Key: " + key);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireNoBot]
        public async Task IdentityValidationSubmitkey(long forumuserid) {
            if (!_service.Enabled)
            {
                var msg = await ReplyErrorLocalized("disabled");
                msg.DeleteAfter(60);
                return;
            }
            if (_service.CanVerifyForumAccount(Context.Guild.Id, Context.User.Id, forumuserid))
            {
                var msg = await ReplyErrorLocalized("already_verified");
                msg.DeleteAfter(60);
                return;
            }
            var forumkey = _service.ValidationKeys.FirstOrDefault(vk => vk.KeyScope == VerificationService.KeyScope.Forum && vk.ForumUserId == forumuserid && vk.DiscordUserId == Context.User.Id && vk.GuildId == Context.Guild.Id);
            if (forumkey == null) return;
            var conversations = await _service.Forum.GetConversations();
            var con = conversations.FirstOrDefault(ci => ci.Title == "Verifizierung Plauderkonfi" && ci.Author.Id == forumuserid);
            if (con == null) return;
            await con.DownloadMessagesAsync();
            var messageparts = con.Messages[0].Content.Split('\n');
            if (!(messageparts.Contains(Context.User.Id.ToString()) && messageparts.Contains(forumkey.Key) && (_service.GetVerifyString(Context.Guild.Id) == null || messageparts.Contains(_service.GetVerifyString(Context.Guild.Id))))) return;

            _service.ValidationKeys.TryRemove(forumkey);
            var success = await con.Reply("Key für Discord: " + _service.GetKey(VerificationService.KeyScope.Discord, forumuserid, Context.User.Id, Context.Guild.Id));

            if (!success) {
                var msg = await ReplyErrorLocalized("reply_failure");
                msg.DeleteAfter(60);
            }
            else {
                var ch = await Context.User.GetOrCreateDMChannelAsync();
                await ch.SendMessageAsync($"Dir wurde ein Key zugesendet. Löse ihn auf dem Server `{Context.Guild.Name}` mit dem Befehl `.identityvalidationsubmit` ein.");
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireNoBot]
        public async Task IdentityValidationSubmit(string discordkey) {
            if (!_service.Enabled)
            {
                var msg = await ReplyErrorLocalized("disabled");
                msg.DeleteAfter(60);
                return;
            }
            
            var key = _service.ValidationKeys.FirstOrDefault(vk => vk.KeyScope == VerificationService.KeyScope.Discord && vk.DiscordUserId == Context.User.Id && vk.GuildId == Context.Guild.Id && vk.Key == discordkey);
            if (key == null) return;
            using (var uow = _db.UnitOfWork) {
                uow.VerificatedUser.SetVerified(Context.Guild.Id, Context.User.Id, key.ForumUserId);
                var roleid = _service.GetVerifiedRoleId(Context.Guild.Id);
                var role = roleid != null ? Context.Guild.GetRole(roleid.Value) : null;
                if (role != null && Context.User is IGuildUser guildUser) await guildUser.AddRoleAsync(role);
            }
            _service.ValidationKeys.TryRemove(key);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task RemoveValidation(IUser user) {
            if (user == null) return;
            using (var uow = _db.UnitOfWork) {
                var msg = uow.VerificatedUser.RemoveVerification(Context.Guild.Id, user.Id) ? await ConfirmLocalized("removed_discord", user.Username) : await ErrorLocalized("removed_discord_fail", user.Username);
                msg.DeleteAfter(60);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task RemoveForumValidation(long forumUserId) {
            using (var uow = _db.UnitOfWork)
                (await (uow.VerificatedUser.RemoveVerification(Context.Guild.Id, forumUserId) ? ConfirmLocalized("removed_forum", forumUserId) : ErrorLocalized("removed_forum_fail", forumUserId))).DeleteAfter(60);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task VerifiedRole(IRole role) {
            var roleid = _service.GetVerifiedRoleId(Context.Guild.Id);
            if (roleid == role?.Id) return;
            await _service.SetVerifiedRole(Context.Guild.Id, role?.Id);
            (role == null ? await ConfirmLocalized("role_not_set") : await ConfirmLocalized("role_set_not_null", role.Id)).DeleteAfter(60);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task VerifiedRole() {
            var roleid = _service.GetVerifiedRoleId(Context.Guild.Id);
            IUserMessage msg;
            if (roleid == null) msg = await ConfirmLocalized("role_not_set");
            else {
                var role = Context.Guild.GetRole(roleid.Value);
                msg = role == null ? await ErrorLocalized("role_not_found", roleid.Value) : await ConfirmLocalized("role", role.Name);
            }
            msg.DeleteAfter(60);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task VerifyString() {
            var vs = _service.GetVerifyString(Context.Guild.Id);
            (string.IsNullOrWhiteSpace(vs) ? await ConfirmLocalized("verifystring_void") : await ConfirmLocalized("verifystring", vs)).DeleteAfter(60);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task VerifyString(string verifystring) {
            var old = _service.GetVerifyString(Context.Guild.Id);
            verifystring = string.Equals(verifystring, "null", StringComparison.OrdinalIgnoreCase) ? null : verifystring;
            await _service.SetVerifyString(Context.Guild.Id, verifystring);
            var msg = await ConfirmLocalized("verifystring_new", old, verifystring);
            msg.DeleteAfter(60);
        }
    }
}