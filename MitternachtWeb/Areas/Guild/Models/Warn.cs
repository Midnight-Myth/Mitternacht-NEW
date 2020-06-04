using System;

namespace MitternachtWeb.Areas.Guild.Models {
	public class Warn {
		public ulong     UserId     { get; set; }
		public string    Username   { get; set; }
		public DateTime? WarnedAt   { get; set; }
		public string    AvatarUrl  { get; set; }
		public string    Reason     { get; set; }
		public bool      Forgiven   { get; set; }
		public string    ForgivenBy { get; set; }
		public string    WarnedBy   { get; set; }

		public string WarnedAtString => WarnedAt.HasValue ? WarnedAt.Value.ToString("yyyy-MM-ddTHH:mm:ssZ") : "-";
	}
}
