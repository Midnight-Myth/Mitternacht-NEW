using Discord;

namespace MitternachtWeb.Areas.Guild.Models {
	public class Verification {
		public ulong   UserId          { get; set; }
		public string  Username        { get; set; }
		public string  AvatarUrl       { get; set; }
		public string  ForumProfileUrl { get; set; }
		public IRole[] Roles           { get; set; }
	}
}
