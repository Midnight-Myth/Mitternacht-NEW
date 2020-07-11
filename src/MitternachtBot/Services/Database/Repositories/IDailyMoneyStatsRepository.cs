using System;
using System.Linq;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories {
	public interface IDailyMoneyStatsRepository : IRepository<DailyMoneyStats> {
		void Add(ulong userId, DateTime timeReceived, long amount);
		IQueryable<DailyMoneyStats> GetAllForUsers(params ulong[] userIds);
	}
}