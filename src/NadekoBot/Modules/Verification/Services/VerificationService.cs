using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Mitternacht.Common.Collections;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Verification.Services
{
    public class VerificationService : INService
    {
        private readonly DbService _db;

        private readonly Random _rnd = new Random();
        public readonly ConcurrentHashSet<ValidationKey> ValidationKeys = new ConcurrentHashSet<ValidationKey>();

        //public event Func<SocketGuildUser, UserInfo, EventTrigger> UserVerified;
        //public event Func<SocketGuildUser, UserInfo, EventTrigger> UserUnverified;

        private const int VerificationKeyValidationTime = 3600000;

        public VerificationService(DbService db) {
            _db = db;
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