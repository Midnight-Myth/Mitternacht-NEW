using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories.Impl {
	public class DailyMoneyRepository : Repository<DailyMoney>, IDailyMoneyRepository {
		public DailyMoneyRepository(DbContext context) : base(context) { }

		public DailyMoney GetOrCreate(ulong userId) {
			var dm = _set.FirstOrDefault(c => c.UserId == userId);

			if(dm == null) {
				_set.Add(dm = new DailyMoney {
					UserId = userId,
					LastTimeGotten = DateTime.MinValue
				});
			}

			return dm;
		}

		public DateTime GetLastReceived(ulong userId)
			=> _set.FirstOrDefault(c => c.UserId == userId)?.LastTimeGotten ?? DateTime.MinValue;

		public bool CanReceive(ulong userId)
			=> GetLastReceived(userId).Date < DateTime.Today.Date;

		public DateTime UpdateState(ulong userId) {
			var dm = GetOrCreate(userId);
			dm.LastTimeGotten = DateTime.Now;

			return dm.LastTimeGotten;
		}

		public void ResetLastTimeReceived(ulong userId) {
			if(!CanReceive(userId)) {
				var dm = GetOrCreate(userId);

				if(dm.LastTimeGotten.Date >= DateTime.Today.Date) {
					dm.LastTimeGotten = DateTime.Today.AddDays(-1);
				}
			}
		}
	}
}
