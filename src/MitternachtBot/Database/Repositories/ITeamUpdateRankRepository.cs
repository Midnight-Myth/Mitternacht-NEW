using Mitternacht.Database.Models;
using System.Linq;

namespace Mitternacht.Database.Repositories {
	public interface ITeamUpdateRankRepository : IRepository<TeamUpdateRank> {
		bool AddRank(ulong guildId, string rank, string prefix);
		bool UpdateMessagePrefix(ulong guildId, string rank, string prefix);
		bool DeleteRank(ulong guildId, string rank);
		IQueryable<TeamUpdateRank> ForGuild(ulong guildId);
	}
}
