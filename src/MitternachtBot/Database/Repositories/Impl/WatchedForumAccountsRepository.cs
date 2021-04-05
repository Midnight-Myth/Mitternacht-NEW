using Mitternacht.Database.Models;
using System.Linq;

namespace Mitternacht.Database.Repositories.Impl {
	public class WatchedForumAccountsRepository : Repository<WatchedForumAccount>, IWatchedForumAccountsRepository {
		public WatchedForumAccountsRepository(MitternachtContext context) : base(context) { }

		public bool Create(ulong guildId, long forumUserId, WatchAction watchAction) {
			if(_set.FirstOrDefault(wfa => wfa.GuildId == guildId && wfa.ForumUserId == forumUserId) is null) {
				_set.Add(new WatchedForumAccount {
					GuildId = guildId,
					ForumUserId = forumUserId,
					WatchAction = watchAction,
				});

				return true;
			} else {
				return false;
			}
		}

		public IQueryable<WatchedForumAccount> GetForGuild(ulong guildId)
			=> _set.AsQueryable().Where(m => m.GuildId == guildId);

		public bool ChangeWatchAction(ulong guildId, long forumUserId, WatchAction watchAction) {
			var wfa = GetForGuild(guildId).FirstOrDefault(m => m.ForumUserId == forumUserId);

			if(wfa is not null){
				wfa.WatchAction = watchAction;

				return true;
			} else {
				return false;
			}
		}
	}
}
