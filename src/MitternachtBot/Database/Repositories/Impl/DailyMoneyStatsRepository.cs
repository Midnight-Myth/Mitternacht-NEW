using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories.Impl {
	public class DailyMoneyStatsRepository : Repository<DailyMoneyStats>, IDailyMoneyStatsRepository {
		public DailyMoneyStatsRepository(MitternachtContext context) : base(context) { }

		public void Add(ulong guildId, ulong userId, DateTime timeReceived, long amount) {
			_set.Add(new DailyMoneyStats {
				GuildId = guildId,
				UserId = userId,
				TimeReceived = timeReceived,
				MoneyReceived = amount,
			});
		}

		public IQueryable<DailyMoneyStats> GetAllForUsers(ulong guildId, params ulong[] userIds)
			=> _set.AsQueryable().Where(dms => dms.GuildId == guildId && userIds.Contains(dms.UserId));
	}
}