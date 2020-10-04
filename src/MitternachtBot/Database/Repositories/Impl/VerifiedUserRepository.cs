using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories.Impl {
	public class VerifiedUserRepository : Repository<VerifiedUser>, IVerifiedUserRepository {
		public VerifiedUserRepository(DbContext context) : base(context) { }

		public VerifiedUser GetVerifiedUser(ulong guildId, ulong userId)
			=> _set.FirstOrDefault(v => v.GuildId == guildId && v.UserId == userId);

		public VerifiedUser GetVerifiedUser(ulong guildId, long forumUserId)
			=> _set.FirstOrDefault(v => v.GuildId == guildId && v.ForumUserId == forumUserId);

		public bool SetVerified(ulong guildId, ulong userId, long forumUserId) {
			if(CanVerifyForumAccount(guildId, userId, forumUserId)) {
				var vu = GetVerifiedUser(guildId, userId);

				if(vu == null) {
					_set.Add(new VerifiedUser {
						GuildId     = guildId,
						UserId      = userId,
						ForumUserId = forumUserId,
					});
				} else {
					vu.ForumUserId = forumUserId;
				}

				return true;
			} else {
				return false;
			}
		}

		public bool IsVerified(ulong guildId, ulong userId)
			=> _set.Any(v => v.GuildId == guildId && v.UserId == userId);

		public bool IsVerified(ulong guildId, long forumUserId)
			=> _set.Any(v => v.GuildId == guildId && v.ForumUserId == forumUserId);

		public bool IsVerified(ulong guildId, ulong userId, long forumUserId)
			=> _set.Any(v => v.GuildId == guildId && v.UserId == userId && v.ForumUserId == forumUserId);


		public bool CanVerifyForumAccount(ulong guildId, ulong userId, long forumUserId)
			=> !IsVerified(guildId, forumUserId) && !IsVerified(guildId, userId, forumUserId) || !IsVerified(guildId, userId);


		public bool RemoveVerification(ulong guildId, ulong userId) {
			var vu = GetVerifiedUser(guildId, userId);
			
			if(vu != null) {
				_set.Remove(vu);

				return true;
			} else {
				return false;
			}
		}

		public bool RemoveVerification(ulong guildId, long forumUserId) {
			var vu = GetVerifiedUser(guildId, forumUserId);
			
			if(vu != null) {
				_set.Remove(vu);

				return true;
			} else {
				return false;
			}
		}


		public IQueryable<VerifiedUser> GetVerifiedUsers(ulong guildId)
			=> _set.AsQueryable().Where(v => v.GuildId == guildId);

		public int GetNumberOfVerificationsInGuild(ulong guildId)
			=> _set.Count(v => v.GuildId == guildId);


		public IQueryable<VerifiedUser> GetVerificationsOfUser(ulong userId)
			=> _set.AsQueryable().Where(v => v.UserId == userId);
	}
}