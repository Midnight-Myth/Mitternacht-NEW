using System;
using System.Linq;
using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories.Impl {
	public class UsernameHistoryRepository : Repository<UsernameHistoryModel>, IUsernameHistoryRepository {
		public UsernameHistoryRepository(MitternachtContext context) : base(context) { }

		public bool AddUsername(ulong userId, string username, ushort discriminator) {
			if(!string.IsNullOrWhiteSpace(username)) {
				username = username.Trim();
				var current = GetUsernamesDescending(userId).FirstOrDefault();
				var now = DateTime.UtcNow;
				if(current != null) {
					if(string.Equals(current.Name, username, StringComparison.Ordinal) && current.DiscordDiscriminator == discriminator) {
						if(current.DateReplaced.HasValue) {
							current.DateReplaced = null;
						}
						
						return false;
					}

					if(!current.DateReplaced.HasValue) {
						current.DateReplaced = now;
					}
				}

				_set.Add(new UsernameHistoryModel {
					UserId = userId,
					Name = username,
					DiscordDiscriminator = discriminator,
					DateSet = now
				});
				return true;
			} else {
				return false;
			}
		}

		public IOrderedQueryable<UsernameHistoryModel> GetUsernamesDescending(ulong userId)
			=> _set.AsQueryable().Where(u => u.UserId == userId && !(u is NicknameHistoryModel)).OrderByDescending(u => u.DateSet);

		public string GetLastUsername(ulong userId)
			=> GetUsernamesDescending(userId).FirstOrDefault()?.Name;

	}
}