namespace Mitternacht.Database.Models {
	public class RoleMoney : DbEntity {
		public ulong GuildId  { get; set; }
		public ulong RoleId   { get; set; }
		public long  Money    { get; set; }
		public int   Priority { get; set; }
	}
}
