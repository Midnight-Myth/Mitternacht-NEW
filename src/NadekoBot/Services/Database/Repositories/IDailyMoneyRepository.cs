using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;

namespace NadekoBot.Services.Database.Repositories
{
    public interface IDailyMoneyRepository : IRepository<DailyMoney>
    {
        DailyMoney GetOrCreate(ulong userId);
        DateTime GetUserDate(ulong userId);
        bool TryUpdateState(ulong userId, DateTime lastGotten);
    }
}
