using System;

namespace Mitternacht.Services.Database.Models {
	public class GuildRepeater : DbEntity {
		public ulong     GuildId        { get; set; }
		public ulong     ChannelId      { get; set; }
		public string    Message        { get; set; }
		public TimeSpan  Interval       { get; set; }
		public TimeSpan? StartTimeOfDay { get; set; }
	}
}
