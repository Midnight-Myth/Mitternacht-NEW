using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Mitternacht.Common.Collections;
using Mitternacht.Common.ModuleBehaviors;
using Mitternacht.Services;

namespace Mitternacht.Modules.Permissions.Services {
	public class GlobalPermissionService : ILateBlocker, IMService {
		public readonly ConcurrentHashSet<string> BlockedModules;
		public readonly ConcurrentHashSet<string> BlockedCommands;

		public GlobalPermissionService(IBotConfigProvider bc) {
			BlockedModules = new ConcurrentHashSet<string>(bc.BotConfig.BlockedModules.Select(x => x.Name));
			BlockedCommands = new ConcurrentHashSet<string>(bc.BotConfig.BlockedCommands.Select(x => x.Name));
		}

		public Task<bool> TryBlockLate(DiscordSocketClient client, IUserMessage msg, IGuild guild, IMessageChannel channel, IUser user, string moduleName, string commandName)
			=> Task.FromResult(!commandName.Equals("resetglobalperms", StringComparison.OrdinalIgnoreCase) && (BlockedCommands.Contains(commandName, StringComparer.OrdinalIgnoreCase) || BlockedModules.Contains(moduleName, StringComparer.OrdinalIgnoreCase)));
	}
}
