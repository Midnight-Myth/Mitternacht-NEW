using Mitternacht.Database.Models;
using System.Linq;

namespace Mitternacht.Database.Repositories {
	public interface IWatchedForumAccountsRepository : IRepository<WatchedForumAccount> {
		bool Create(ulong guildId, long forumUserId, WatchAction watchAction);
		IQueryable<WatchedForumAccount> GetForGuild(ulong guildId);
	}
}
