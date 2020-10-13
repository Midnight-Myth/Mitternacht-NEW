using System;
using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories {
	public interface IDailyMoneyRepository : IRepository<DailyMoney> {
		DailyMoney GetOrCreate(ulong guildId, ulong userId);
		DateTime GetLastReceived(ulong guildId, ulong userId);
		bool CanReceive(ulong guildId, ulong userId, TimeZoneInfo timeZoneInfo);
		DateTime UpdateState(ulong guildId, ulong userId);
		void ResetLastTimeReceived(ulong guildId, ulong userId, TimeZoneInfo timeZoneInfo);
	}
}
