using System;

namespace Mitternacht.Database.Models {
	public class DailyMoney : DbEntity {
		public ulong    GuildId          { get; set; }
		public ulong    UserId           { get; set; }
		public DateTime LastTimeReceived { get; set; }
	}
}
