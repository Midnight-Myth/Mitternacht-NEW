using System;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories {
	public interface IDailyMoneyRepository : IRepository<DailyMoney> {
		DailyMoney GetOrCreate(ulong userId);
		DateTime GetLastReceived(ulong userId);
		bool CanReceive(ulong userId);
		DateTime UpdateState(ulong userId);
		void ResetLastTimeReceived(ulong userId);
	}
}
