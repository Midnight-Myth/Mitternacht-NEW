using Discord;
using System;

namespace MitternachtWeb.Models {
	[Flags]
	public enum BotLevelPermission {
		None                         = 0b_0000_0000_0000_0000,
		ReadBotConfig                = 0b_0000_0000_0000_0001,
		WriteBotConfig               = 0b_0000_0000_0000_0011,
		ReadAllGuildConfigs          = 0b_0000_0000_0000_0100,
		WriteAllGuildConfigs         = 0b_0000_0000_0000_1100,
		ReadAllMutes                 = 0b_0000_0000_0001_0000,
		WriteAllMutes                = 0b_0000_0000_0011_0000,
		ReadAllWarns                 = 0b_0000_0000_0100_0000,
		ForgiveAllWarns              = 0b_0000_0000_1100_0000,
		WriteAllWarns                = 0b_0000_0001_1100_0000,
		ReadAllQuotes                = 0b_0000_0010_0000_0000,
		WriteAllQuotes               = 0b_0000_0110_0001_0000,
		ReadAllVerifications         = 0b_0000_1000_0000_0000,
		WriteAllVerifications        = 0b_0001_1000_0000_0000,
		ReadAllWatchedForumAccounts  = 0b_0010_0000_0000_0000,
		WriteAllWatchedForumAccounts = 0b_0110_0000_0000_0000,
		ReadAllGuilds                = ReadAllGuildConfigs | ReadAllMutes | ReadAllWarns | ReadAllQuotes | ReadAllVerifications | ReadAllWatchedForumAccounts,
		ReadAll                      = ReadBotConfig | ReadAllGuilds,
		All                          = 0b_0111_1111_1111_1111,
	}

	[Flags]
	public enum GuildLevelPermission {
		None                      = 0b_0000_0000_0000_0000,
		ReadGuildConfig           = 0b_0000_0000_0000_0001,
		WriteGuildConfig          = 0b_0000_0000_0000_0011,
		ReadMutes                 = 0b_0000_0000_0000_0100,
		WriteMutes                = 0b_0000_0000_0000_1100,
		ReadWarns                 = 0b_0000_0000_0001_0000,
		ForgiveWarns              = 0b_0000_0000_0011_0000,
		WriteWarns                = 0b_0000_0000_0111_0000,
		ReadQuotes                = 0b_0000_0000_1000_0000,
		WriteQuotes               = 0b_0000_0001_1000_0100,
		ReadVerifications         = 0b_0000_0010_0000_0000,
		WriteVerifications        = 0b_0000_0110_0000_0000,
		ReadWatchedForumAccounts  = 0b_0000_1000_0000_0000,
		WriteWatchedForumAccounts = 0b_0001_1000_0000_0000,
		ReadAll                   = ReadGuildConfig | ReadMutes | ReadWarns | ReadQuotes | ReadVerifications | ReadWatchedForumAccounts,
		All                       = 0b_0001_1111_1111_1111,
	}

	public static class PagePermissionExtensions {
		public static GuildLevelPermission GetGuildLevelPermissions(this GuildPermissions guildPerms) {
			var perms = GuildLevelPermission.None;

			if(guildPerms.ViewAuditLog) {
				perms |= GuildLevelPermission.ReadAll;
			}

			if(guildPerms.ManageMessages) {
				perms |= GuildLevelPermission.WriteMutes | GuildLevelPermission.ForgiveWarns | GuildLevelPermission.WriteQuotes | GuildLevelPermission.ReadWatchedForumAccounts;
			}

			if(guildPerms.BanMembers) {
				perms |= GuildLevelPermission.WriteWarns | GuildLevelPermission.WriteVerifications | GuildLevelPermission.WriteWatchedForumAccounts;
			}
			
			if(guildPerms.Administrator) {
				perms |= GuildLevelPermission.All;
			}

			return perms;
		}
	}
}
