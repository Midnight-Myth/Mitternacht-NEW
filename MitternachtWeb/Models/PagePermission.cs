using Discord;
using System;

namespace MitternachtWeb.Models {
	[Flags]
	public enum BotPagePermission {
		None                 = 0b_0000,
		ReadBotConfig        = 0b_0001,
		WriteBotConfig       = 0b_0011,
		ReadAllGuildConfigs  = 0b_0100,
		WriteAllGuildConfigs = 0b_1100,
	}

	[Flags]
	public enum GuildPagePermission {
		None             = 0b_0000_0000,
		ReadGuildConfig  = 0b_0000_0001,
		WriteGuildConfig = 0b_0000_0011,
	}

	public static class PagePermissionExtensions {
		public static GuildPagePermission GetGuildPagePermissions(this GuildPermissions guildPerms) {
			var perms = GuildPagePermission.None;

			if(guildPerms.KickMembers) {
				perms |= GuildPagePermission.ReadGuildConfig;
			}
			
			if(guildPerms.Administrator) {
				perms |= GuildPagePermission.WriteGuildConfig;
			}

			return perms;
		}
	}
}
