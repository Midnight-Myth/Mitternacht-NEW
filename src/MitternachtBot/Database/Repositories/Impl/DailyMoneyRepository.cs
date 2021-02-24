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

		public IQueryable<DailyMoney> ForGuild(ulong guildId)
			=> _set.AsQueryable().Where(dm => dm.GuildId == guildId);

		public DateTime GetLastReceived(ulong guildId, ulong userId)
			=> _set.FirstOrDefault(c => c.GuildId == guildId && c.UserId == userId)?.LastTimeGotten ?? DateTime.MinValue;

		private static bool CanReceive(DateTime lastReceived, TimeZoneInfo timeZoneInfo) {
			lastReceived = TimeZoneInfo.ConvertTimeFromUtc(lastReceived, timeZoneInfo ?? TimeZoneInfo.Utc).Date;
			var currentTimeZoneDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo ?? TimeZoneInfo.Utc).Date;

			return lastReceived < currentTimeZoneDate;
		}

		public bool CanReceive(ulong guildId, ulong userId, TimeZoneInfo timeZoneInfo)
			=> CanReceive(GetLastReceived(guildId, userId), timeZoneInfo);

		public DateTime UpdateState(ulong guildId, ulong userId) {
			var dm = GetOrCreate(guildId, userId);
			dm.LastTimeGotten = DateTime.UtcNow;

			return dm.LastTimeGotten;
		}

		private static void ResetLastTimeReceived(DailyMoney dailyMoney, TimeZoneInfo timeZoneInfo) {
			if(!CanReceive(dailyMoney.LastTimeGotten, timeZoneInfo) && dailyMoney.LastTimeGotten.Date >= DateTime.Today.Date) {
				dailyMoney.LastTimeGotten = DateTime.Today.AddDays(-1);
			}
		}

		public void ResetLastTimeReceived(ulong guildId, ulong userId, TimeZoneInfo timeZoneInfo)
			=> ResetLastTimeReceived(GetOrCreate(guildId, userId), timeZoneInfo);

		public void ResetLastTimeReceivedForGuild(ulong guildId, TimeZoneInfo timeZoneInfo) {
			foreach(var dm in ForGuild(guildId)) {
				ResetLastTimeReceived(dm, timeZoneInfo);
			}
		}
	}
}
