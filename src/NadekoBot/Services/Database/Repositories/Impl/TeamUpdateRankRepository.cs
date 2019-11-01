using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Mitternacht.Services.Database.Repositories.Impl
{
    public class TeamUpdateRankRepository : Repository<TeamUpdateRank>, ITeamUpdateRankRepository
    {
        public TeamUpdateRankRepository(DbContext context) : base(context) {}

        public bool AddRank(ulong guildId, string rank)
        {
            if (_set.FirstOrDefault(tur => tur.GuildId == guildId && tur.Rankname.Equals(rank, StringComparison.OrdinalIgnoreCase)) != null) return false;
            
            _set.Add(new TeamUpdateRank
            {
                GuildId = guildId,
                Rankname = rank
            });
            return true;
        }

        public bool DeleteRank(ulong guildId, string rank)
        {
            var teamrank = _set.FirstOrDefault(tur => tur.GuildId == guildId && tur.Rankname.Equals(rank, StringComparison.OrdinalIgnoreCase));
            if (teamrank == null) return false;
            _set.Remove(teamrank);
            return true;
        }

        public List<string> GetGuildRanks(ulong guildId)
            => _set.Where((Expression<Func<TeamUpdateRank, bool>>) (tur => tur.GuildId == guildId)).Select(tur => tur.Rankname).ToList();
    }
}
