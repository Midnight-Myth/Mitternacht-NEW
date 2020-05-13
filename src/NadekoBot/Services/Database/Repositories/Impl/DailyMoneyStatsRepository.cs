using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;
using System.Linq.Expressions;

namespace Mitternacht.Services.Database.Repositories.Impl
{
    public class DailyMoneyStatsRepository : Repository<DailyMoneyStats>, IDailyMoneyStatsRepository
    {
        public DailyMoneyStatsRepository(DbContext context) : base(context){}

        public void Add(ulong userId, DateTime timeReceived, long amount)
        {
            _set.Add(new DailyMoneyStats
            {
                UserId = userId,
                TimeReceived = timeReceived,
                MoneyReceived = amount
            });
            _context.SaveChanges();
        }

        public List<DailyMoneyStats> GetAllUser(params ulong[] userIds)
            => _set.Where((Expression<Func<DailyMoneyStats, bool>>)(dms => userIds.Contains(dms.UserId))).ToList();

        public void RemoveAll(ulong userId)
        {
            _set.RemoveRange(GetAllUser(userId));
            _context.SaveChanges();
        }
    }
}