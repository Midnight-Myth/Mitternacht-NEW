using System;
using System.Linq;
using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories {
	public interface IDailyMoneyStatsRepository : IRepository<DailyMoneyStats> {
		void Add(ulong guildId, ulong userId, DateTime timeReceived, long amount);
		IQueryable<DailyMoneyStats> GetAllForUsers(ulong guildId, params ulong[] userIds);
	}
}