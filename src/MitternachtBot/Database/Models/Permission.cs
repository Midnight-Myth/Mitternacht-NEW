using Mitternacht.Common;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace Mitternacht.Database.Models {
	[DebuggerDisplay("{PrimaryTarget}{SecondaryTarget} {SecondaryTargetName} {State} {PrimaryTargetId}")]
	public class Permission : DbEntity, IIndexed {
		public int? GuildConfigId { get; set; }
		public int Index { get; set; }

		public PrimaryPermissionType PrimaryTarget { get; set; }
		public ulong PrimaryTargetId { get; set; }

		public SecondaryPermissionType SecondaryTarget { get; set; }
		public string SecondaryTargetName { get; set; }

		public bool State { get; set; }

		[NotMapped]
		public static Permission AllowAllPerm => new Permission() {
			PrimaryTarget = PrimaryPermissionType.Server,
			PrimaryTargetId = 0,
			SecondaryTarget = SecondaryPermissionType.AllModules,
			SecondaryTargetName = "*",
			State = true,
			Index = 0,
		};

		[NotMapped]
		private static Permission BlockNsfwPerm => new Permission() {
			PrimaryTarget = PrimaryPermissionType.Server,
			PrimaryTargetId = 0,
			SecondaryTarget = SecondaryPermissionType.Module,
			SecondaryTargetName = "nsfw",
			State = false,
			Index = 1
		};

		public static List<Permission> GetDefaultPermlist => new List<Permission> {
			BlockNsfwPerm,
			AllowAllPerm
		};
	}

	public enum PrimaryPermissionType {
		User,
		Channel,
		Role,
		Server
	}

	public enum SecondaryPermissionType {
		Module,
		Command,
		AllModules
	}
}
