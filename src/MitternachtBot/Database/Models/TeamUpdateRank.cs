namespace Mitternacht.Database.Models {
	public class TeamUpdateRank : DbEntity {
		public ulong  GuildId       { get; set; }
		public string Rankname      { get; set; }
		public string MessagePrefix { get; set; }
	}
}
