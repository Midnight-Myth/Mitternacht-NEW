namespace Mitternacht.Database.Models {
	public class Currency : DbEntity {
		public ulong GuildId { get; set; }
		public ulong UserId { get; set; }
		public long  Amount { get; set; }
	}
}
