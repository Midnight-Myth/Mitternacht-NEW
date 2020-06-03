using System;

namespace MitternachtWeb.Areas.Moderation.Models {
	public class Mute {
		public ulong     UserId   { get; set; }
		public bool      Muted    { get; set; }
		public DateTime? UnmuteAt { get; set; }

		public string MutedUntil => UnmuteAt.HasValue ? UnmuteAt.Value.ToString("yyyy-MM-ddTHH:mm:ssZ") : "-";
	}
}
