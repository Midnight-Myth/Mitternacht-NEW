using Mitternacht.Services.Database.Models;
using System.Collections.Generic;

namespace Mitternacht.Services.Database.Repositories {
	public interface ITeamUpdateRankRepository : IRepository<TeamUpdateRank> {
		bool AddRank(ulong guildId, string rank, string prefix);
		bool UpdateMessagePrefix(ulong guildId, string rank, string prefix);
		bool DeleteRank(ulong guildId, string rank);
		List<TeamUpdateRank> GetGuildRanks(ulong guildId);
	}
}
