﻿namespace Mitternacht.Database.Models {
	public class CurrencyTransaction : DbEntity {
		public long   Amount { get; set; }
		public string Reason { get; set; }
		public ulong  GuildId { get; set; }
		public ulong  UserId { get; set; }
	}
}
