using NadekoBot.Services.Database.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System;

namespace NadekoBot.Services.Database.Repositories.Impl
{
    public class DailyMoneyRepository : Repository<DailyMoney>, IDailyMoneyRepository
    {
        public DailyMoneyRepository(DbContext context) : base(context)
        {
        }

        public DailyMoney GetOrCreate(ulong userId)
        {
            var cur = _set.FirstOrDefault(c => c.UserId == userId);

            if (cur == null)
            {
                _set.Add(cur = new DailyMoney()
                {
                    UserId = userId,
                    LastTimeGotten = DateTime.Today.AddDays(-1)
                });
                _context.SaveChanges();
            }
            return cur;
        }

        public DateTime GetUserDate(ulong userId) =>
            GetOrCreate(userId).LastTimeGotten;

        public bool TryUpdateState(ulong userId, DateTime LastTimeGotten)
        {
            var dailymoney = _set.Where(d => d.UserId == userId).FirstOrDefault();

            dailymoney.LastTimeGotten = DateTime.Today;
            _set.Update(dailymoney);
            return true;
        }
    }
}
