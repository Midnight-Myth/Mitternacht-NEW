using System;

namespace MitternachtWeb.Areas.Guild.Models {
	public class Mute {
		public ulong     UserId    { get; set; }
		public bool      Muted     { get; set; }
		public DateTime? UnmuteAt  { get; set; }
		public string    Username  { get; set; }
		public string    AvatarUrl { get; set; }

		public string MutedUntil => UnmuteAt.HasValue ? UnmuteAt.Value.ToString("yyyy-MM-ddTHH:mm:ssZ") : "-";
	}
}
