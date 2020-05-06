using Discord;
using System;

namespace MitternachtWeb.Models {
	[Flags]
	public enum BotLevelPermission {
		None                 = 0b_0000,
		ReadBotConfig        = 0b_0001,
		WriteBotConfig       = 0b_0011,
		ReadAllGuildConfigs  = 0b_0100,
		WriteAllGuildConfigs = 0b_1100,
		All                  = 0b_1111,
	}

	[Flags]
	public enum GuildLevelPermission {
		None             = 0b_0000_0000,
		ReadGuildConfig  = 0b_0000_0001,
		WriteGuildConfig = 0b_0000_0011,
	}

	public static class PagePermissionExtensions {
		public static GuildLevelPermission GetGuildPagePermissions(this GuildPermissions guildPerms) {
			var perms = GuildLevelPermission.None;

			if(guildPerms.KickMembers) {
				perms |= GuildLevelPermission.ReadGuildConfig;
			}
			
			if(guildPerms.Administrator) {
				perms |= GuildLevelPermission.WriteGuildConfig;
			}

			return perms;
		}
	}
}
