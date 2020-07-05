namespace Mitternacht.Services.Database.Models {
	public class VerifiedUser : DbEntity {
		public ulong GuildId     { get; set; }
		public ulong UserId      { get; set; }
		public long  ForumUserId { get; set; }
	}
}