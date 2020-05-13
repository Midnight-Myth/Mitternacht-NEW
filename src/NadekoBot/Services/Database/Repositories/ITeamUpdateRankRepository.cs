using Mitternacht.Services.Database.Models;
using System.Collections.Generic;

namespace Mitternacht.Services.Database.Repositories
{
    public interface ITeamUpdateRankRepository : IRepository<TeamUpdateRank>
    {
        List<string> GetGuildRanks(ulong guildId);
        bool AddRank(ulong guildId, string rank);
        bool DeleteRank(ulong guildId, string rank);
    }
}
