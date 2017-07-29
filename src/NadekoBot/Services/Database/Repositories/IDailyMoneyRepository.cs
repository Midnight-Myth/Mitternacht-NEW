using NadekoBot.Services.Database.Models;
using System;

namespace NadekoBot.Services.Database.Repositories
{
    public interface IDailyMoneyRepository : IRepository<DailyMoney>
    {
        DailyMoney GetOrCreate(ulong userId);
        DateTime GetUserDate(ulong userId);
        bool TryUpdateState(ulong userId);
        bool TryResetReceived(ulong userId);
    }
}
