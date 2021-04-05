using Mitternacht.Database.Models;
using System;
using System.Linq;

namespace Mitternacht.Database.Repositories.Impl {
	public class TeamUpdateRankRepository : Repository<TeamUpdateRank>, ITeamUpdateRankRepository {
		public TeamUpdateRankRepository(MitternachtContext context) : base(context) { }

		public bool AddRank(ulong guildId, string rank, string prefix) {
			if(rank == null)
				throw new ArgumentNullException(nameof(rank));

			if(!ForGuild(guildId).AsEnumerable().Any(tur => tur.Rankname.Equals(rank, StringComparison.OrdinalIgnoreCase))) {
				_set.Add(new TeamUpdateRank {
					GuildId       = guildId,
					Rankname      = rank,
					MessagePrefix = prefix,
				});

				return true;
			} else {
				return false;
			}
		}
		
		public bool UpdateMessagePrefix(ulong guildId, string rank, string prefix) {
			if(rank == null)
				throw new ArgumentNullException(nameof(rank));

			var teamUpdateRank = ForGuild(guildId).AsEnumerable().FirstOrDefault(tur => tur.Rankname.Equals(rank, StringComparison.OrdinalIgnoreCase));

			if(teamUpdateRank != null && !string.Equals(teamUpdateRank.MessagePrefix, prefix, StringComparison.OrdinalIgnoreCase)) {
				teamUpdateRank.MessagePrefix = prefix;
				return true;
			} else {
				return false;
			}
		}

		public bool DeleteRank(ulong guildId, string rank) {
			if(rank == null)
				throw new ArgumentNullException(nameof(rank));

			var teamUpdateRank = ForGuild(guildId).AsEnumerable().FirstOrDefault(tur => tur.Rankname.Equals(rank, StringComparison.OrdinalIgnoreCase));

			if(teamUpdateRank != null) {
				_set.Remove(teamUpdateRank);
				return true;
			} else {
				return false;
			}
		}

		public IQueryable<TeamUpdateRank> ForGuild(ulong guildId)
			=> _set.AsQueryable().Where(tur => tur.GuildId == guildId);
	}
}
