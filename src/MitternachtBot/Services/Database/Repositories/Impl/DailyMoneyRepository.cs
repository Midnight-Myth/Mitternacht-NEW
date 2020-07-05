using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories.Impl
{
    public class DailyMoneyRepository : Repository<DailyMoney>, IDailyMoneyRepository
    {
        public DailyMoneyRepository(DbContext context) : base(context)
        {
        }

        public DailyMoney GetOrCreate(ulong userId)
        {
            var cur = _set.FirstOrDefault(c => c.UserId == userId);

            if (cur != null) return cur;
            _set.Add(cur = new DailyMoney
            {
                UserId = userId,
                LastTimeGotten = DateTime.MinValue
            });
            return cur;
        }

        public DateTime GetUserDate(ulong userId) 
            => GetOrCreate(userId).LastTimeGotten;

        public bool CanReceive(ulong userId) 
            => GetOrCreate(userId).LastTimeGotten.Date < DateTime.Today.Date;

        public bool TryUpdateState(ulong userId)
        {
            var dm = GetOrCreate(userId);
            if (dm.LastTimeGotten.Date >= DateTime.Today.Date) return false;
            dm.LastTimeGotten = DateTime.Now;
            _set.Update(dm);
            return true;
        }

        public bool TryResetReceived(ulong userId)
        {
            var dm = GetOrCreate(userId);
            if (dm.LastTimeGotten.Date < DateTime.Today.Date) return false;
            dm.LastTimeGotten = DateTime.Today.AddDays(-1);
            _set.Update(dm);
            return true;
        }
    }
}
