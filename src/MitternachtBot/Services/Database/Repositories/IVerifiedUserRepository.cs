using System.Linq;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories {
	public interface IVerifiedUserRepository : IRepository<VerifiedUser> {
		VerifiedUser GetVerifiedUser(ulong guildId, ulong userId);
		VerifiedUser GetVerifiedUser(ulong guildId, long forumUserId);
		bool SetVerified(ulong guildId, ulong userId, long forumUserId);
		bool IsVerified(ulong guildId, ulong userId);
		bool IsVerified(ulong guildId, long forumUserId);
		bool IsVerified(ulong guildId, ulong userId, long forumUserId);
		bool CanVerifyForumAccount(ulong guildId, ulong userId, long forumUserId);
		bool RemoveVerification(ulong guildId, ulong userId);
		bool RemoveVerification(ulong guildId, long forumUserId);
		IQueryable<VerifiedUser> GetVerifiedUsers(ulong guildId);
		int GetNumberOfVerificationsInGuild(ulong guildId);

		IQueryable<VerifiedUser> GetVerificationsOfUser(ulong userId);
	}
}