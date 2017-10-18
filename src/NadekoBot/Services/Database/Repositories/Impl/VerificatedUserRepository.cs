using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories.Impl
{
    public class VerificatedUserRepository : Repository<VerificatedUser>, IVerificatedUserRepository
    {
        public VerificatedUserRepository(DbContext context) : base(context)
        {
        }

        public void SetVerified(ulong guildId, ulong userId, long forumUserId) {
            var vu = _set.FirstOrDefault(v => v.GuildId == guildId && v.UserId == userId);
            if (vu == null) {
                _set.Add(new VerificatedUser {
                    GuildId = guildId,
                    UserId = userId,
                    ForumUserId = forumUserId
                });
            }
            else {
                vu.ForumUserId = forumUserId;
                _set.Update(vu);
            }
            _context.SaveChanges();
        }

        public bool IsDiscordUserVerified(ulong guildId, ulong userId) 
            => _set.Any(v => v.GuildId == guildId && v.UserId == userId);

        public bool IsForumUserVerified(ulong guildId, long forumUserId) 
            => _set.Any(v => v.GuildId == guildId && v.ForumUserId == forumUserId);

        public bool IsVerified(ulong guildId, ulong userId, long forumUserId)
            => _set.Any(v => v.GuildId == guildId && v.UserId == userId && v.ForumUserId == forumUserId);

        public bool IsForumUserIndependentFromDiscordUser(ulong guildId, ulong userId, long forumUserId) 
            => IsForumUserVerified(guildId, forumUserId) && IsDiscordUserVerified(guildId, userId) && !IsVerified(guildId, userId, forumUserId);

        public bool RemoveVerification(ulong guildId, ulong userId) {
            var vu = _set.FirstOrDefault(v => v.GuildId == guildId && v.UserId == userId);
            if (vu == null) return false;
            _set.Remove(vu);
            _context.SaveChanges();
            return true;
        }

        public bool RemoveVerification(ulong guildId, long forumUserId) {
            var vu = _set.FirstOrDefault(v => v.GuildId == guildId && v.ForumUserId == forumUserId);
            if (vu == null) return false;
            _set.Remove(vu);
            _context.SaveChanges();
            return true;
        }

        public IEnumerable<VerificatedUser> GetVerificatedUsers(ulong guildId) {
            return _set.Where(v => v.GuildId == guildId);
        }

        public int GetCount(ulong guildId) {
            return _set.Count(v => v.GuildId == guildId);
        }
    }
}