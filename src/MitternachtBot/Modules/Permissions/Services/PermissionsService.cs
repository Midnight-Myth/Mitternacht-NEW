using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Common.ModuleBehaviors;
using Mitternacht.Extensions;
using Mitternacht.Modules.Permissions.Common;
using Mitternacht.Services;
using Mitternacht.Database.Models;
using Mitternacht.Services.Impl;

namespace Mitternacht.Modules.Permissions.Services {
	public class PermissionService : ILateBlocker, IMService {
		private readonly DbService _db;
		private readonly CommandHandler _cmd;
		private readonly StringService _strings;

		public ConcurrentDictionary<ulong, PermissionCache> Cache { get; } = new ConcurrentDictionary<ulong, PermissionCache>();

		public PermissionService(DiscordSocketClient client, DbService db, CommandHandler cmd, StringService strings) {
			_db = db;
			_cmd = cmd;
			_strings = strings;

			using var uow = _db.UnitOfWork;
			foreach(var x in uow.GuildConfigs.Permissionsv2ForAll(client.Guilds.ToArray().Select(x => x.Id).ToList())) {
				Cache.TryAdd(x.GuildId, new PermissionCache {
					Verbose = x.VerbosePermissions,
					PermRole = x.PermissionRole,
					Permissions = new PermissionsCollection(x.Permissions)
				});
			}
		}

		public PermissionCache GetCache(ulong guildId) {
			if(Cache.TryGetValue(guildId, out var pc))
				return pc;
			using(var uow = _db.UnitOfWork) {
				var config = uow.GuildConfigs.For(guildId,
					set => set.Include(x => x.Permissions));
				UpdateCache(config);
			}
			Cache.TryGetValue(guildId, out pc);
			if(pc == null)
				throw new Exception("Cache is null.");
			return pc;
		}

		public async Task AddPermissions(ulong guildId, params Permission[] perms) {
			using(var uow = _db.UnitOfWork) {
				var config = uow.GuildConfigs.GcWithPermissionsv2For(guildId);
				//var orderedPerms = new PermissionsCollection<Permissionv2>(config.Permissions);
				var max = config.Permissions.Max(x => x.Index); //have to set its index to be the highest
				foreach(var perm in perms) {
					perm.Index = ++max;
					config.Permissions.Add(perm);
				}
				await uow.SaveChangesAsync().ConfigureAwait(false);
				UpdateCache(config);
			}
		}

		public void UpdateCache(GuildConfig config) {
			Cache.AddOrUpdate(config.GuildId, new PermissionCache {
				Permissions = new PermissionsCollection(config.Permissions),
				PermRole = config.PermissionRole,
				Verbose = config.VerbosePermissions
			}, (id, old) => {
				old.Permissions = new PermissionsCollection(config.Permissions);
				old.PermRole = config.PermissionRole;
				old.Verbose = config.VerbosePermissions;
				return old;
			});
		}

		public async Task<bool> TryBlockLate(DiscordSocketClient client, IUserMessage msg, IGuild guild, IMessageChannel channel, IUser user, string moduleName, string commandName) {
			await Task.Yield();
			if(!(guild is SocketGuild socketGuild))
				return false;
			var resetCommand = commandName == "resetperms";

			var pc = GetCache(guild.Id);
			if(!resetCommand && !pc.Permissions.CheckPermissions(msg, commandName, moduleName, out var index)) {
				if(!pc.Verbose)
					return true;
				try { await channel.SendErrorAsync(_strings.GetText("permissions", "trigger", guild.Id, index + 1, pc.Permissions[index].GetCommand(_cmd.GetPrefix(guild), socketGuild))).ConfigureAwait(false); } catch { }
				return true;
			}


			if(moduleName != "Permissions")
				return false;
			var roles = (user as SocketGuildUser)?.Roles ?? ((IGuildUser)user).RoleIds.Select(guild.GetRole).Where(x => x != null);
			if(roles.Any(r => string.Equals(r.Name.Trim(), pc.PermRole.Trim(), StringComparison.InvariantCultureIgnoreCase)) || user.Id == ((IGuildUser)user).Guild.OwnerId)
				return false;
			var returnMsg = $"You need the **{pc.PermRole}** role in order to use permission commands.";
			if(!pc.Verbose)
				return true;
			try { await channel.SendErrorAsync(returnMsg).ConfigureAwait(false); } catch { }
			return true;
		}
	}
}