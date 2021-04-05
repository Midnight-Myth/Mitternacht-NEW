using MitternachtWeb.Models;
using System;

namespace MitternachtWeb.Areas.Guild.Models {
	public class Quote {
		public ModeledDiscordUser Author  { get; set; }
		public int                Id      { get; set; }
		public string             Keyword { get; set; }
		public string             Text    { get; set; }
		public DateTime           AddedAt { get; set; }
	}
}
