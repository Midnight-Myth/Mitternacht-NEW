using System;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories {
	public interface IDailyMoneyRepository : IRepository<DailyMoney> {
		DailyMoney GetOrCreate(ulong userId);
		DateTime GetUserDate(ulong userId);
		bool CanReceive(ulong userId);
		bool TryUpdateState(ulong userId);
		bool TryResetReceived(ulong userId);
	}
}
