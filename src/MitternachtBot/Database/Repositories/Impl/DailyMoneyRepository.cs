using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories.Impl {
	public class DailyMoneyRepository : Repository<DailyMoney>, IDailyMoneyRepository {
		public DailyMoneyRepository(DbContext context) : base(context) { }

		public DailyMoney GetOrCreate(ulong guildId, ulong userId) {
			var dm = _set.FirstOrDefault(c => c.GuildId == guildId && c.UserId == userId);

			if(dm == null) {
				_set.Add(dm = new DailyMoney {
					GuildId = guildId,
					UserId = userId,
					LastTimeGotten = DateTime.MinValue
				});
			}

			return dm;
		}

		public DateTime GetLastReceived(ulong guildId, ulong userId)
			=> _set.FirstOrDefault(c => c.GuildId == guildId && c.UserId == userId)?.LastTimeGotten ?? DateTime.MinValue;

		public bool CanReceive(ulong guildId, ulong userId)
			=> GetLastReceived(guildId, userId).Date < DateTime.Today.Date;

		public DateTime UpdateState(ulong guildId, ulong userId) {
			var dm = GetOrCreate(guildId, userId);
			dm.LastTimeGotten = DateTime.Now;

			return dm.LastTimeGotten;
		}

		public void ResetLastTimeReceived(ulong guildId, ulong userId) {
			if(!CanReceive(guildId, userId)) {
				var dm = GetOrCreate(guildId, userId);

				if(dm.LastTimeGotten.Date >= DateTime.Today.Date) {
					dm.LastTimeGotten = DateTime.Today.AddDays(-1);
				}
			}
		}
	}
}
