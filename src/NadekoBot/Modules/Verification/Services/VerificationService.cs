using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GommeHDnetForumAPI.DataModels;
using Mitternacht.Common.Collections;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;
using Mitternacht.Services.Impl;

namespace Mitternacht.Modules.Verification.Services
{
    public class VerificationService : INService
    {
        private readonly DbService _db;
        private readonly ForumService _fs;
        private readonly DiscordSocketClient _client;

        private readonly Random _rnd = new Random();
        public readonly ConcurrentHashSet<ValidationKey> ValidationKeys = new ConcurrentHashSet<ValidationKey>();

        //public event Func<SocketGuildUser, UserInfo, EventTrigger> UserVerified;
        //public event Func<SocketGuildUser, UserInfo, EventTrigger> UserUnverified;

        private const int VerificationKeyValidationTime = 60 * 60 * 1000;

        public VerificationService(DbService db, ForumService fs, DiscordSocketClient client) {
            _db = db;
            _fs = fs;
            _client = client;

            var timer = new Timer(5 * 60 * 1000);
            timer.Elapsed += (s, args) => Task.Run(async () => await CheckGommeTeamMembers().ConfigureAwait(false));
            timer.Start();
        }

        private string InternalGenerateKey() {
            var bytes = new byte[8];
            _rnd.NextBytes(bytes);
            return Convert.ToBase64String(bytes, Base64FormattingOptions.None);
        }

        public ValidationKey GenerateKey(KeyScope keyscope, long forumuserid, ulong userid, ulong guildid) {
            ValidationKey key;
            while (ValidationKeys.Contains(key = new ValidationKey(InternalGenerateKey(), keyscope, forumuserid, userid, guildid))) { }
            Task.Run(async () => {
                await Task.Delay(VerificationKeyValidationTime);
                ValidationKeys.TryRemove(key);
            });
            ValidationKeys.Add(key);
            return key;
        }

        public IEnumerable<VerifiedUser> GetVerifiedUsers(ulong guildId) {
            using (var uow = _db.UnitOfWork)
                return uow.VerifiedUsers.GetVerifiedUsers(guildId).ToList();
        }

        public int GetVerifiedUserCount(ulong guildId) {
            using (var uow = _db.UnitOfWork)
                return uow.VerifiedUsers.GetCount(guildId);
        }

        public bool CanVerifyForumAccount(ulong guildId, ulong userId, long forumUserId) {
            using (var uow = _db.UnitOfWork)
                return uow.VerifiedUsers.CanVerifyForumAccount(guildId, userId, forumUserId);
        }

        public async Task SetVerifiedRole(ulong guildId, ulong? roleId)
        {
            using (var uow = _db.UnitOfWork)
            {
                uow.GuildConfigs.For(guildId, set => set).VerifiedRoleId = roleId;
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        public ulong? GetVerifiedRoleId(ulong guildId)
        {
            using (var uow = _db.UnitOfWork)
            {
                return uow.GuildConfigs.For(guildId, set => set).VerifiedRoleId;
            }
        }

        public async Task SetVerifyString(ulong guildId, string verifystring)
        {
            using (var uow = _db.UnitOfWork) {
                uow.GuildConfigs.For(guildId, set => set).VerifyString = verifystring;
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        public string GetVerifyString(ulong guildId)
        {
            using (var uow = _db.UnitOfWork) return uow.GuildConfigs.For(guildId, set => set).VerifyString;
        }

        public string GetVerificationTutorialText(ulong guildId) {
            using (var uow = _db.UnitOfWork) return uow.GuildConfigs.For(guildId, set => set).VerificationTutorialText;
        }

        public async Task SetVerificationTutorialText(ulong guildId, string text) {
            using (var uow = _db.UnitOfWork) {
                uow.GuildConfigs.For(guildId, set => set).VerificationTutorialText = text;
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        public async Task<bool> SetVerified(IGuild guild, IGuildUser user, long forumUserId) {
            using (var uow = _db.UnitOfWork) {
                if (!uow.VerifiedUsers.SetVerified(guild.Id, user.Id, forumUserId)) return false;
                var roleid = GetVerifiedRoleId(guild.Id);
                var role = roleid != null ? guild.GetRole(roleid.Value) : null;
                if (role != null) await user.AddRoleAsync(role).ConfigureAwait(false);
                await uow.CompleteAsync().ConfigureAwait(false);
                return true;
            }
        }

        public string[] GetAdditionalVerificationUsers(ulong guildId) {
            using (var uow = _db.UnitOfWork) {
                var gc = uow.GuildConfigs.For(guildId, set => set);
                return string.IsNullOrWhiteSpace(gc.AdditionalVerificationUsers) ? new string[0] : gc.AdditionalVerificationUsers.Split(',');
            }
        }

        public async Task SetAdditionalVerificationUsers(ulong guildId, string[] users) {
            using (var uow = _db.UnitOfWork) {
                var gc = uow.GuildConfigs.For(guildId, set => set);
                gc.AdditionalVerificationUsers = string.Join(',', users);
                uow.GuildConfigs.Update(gc);
                await uow.CompleteAsync();
            }
        }

        private async Task CheckGommeTeamMembers()
        {
            if (!_fs.LoggedIn) return;
            var staffIds = (await _fs.Forum.GetMembersList(MembersListType.Staff).ConfigureAwait(false)).Select(ui => ui.Id).ToList();
            using (var uow = _db.UnitOfWork)
            {
                var gcs = uow.GuildConfigs.GetAllGuildConfigs(_client.Guilds.Select(sg => sg.Id).ToList());
                foreach (var gc in gcs)
                {
                    if (gc.GommeTeamMemberRoleId == null) continue;
                    var guild = _client.Guilds.First(sg => sg.Id == gc.GuildId);
                    var gommeTeamRole = guild.GetRole(gc.GommeTeamMemberRoleId.Value);
                    if (gommeTeamRole == null) continue;
                    var verifiedUsers = uow.VerifiedUsers.GetVerifiedUsers(gc.GuildId).Select(vu => (ForumUserId: vu.ForumUserId, User: guild.GetUser(vu.UserId))).ToList();
                    foreach (var (_, user) in verifiedUsers.Where(a => a.User.Roles.Any(r => r.Id == gc.GommeTeamMemberRoleId) && !staffIds.Contains(a.ForumUserId)))
                    {
                        await user.RemoveRoleAsync(gommeTeamRole).ConfigureAwait(false);
                    }

                    foreach (var (_, user) in verifiedUsers.Where(a => staffIds.Contains(a.ForumUserId)))
                    {
                        await user.AddRoleAsync(gommeTeamRole).ConfigureAwait(false);
                    }
                }
            }
        }

        public class ValidationKey
        {
            public string Key { get; }
            public KeyScope KeyScope { get; }
            public long ForumUserId { get; }
            public ulong DiscordUserId { get; }
            public ulong GuildId { get; }
            public DateTime CreatedAt { get; }

            public ValidationKey(string key, KeyScope keyscope, long forumuserid, ulong userid, ulong guildid) {
                Key = key;
                KeyScope = keyscope;
                ForumUserId = forumuserid;
                DiscordUserId = userid;
                GuildId = guildid;
                CreatedAt = DateTime.UtcNow;
            }

            public override bool Equals(object obj) {
                return obj is ValidationKey vk && Equals(vk);
            }

            protected bool Equals(ValidationKey other) {
                return string.Equals(Key, other.Key) && KeyScope == other.KeyScope && ForumUserId == other.ForumUserId && DiscordUserId == other.DiscordUserId && GuildId == other.GuildId && CreatedAt == other.CreatedAt;
            }

            public override int GetHashCode() {
                unchecked {
                    var hashCode = Key != null ? Key.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^ (int) KeyScope;
                    hashCode = (hashCode * 397) ^ ForumUserId.GetHashCode();
                    hashCode = (hashCode * 397) ^ DiscordUserId.GetHashCode();
                    hashCode = (hashCode * 397) ^ GuildId.GetHashCode();
                    hashCode = (hashCode * 397) ^ CreatedAt.GetHashCode();
                    return hashCode;
                }
            }
        }

        public enum KeyScope
        {
            Forum, Discord
        }

        public enum EventTrigger
        {
            User, Admin
        }
    }
}