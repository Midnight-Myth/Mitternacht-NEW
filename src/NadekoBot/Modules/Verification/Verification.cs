using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Verification.Services;
using Mitternacht.Services;

namespace Mitternacht.Modules.Verification
{
    public class Verification : NadekoTopLevelModule<VerificationService>
    {
        private readonly DbService _db;
        private readonly IBotCredentials _creds;

        public Verification(DbService db, IBotCredentials creds) {
            _db = db;
            _creds = creds;
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireNoBot]
        public async Task IdentityValidationDmKey(long forumuserid) {
            if (!Service.Enabled) {
                var msg = await ReplyErrorLocalized("disabled");
                msg.DeleteAfter(60);
                return;
            }
            if (Service.CanVerifyForumAccount(Context.Guild.Id, Context.User.Id, forumuserid)) {
                var msg = await ReplyErrorLocalized("already_verified");
                msg.DeleteAfter(60);
                return;
            }

            var key = Service.GetKey(VerificationService.KeyScope.Forum, forumuserid, Context.User.Id, Context.Guild.Id);
            var ch = await Context.User.GetOrCreateDMChannelAsync();
            await ch.SendMessageAsync(key);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireNoBot]
        public async Task IdentityValidationSubmitkey(long forumuserid) {
            if (!Service.Enabled)
            {
                var msg = await ReplyErrorLocalized("disabled");
                msg.DeleteAfter(60);
                return;
            }
            if (Service.CanVerifyForumAccount(Context.Guild.Id, Context.User.Id, forumuserid))
            {
                var msg = await ReplyErrorLocalized("already_verified");
                msg.DeleteAfter(60);
                return;
            }
            var forumkey = Service.ValidationKeys.FirstOrDefault(vk => vk.KeyScope == VerificationService.KeyScope.Forum && vk.ForumUserId == forumuserid && vk.DiscordUserId == Context.User.Id && vk.GuildId == Context.Guild.Id);
            if (forumkey == null) {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}: Es existiert kein Key, der mit diesem Forum- und Discordaccount verknüpft ist!");
                return;
            }
            var conversations = await Service.Forum.GetConversations();
            var con = conversations.FirstOrDefault(ci => (string.IsNullOrWhiteSpace(Service.GetVerifyString(Context.Guild.Id)) || ci.Title.Trim().Equals(Service.GetVerifyString(Context.Guild.Id))) && ci.Author.Id == forumuserid);
            if (con == null) {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}: Der angegebene Forumaccount hat keine gültige Konversation erstellt!");
                return;
            }
            Service.Log.Info(con.UrlPath);
            Service.Log.Info(con);
            await con.DownloadMessagesAsync();
            var messageparts = con.Messages[0].Content.Split('\n');
            if (!(messageparts.Contains(Context.User.Id.ToString()) && messageparts.Contains(forumkey.Key))) return;

            Service.ValidationKeys.TryRemove(forumkey);
            var success = await con.Reply("Key für Discord: " + Service.GetKey(VerificationService.KeyScope.Discord, forumuserid, Context.User.Id, Context.Guild.Id));

            if (!success) {
                var msg = await ReplyErrorLocalized("reply_failure");
                msg.DeleteAfter(60);
            }
            else {
                var ch = await Context.User.GetOrCreateDMChannelAsync();
                await ch.SendMessageAsync("Dir wurde ein Key zugesendet. Löse ihn mit dem Befehl `.identityvalidationsubmit <key>` ein.");
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireNoBot]
        public async Task IdentityValidationSubmit([Remainder]string discordkey) {
            if (!Service.Enabled)
            {
                var msg = await ReplyErrorLocalized("disabled");
                msg.DeleteAfter(60);
                return;
            }
            
            var key = Service.ValidationKeys.FirstOrDefault(vk => vk.KeyScope == VerificationService.KeyScope.Discord && vk.DiscordUserId == Context.User.Id && vk.GuildId == Context.Guild.Id && vk.Key == discordkey);
            if (key == null) return;
            using (var uow = _db.UnitOfWork) {
                uow.VerificatedUser.SetVerified(Context.Guild.Id, Context.User.Id, key.ForumUserId);
                var roleid = Service.GetVerifiedRoleId(Context.Guild.Id);
                var role = roleid != null ? Context.Guild.GetRole(roleid.Value) : null;
                if (role != null && Context.User is IGuildUser guildUser) await guildUser.AddRoleAsync(role);
                await uow.CompleteAsync();
            }
            Service.ValidationKeys.TryRemove(key);
            var ch = await Context.User.GetOrCreateDMChannelAsync();
            await ch.SendMessageAsync("Verification complete (WIP)");
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task RemoveValidation(IUser user) {
            if (user == null) return;
            using (var uow = _db.UnitOfWork) {
                var msg = uow.VerificatedUser.RemoveVerification(Context.Guild.Id, user.Id) ? await ConfirmLocalized("removed_discord", user.Username) : await ErrorLocalized("removed_discord_fail", user.Username);
                //msg.DeleteAfter(60);
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
            var roleid = Service.GetVerifiedRoleId(Context.Guild.Id);
            if (roleid == role?.Id) return;
            await Service.SetVerifiedRole(Context.Guild.Id, role?.Id);
            await (role == null ? ConfirmLocalized("role_not_set") : ConfirmLocalized("role_set_not_null", role.Name));
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task VerifiedRole() {
            var roleid = Service.GetVerifiedRoleId(Context.Guild.Id);
            IUserMessage msg;
            if (roleid == null) msg = await ConfirmLocalized("role_not_set");
            else {
                var role = Context.Guild.GetRole(roleid.Value);
                msg = role == null ? await ErrorLocalized("role_not_found", roleid.Value) : await ConfirmLocalized("role", role.Name);
            }
            //msg.DeleteAfter(60);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task VerifyString() {
            var vs = Service.GetVerifyString(Context.Guild.Id);
            await (string.IsNullOrWhiteSpace(vs) ? ConfirmLocalized("verifystring_void") : ConfirmLocalized("verifystring", vs));
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task VerifyString([Remainder]string verifystring) {
            var old = Service.GetVerifyString(Context.Guild.Id);
            verifystring = string.Equals(verifystring, "null", StringComparison.OrdinalIgnoreCase) ? null : verifystring.Trim();
            await Service.SetVerifyString(Context.Guild.Id, verifystring);
            await ConfirmLocalized("verifystring_new", old, verifystring);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task VerificationKeys(int page = 1) {
            if (page < 1) return;

            const int keycount = 10;
            await Context.Channel.SendPaginatedMessageAsync(Context.Client as DiscordSocketClient, page - 1, p => {
                var keys = Service.ValidationKeys.Where(k => k.GuildId == Context.Guild.Id).Skip(p * keycount).Take(keycount);
                var sb = new StringBuilder($"```Verifizierungsschlüssel\n{"Discorduser",-32} | {"ForumId", -8} | {"Key", -32} | {"Scope", -8}\n---------------------------------|----------|----------------------------------|---------\n");
                var keystrings = (from key in keys
                    let user = Context.Guild.GetUserAsync(key.DiscordUserId).GetAwaiter().GetResult()
                    let discordname = string.IsNullOrWhiteSpace(user.Nickname) ? string.IsNullOrWhiteSpace(user.Username) ? user.Id.ToString() : user.Username : user.Nickname
                    select $"{discordname,-32} | {key.ForumUserId,-8} | {key.Key,-32} | {key.KeyScope,-8}").ToList();
                sb.Append(string.Join("\n", keystrings));
                sb.Append("```");
                return sb.ToString();
            }, Service.ValidationKeys.Count / keycount);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task VerifiedUsers(int page = 1) {
            if (page < 1) return;

            const int usercount = 20;
            await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page - 1, p => {
                var vus = Service.GetVerifiedUsers(Context.Guild.Id).Skip(p * usercount).Take(usercount);
                var embed = new EmbedBuilder().WithOkColor().WithTitle("Verifizierte Nutzer");
                foreach (var vu in vus) {
                    var user = Context.Guild.GetUserAsync(vu.UserId).GetAwaiter().GetResult();
                    embed.AddField(efb => efb.WithName((user?.Username ?? vu.UserId.ToString()).TrimTo(24, true)).WithValue(vu.ForumUserId).WithIsInline(true));
                }
                return embed;
            }, Service.GetVerifiedUserCount(Context.Guild.Id) / usercount);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task HowToVerify(bool dm = true, bool delete = true) {
            delete = !_creds.IsOwner(Context.User) || delete;
            var text = Service.GetVerificationTutorialText(Context.Guild.Id);
            if (string.IsNullOrWhiteSpace(text)) {
                (await Context.Channel.SendErrorAsync("Keine Verifizierungsanleitung verfügbar")).DeleteAfter(60);
                return;
            }
            var ch = dm ? await Context.User.GetOrCreateDMChannelAsync() : Context.Channel;
            var msg = await ch.SendConfirmAsync("Verifizierungsanleitung", text);
            if (delete && !dm) msg.DeleteAfter(120);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task SetVerifyTutorialText([Remainder] string text) {
            await Service.SetVerificationTutorialText(Context.Guild.Id, text);
            (await Context.Channel.SendConfirmAsync("Der Verifizierungstutorialtext wurde gesetzt!")).DeleteAfter(60);
        }
    }
}