using System;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories {
	public interface IDailyMoneyRepository : IRepository<DailyMoney> {
		DailyMoney GetOrCreate(ulong guildId, ulong userId);
		DateTime GetLastReceived(ulong guildId, ulong userId);
		bool CanReceive(ulong guildId, ulong userId);
		DateTime UpdateState(ulong guildId, ulong userId);
		void ResetLastTimeReceived(ulong guildId, ulong userId);
	}
}
