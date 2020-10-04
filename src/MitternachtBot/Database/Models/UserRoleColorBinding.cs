namespace Mitternacht.Services.Database.Models {
	public class UserRoleColorBinding : DbEntity {
		public ulong UserId  { get; set; }
		public ulong GuildId { get; set; }
		public ulong RoleId  { get; set; }
	}
}
