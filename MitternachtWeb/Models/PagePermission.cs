using Discord;
using System;

namespace MitternachtWeb.Models {
	[Flags]
	public enum BotLevelPermission {
		None                 = 0b_0000_0000,
		ReadBotConfig        = 0b_0000_0001,
		WriteBotConfig       = 0b_0000_0011,
		ReadAllGuildConfigs  = 0b_0000_0100,
		WriteAllGuildConfigs = 0b_0000_1100,
		ReadAllModerations   = 0b_0001_0000,
		WriteAllMutes        = 0b_0011_0000,
		All                  = 0b_1111_1111,
	}

	[Flags]
	public enum GuildLevelPermission {
		None             = 0b_0000_0000,
		ReadGuildConfig  = 0b_0000_0001,
		WriteGuildConfig = 0b_0000_0011,
		ReadModeration   = 0b_0000_0100,
		WriteMutes       = 0b_0000_1100,
		ReadAll          = ReadGuildConfig | ReadModeration,
		All              = 0b_1111_1111,
	}

	public static class PagePermissionExtensions {
		public static GuildLevelPermission GetGuildLevelPermissions(this GuildPermissions guildPerms) {
			var perms = GuildLevelPermission.None;

			if(guildPerms.ViewAuditLog) {
				perms |= GuildLevelPermission.ReadAll;
			}

			if(guildPerms.ManageMessages) {
				perms |= GuildLevelPermission.WriteMutes;
			}
			
			if(guildPerms.Administrator) {
				perms |= GuildLevelPermission.All;
			}

			return perms;
		}
	}
}
