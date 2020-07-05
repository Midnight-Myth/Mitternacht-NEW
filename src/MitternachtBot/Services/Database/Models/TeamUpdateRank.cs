namespace Mitternacht.Services.Database.Models {
	public class TeamUpdateRank : DbEntity {
		public ulong  GuildId  { get; set; }
		public string Rankname { get; set; }
	}
}
