using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Mitternacht.Database.Models;

namespace Mitternacht.Modules.Permissions.Common {
	public static class PermissionExtensions {
		public static bool CheckPermissions(this IEnumerable<Permission> permsEnumerable, IUserMessage message, string commandName, string moduleName, out int permIndex) {
			var perms = permsEnumerable as List<Permission> ?? permsEnumerable.ToList();

			for(int i = perms.Count - 1; i >= 0; i--) {
				var perm = perms[i];

				var result = perm.CheckPermission(message, commandName, moduleName);

				if(result == null) {
					continue;
				} else {
					permIndex = i;
					return result.Value;
				}
			}
			permIndex = -1;
			return true;
		}

		/// <returns>null if not applicable, true if allowed, false if not allowed.</returns>
		public static bool? CheckPermission(this Permission perm, IUserMessage message, string commandName, string moduleName) {
			if(!((perm.SecondaryTarget == SecondaryPermissionType.Command &&
					perm.SecondaryTargetName.ToLowerInvariant() == commandName.ToLowerInvariant()) ||
				(perm.SecondaryTarget == SecondaryPermissionType.Module &&
					perm.SecondaryTargetName.ToLowerInvariant() == moduleName.ToLowerInvariant()) ||
					perm.SecondaryTarget == SecondaryPermissionType.AllModules))
				return null;

			switch(perm.PrimaryTarget) {
				case PrimaryPermissionType.User:
					if(perm.PrimaryTargetId == message.Author.Id)
						return perm.State;
					break;
				case PrimaryPermissionType.Channel:
					if(perm.PrimaryTargetId == message.Channel.Id)
						return perm.State;
					break;
				case PrimaryPermissionType.Role:
					if(!(message.Author is IGuildUser guildUser))
						break;
					if(guildUser.RoleIds.Contains(perm.PrimaryTargetId))
						return perm.State;
					break;
				case PrimaryPermissionType.Server:
					if(!(message.Author is IGuildUser))
						break;
					return perm.State;
			}
			return null;
		}

		public static string GetCommand(this Permission perm, string prefix, SocketGuild guild = null) {
			var com = "";
			switch(perm.PrimaryTarget) {
				case PrimaryPermissionType.User:
					com += "u";
					break;
				case PrimaryPermissionType.Channel:
					com += "c";
					break;
				case PrimaryPermissionType.Role:
					com += "r";
					break;
				case PrimaryPermissionType.Server:
					com += "s";
					break;
			}

			switch(perm.SecondaryTarget) {
				case SecondaryPermissionType.Module:
					com += "m";
					break;
				case SecondaryPermissionType.Command:
					com += "c";
					break;
				case SecondaryPermissionType.AllModules:
					com = "a" + com + "m";
					break;
			}

			var secName = perm.SecondaryTarget == SecondaryPermissionType.Command ? prefix + perm.SecondaryTargetName : perm.SecondaryTargetName;
			com += $" {(perm.SecondaryTargetName != "*" ? $"{secName} " : "")}{(perm.State ? "enable" : "disable")} ";

			switch(perm.PrimaryTarget) {
				case PrimaryPermissionType.User:
					com += guild?.GetUser(perm.PrimaryTargetId)?.ToString() ?? $"<@{perm.PrimaryTargetId}>";
					break;
				case PrimaryPermissionType.Channel:
					com += $"<#{perm.PrimaryTargetId}>";
					break;
				case PrimaryPermissionType.Role:
					com += guild?.GetRole(perm.PrimaryTargetId)?.ToString() ?? $"<@&{perm.PrimaryTargetId}>";
					break;
				case PrimaryPermissionType.Server:
					break;
			}

			return prefix + com;
		}
	}
}
