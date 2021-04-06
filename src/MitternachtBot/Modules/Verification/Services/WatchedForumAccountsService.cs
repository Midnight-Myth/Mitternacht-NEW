using Discord;
using Mitternacht.Services;
using Mitternacht.Services.Impl;
using System.Linq;
using System.Threading.Tasks;

namespace Mitternacht.Modules.Verification.Services {
	public class WatchedForumAccountsService : IMService {
		private readonly DbService _db;
		private readonly VerificationService _vs;

		public WatchedForumAccountsService(DbService db, VerificationService vs) {
			_db = db;
			_vs = vs;

			_vs.UserVerified += UserVerified;
		}

		private async Task UserVerified(IGuildUser guildUser, long forumUserId) {
			using var uow = _db.UnitOfWork;
			var wfa = uow.WatchedForumAccounts.GetForGuild(guildUser.GuildId).FirstOrDefault(wfa => wfa.ForumUserId == forumUserId);

			if(wfa is not null) {
				switch(wfa.WatchAction) {
					case Database.Models.WatchAction.NONE:
						break;
					case Database.Models.WatchAction.NOTIFY:
						break;
					case Database.Models.WatchAction.BAN:
						await guildUser.BanAsync(reason: "Automatic ban due to forum account being watched with action BAN on verification.").ConfigureAwait(false);
						break;
					default:
						break;
				}
			}
		}
	}
}
