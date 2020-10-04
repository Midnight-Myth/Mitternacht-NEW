using System;

namespace Mitternacht.Services.Database.Models {
	public class DailyMoneyStats : DbEntity {
		public ulong    GuildId       { get; set; }
		public ulong    UserId        { get; set; }
		public DateTime TimeReceived  { get; set; }
		public long     MoneyReceived { get; set; }
	}
}