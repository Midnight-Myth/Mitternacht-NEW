using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace Mitternacht.Database.Models {
	public interface IIndexed {
		int Index { get; set; }
	}

	[DebuggerDisplay("{PrimaryTarget}{SecondaryTarget} {SecondaryTargetName} {State} {PrimaryTargetId}")]
	public class Permissionv2 : DbEntity, IIndexed {
		public int? GuildConfigId { get; set; }
		public int Index { get; set; }

		public PrimaryPermissionType PrimaryTarget { get; set; }
		public ulong PrimaryTargetId { get; set; }

		public SecondaryPermissionType SecondaryTarget { get; set; }
		public string SecondaryTargetName { get; set; }

		public bool State { get; set; }

		[NotMapped]
		public static Permissionv2 AllowAllPerm => new Permissionv2() {
			PrimaryTarget = PrimaryPermissionType.Server,
			PrimaryTargetId = 0,
			SecondaryTarget = SecondaryPermissionType.AllModules,
			SecondaryTargetName = "*",
			State = true,
			Index = 0,
		};

		[NotMapped]
		private static Permissionv2 BlockNsfwPerm => new Permissionv2() {
			PrimaryTarget = PrimaryPermissionType.Server,
			PrimaryTargetId = 0,
			SecondaryTarget = SecondaryPermissionType.Module,
			SecondaryTargetName = "nsfw",
			State = false,
			Index = 1
		};

		public static List<Permissionv2> GetDefaultPermlist => new List<Permissionv2> {
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
