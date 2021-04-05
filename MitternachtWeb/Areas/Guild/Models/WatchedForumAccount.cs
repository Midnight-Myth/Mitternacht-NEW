using GommeHDnetForumAPI;
using Mitternacht.Database.Models;

namespace MitternachtWeb.Areas.Guild.Models {
	public class WatchedForumAccount {
		public ulong? UserId { get; set; }
		public string Username { get; set; }
		public string AvatarUrl { get; set; }
		public long ForumUserId { get; set; }
		public WatchAction WatchAction { get; set; }
		public string ForumProfileUrl => $"{ForumPaths.MembersUrl}{ForumUserId}";
	}
}
