using System.Collections.Generic;

namespace Mitternacht.Services.Database.Models {
	public class StreamRoleSettings : DbEntity {
		public int         GuildConfigId { get; set; }
		public GuildConfig GuildConfig   { get; set; }
		public bool        Enabled       { get; set; }
		public ulong       AddRoleId     { get; set; }
		public ulong       FromRoleId    { get; set; }
		public string      Keyword       { get; set; }

		public HashSet<StreamRoleWhitelistedUser> Whitelist { get; set; } = new HashSet<StreamRoleWhitelistedUser>();
		public HashSet<StreamRoleBlacklistedUser> Blacklist { get; set; } = new HashSet<StreamRoleBlacklistedUser>();
	}

	public class StreamRoleBlacklistedUser : DbEntity {
		public ulong  UserId   { get; set; }
		public string Username { get; set; }

		public override bool Equals(object obj)
			=> obj is StreamRoleBlacklistedUser x && x.UserId == UserId;

		public override int GetHashCode()
			=> UserId.GetHashCode();
	}

	public class StreamRoleWhitelistedUser : DbEntity {
		public ulong  UserId   { get; set; }
		public string Username { get; set; }

		public override bool Equals(object obj)
			=> obj is StreamRoleWhitelistedUser x && x.UserId == UserId;

		public override int GetHashCode()
			=> UserId.GetHashCode();
	}
}
