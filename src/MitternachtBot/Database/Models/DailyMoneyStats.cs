using System;

namespace Mitternacht.Database.Models {
	public class DailyMoneyStats : DbEntity {
		public ulong    GuildId       { get; set; }
		public ulong    UserId        { get; set; }
		public DateTime TimeReceived  { get; set; }
		public long     MoneyReceived { get; set; }
	}
}