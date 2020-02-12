using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;
using System.Linq.Expressions;

namespace Mitternacht.Services.Database.Repositories.Impl {
	public class UsernameHistoryRepository : Repository<UsernameHistoryModel>, IUsernameHistoryRepository
    {
        public UsernameHistoryRepository(DbContext context) : base(context) { }

        public bool AddUsername(ulong userId, string username, ushort discriminator) {
            if (string.IsNullOrWhiteSpace(username)) return false;

            username = username.Trim();
            var current = GetUsernamesDescending(userId).FirstOrDefault();
            var now = DateTime.UtcNow;
            if (current != null)
            {
                if (string.Equals(current.Name, username, StringComparison.Ordinal) && current.DiscordDiscriminator == discriminator)
                {
                    if (!current.DateReplaced.HasValue) return false;
                    current.DateReplaced = null;
                    _set.Update(current);
                    return false;
                }

                if (!current.DateReplaced.HasValue)
                {
                    current.DateReplaced = now;
                    _set.Update(current);
                }
            }

            _set.Add(new UsernameHistoryModel {
                UserId = userId,
                Name = username,
                DiscordDiscriminator = discriminator,
                DateSet = now
            });
            return true;
        }

		public IOrderedQueryable<UsernameHistoryModel> GetUsernamesDescending(ulong userId)
            => _set.Where((Expression<Func<UsernameHistoryModel, bool>>)(u => u.UserId == userId && !(u is NicknameHistoryModel))).OrderByDescending(u => u.DateSet);

		public string GetLastUsername(ulong userId)
			=> GetUsernamesDescending(userId).FirstOrDefault()?.Name;

	}
}