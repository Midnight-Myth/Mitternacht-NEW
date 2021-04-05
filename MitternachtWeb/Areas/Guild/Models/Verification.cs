using Discord;
using MitternachtWeb.Models;

namespace MitternachtWeb.Areas.Guild.Models {
	public class Verification {
		public ModeledDiscordUser DiscordUser     { get; set; }
		public string             ForumProfileUrl { get; set; }
		public IRole[]            Roles           { get; set; }
	}
}
