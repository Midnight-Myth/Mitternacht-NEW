using GommeHDnetForumAPI;
using Mitternacht.Database.Models;
using MitternachtWeb.Models;

namespace MitternachtWeb.Areas.Guild.Models {
	public class WatchedForumAccount {
		public ModeledDiscordUser DiscordUser { get; set; }
		public long               ForumUserId { get; set; }
		public WatchAction        WatchAction { get; set; }
		public string             ForumProfileUrl => $"{ForumPaths.MembersUrl}{ForumUserId}";
	}
}
