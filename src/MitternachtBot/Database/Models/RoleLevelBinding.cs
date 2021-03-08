namespace Mitternacht.Database.Models {
	public class RoleLevelBinding : DbEntity {
		public ulong GuildId      { get; set; }
		public ulong RoleId       { get; set; }
		public int   MinimumLevel { get; set; }
	}
}
