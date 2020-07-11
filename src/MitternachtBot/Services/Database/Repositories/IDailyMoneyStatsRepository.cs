using System;
using System.Collections.Generic;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories {
	public interface IDailyMoneyStatsRepository : IRepository<DailyMoneyStats> {
		void Add(ulong userId, DateTime timeReceived, long amount);
		List<DailyMoneyStats> GetAllUser(params ulong[] userIds);
		void RemoveAll(ulong userId);
	}
}