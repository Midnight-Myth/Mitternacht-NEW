using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Common.Collections;
using Mitternacht.Common.ModuleBehaviors;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Permissions.Services {
	public class CommandCooldownService : ILateBlocker, IMService {
		private readonly DbService       _db;
		private readonly IBotCredentials _creds;

		public ConcurrentDictionary<ulong, ConcurrentHashSet<ActiveCooldown>> ActiveCooldowns { get; } = new ConcurrentDictionary<ulong, ConcurrentHashSet<ActiveCooldown>>();

		public CommandCooldownService(DbService db, IBotCredentials creds) {
			_db    = db;
			_creds = creds;
		}

		private CommandCooldown[] CommandCooldowns(ulong guildId) {
			using var uow = _db.UnitOfWork;
			return uow.GuildConfigs.For(guildId, set => set.Include(gc => gc.CommandCooldowns)).CommandCooldowns.ToArray();
		}

		public Task<bool> TryBlockLate(DiscordSocketClient client, IUserMessage message, IGuild guild, IMessageChannel channel, IUser user, string moduleName, string commandName) {
			if(guild == null || _creds.IsOwner(user))
				return Task.FromResult(false);

			var commandCooldowns = CommandCooldowns(guild.Id);
			var commandCooldown  = commandCooldowns.FirstOrDefault(cc => cc.CommandName.Equals(commandName, StringComparison.OrdinalIgnoreCase));
			
			if(commandCooldown != null) {
				var activeCooldownsInGuild = ActiveCooldowns.GetOrAdd(guild.Id, new ConcurrentHashSet<ActiveCooldown>());

				if(activeCooldownsInGuild.Any(ac => ac.UserId == user.Id && ac.Command.Equals(commandName, StringComparison.OrdinalIgnoreCase))) {
					return Task.FromResult(true);
				}

				activeCooldownsInGuild.Add(new ActiveCooldown() {
					UserId  = user.Id,
					Command = commandName,
				});
				var _ = Task.Run(async () => {
					try {
						await Task.Delay(commandCooldown.Seconds * 1000);
						activeCooldownsInGuild.RemoveWhere(ac => ac.UserId == user.Id && ac.Command.Equals(commandName, StringComparison.OrdinalIgnoreCase));
					} catch { }
				});
			}
			return Task.FromResult(false);
		}
	}

	public class ActiveCooldown {
		public string Command { get; set; }
		public ulong  UserId  { get; set; }
	}
}
