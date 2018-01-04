using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories.Impl
{
    public class UsernameHistoryRepository : Repository<UsernameHistoryModel>, IUsernameHistoryRepository
    {
        public UsernameHistoryRepository(DbContext context) : base(context) { }

        public bool AddUsername(ulong guildId, ulong userId, string username, bool isNick) {
            if (string.IsNullOrWhiteSpace(username)) return false;

            var current = _set.Where(u => u.UserId == userId).OrderByDescending(u => u.DateSet).FirstOrDefault();
            if (current != null && string.Equals(current.Name, username, StringComparison.Ordinal) && current.IsNickname == isNick) return false;

            var now = DateTime.UtcNow;
            if (current != null && !current.DateReplaced.HasValue) {
                current.DateReplaced = now;
                _set.Update(current);
            }

            _set.Add(new UsernameHistoryModel {
                GuildId = guildId,
                UserId = userId,
                Name = username,
                IsNickname = isNick,
                DateSet = now
            });
            return true;
        }

        public IEnumerable<UsernameHistoryModel> GetGuildNames(ulong guildId) 
            => _set.Where(u => u.GuildId == guildId).OrderByDescending(u => u.DateSet);

        public IEnumerable<UsernameHistoryModel> GetGuildUserNames(ulong guildId, ulong userId)
            => _set.Where(u => u.GuildId == guildId && u.UserId == userId).OrderByDescending(u => u.DateSet);

        public IEnumerable<UsernameHistoryModel> GetUserNames(ulong userId)
            => _set.Where(u => u.UserId == userId).OrderByDescending(u => u.DateSet);
    }
}