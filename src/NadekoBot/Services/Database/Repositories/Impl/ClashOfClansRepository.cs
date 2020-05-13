using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;
using System;
using System.Linq.Expressions;

namespace Mitternacht.Services.Database.Repositories.Impl
{
    public class ClashOfClansRepository : Repository<ClashWar>, IClashOfClansRepository
    {
        public ClashOfClansRepository(DbContext context) : base(context)
        {
        }

        public IEnumerable<ClashWar> GetAllWars(List<long> guilds)
        {
            var toReturn =  _set
                .Where((Expression<Func<ClashWar, bool>>)(cw => guilds.Contains((long) cw.GuildId)))
                .Include(cw => cw.Bases)
                        .ToList();
            toReturn.ForEach(cw => cw.Bases = cw.Bases.Where(w => w.SequenceNumber != null).OrderBy(w => w.SequenceNumber).ToList());
            return toReturn;
        }
    }
}
