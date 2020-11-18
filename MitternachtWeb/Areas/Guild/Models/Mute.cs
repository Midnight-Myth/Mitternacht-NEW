using System;

namespace MitternachtWeb.Areas.Guild.Models {
	public class Mute {
		public ulong     UserId       { get; set; }
		public bool      Muted        { get; set; }
		public DateTime? MutedSince   { get; set; }
		public DateTime? UnmuteAt     { get; set; }
		public string    Username     { get; set; }
		public string    AvatarUrl    { get; set; }

		public string MuteDuration => MutedSince.HasValue && UnmuteAt.HasValue ? $"{UnmuteAt.Value - MutedSince.Value:d'd'h'h'm'min'}" : "-";
	}
}
