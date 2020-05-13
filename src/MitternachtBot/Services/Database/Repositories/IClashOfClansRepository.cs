using System.Collections.Generic;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories
{
    public interface IClashOfClansRepository : IRepository<ClashWar>
    {
        IEnumerable<ClashWar> GetAllWars(List<long> guilds);
    }
}
