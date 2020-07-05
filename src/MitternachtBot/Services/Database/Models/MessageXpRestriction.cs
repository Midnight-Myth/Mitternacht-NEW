namespace Mitternacht.Services.Database.Models {
	public class MessageXpRestriction : DbEntity {
		public ulong GuildId   { get; set; }
		public ulong ChannelId { get; set; }
	}
}