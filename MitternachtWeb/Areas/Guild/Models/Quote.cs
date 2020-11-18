using System;

namespace MitternachtWeb.Areas.Guild.Models {
	public class Quote {
		public int      Id         { get; set; }
		public ulong    AuthorId   { get; set; }
		public string   Authorname { get; set; }
		public string   AvatarUrl  { get; set; }
		public string   Keyword    { get; set; }
		public string   Text       { get; set; }
		public DateTime AddedAt    { get; set; }
	}
}
