using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories.Impl {
	public class WatchedForumAccountsRepository : Repository<WatchedForumAccount>, IWatchedForumAccountsRepository {
		public WatchedForumAccountsRepository(MitternachtContext context) : base(context) { }
	}
}
