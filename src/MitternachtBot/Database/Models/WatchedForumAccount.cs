namespace Mitternacht.Database.Models {
	public class WatchedForumAccount : DbEntity {
		public ulong       GuildId     { get; set; }
		public long        ForumUserId { get; set; }
		public WatchAction WatchAction { get; set; }
	}

	public enum WatchAction {
		NONE   = 0,
		NOTIFY = 1,
		BAN    = 2,
	}
}
