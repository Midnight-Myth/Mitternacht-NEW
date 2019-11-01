using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;
using System;
using System.Linq.Expressions;

namespace Mitternacht.Services.Database.Repositories.Impl
{
    public class VerifiedUserRepository : Repository<VerifiedUser>, IVerifiedUserRepository
    {
        public VerifiedUserRepository(DbContext context) : base(context)
        {
        }

        public bool SetVerified(ulong guildId, ulong userId, long forumUserId) {
            if (!CanVerifyForumAccount(guildId, userId, forumUserId))
                return false;
            var vu = _set.FirstOrDefault(v => v.GuildId == guildId && v.UserId == userId);
            if (vu == null) {
                _set.Add(new VerifiedUser {
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
            return true;
        }

        public bool IsDiscordUserVerified(ulong guildId, ulong userId) 
            => _set.Any(v => v.GuildId == guildId && v.UserId == userId);

        public bool IsForumUserVerified(ulong guildId, long forumUserId) 
            => _set.Any(v => v.GuildId == guildId && v.ForumUserId == forumUserId);

        public bool IsVerified(ulong guildId, ulong userId, long forumUserId)
            => _set.Any(v => v.GuildId == guildId && v.UserId == userId && v.ForumUserId == forumUserId);

        public bool CanVerifyForumAccount(ulong guildId, ulong userId, long forumUserId) {
            return !IsForumUserVerified(guildId, forumUserId) && !IsVerified(guildId, userId, forumUserId) || !IsDiscordUserVerified(guildId, userId);
        }

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

        public IEnumerable<VerifiedUser> GetVerifiedUsers(ulong guildId) {
            return _set.Where((Expression<Func<VerifiedUser, bool>>)(v => v.GuildId == guildId));
        }

        public long? GetVerifiedUserForumId(ulong guildId, ulong userId) 
            => _set.FirstOrDefault(vu => vu.GuildId == guildId && vu.UserId == userId)?.ForumUserId;

        public ulong? GetVerifiedUserId(ulong guildId, long forumUserId)
            => _set.FirstOrDefault(vu => vu.GuildId == guildId && vu.ForumUserId == forumUserId)?.UserId;

        public int GetCount(ulong guildId) {
            return _set.Count(v => v.GuildId == guildId);
        }
    }
}