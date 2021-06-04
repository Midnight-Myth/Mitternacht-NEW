using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Database;
using Mitternacht.Services;
using Mitternacht.Database.Models;
using Mitternacht.Services.Impl;
using NLog;

namespace Mitternacht.Modules.Administration.Services {
	public class MuteService : IMService {
		public ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, Timer>> UnmuteTimers { get; } = new ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, Timer>>();

		public event Action<IGuildUser> UserMuted   = delegate { };
		public event Action<IGuildUser> UserUnmuted = delegate { };

		private readonly Logger _log = LogManager.GetCurrentClassLogger();
		private readonly DiscordSocketClient _client;
		private readonly DbService _db;

		public MuteService(DiscordSocketClient client, DbService db) {
			_client = client;
			_db = db;

			using var uow = _db.UnitOfWork;

			foreach(var gc in uow.GuildConfigs.GetAllGuildConfigs(client.Guilds.Select(g => g.Id).ToList(), set => set.Include(x => x.UnmuteTimers))) {
				foreach(var ut in gc.UnmuteTimers) {
					var after = ut.UnmuteAt - TimeSpan.FromMinutes(2) <= DateTime.UtcNow ? TimeSpan.FromMinutes(2) : ut.UnmuteAt - DateTime.UtcNow;
					StartUnmuteTimer(gc.GuildId, ut.UserId, after);
				}
			}

			_client.UserJoined         += Client_UserJoined;
			_client.GuildMemberUpdated += Client_GuildMemberUpdated;
		}

		private Task Client_UserJoined(IGuildUser guildUser) {
			try {
				using var uow = _db.UnitOfWork;

				//Mute user if a mute is saved in the database.
				if(uow.GuildConfigs.For(guildUser.GuildId, set => set.Include(gc => gc.MutedUsers)).MutedUsers.Any(mu => mu.UserId == guildUser.Id)) {
					var _ = Task.Run(() => MuteUser(guildUser).ConfigureAwait(false));
				}
			} catch(Exception ex) {
				_log.Warn(ex);
			}
			return Task.CompletedTask;
		}

		private async Task Client_GuildMemberUpdated(Cacheable<SocketGuildUser, ulong> oldUser, SocketGuildUser updatedUser) {
			var       oldRoles     = (await oldUser.GetOrDownloadAsync()).Roles;
			var       changedRoles = updatedUser.Roles.Where(r => !oldRoles.Contains(r)).ToArray();
			using var uow          = _db.UnitOfWork;
			var       mutedRole    = await GetMuteRole(updatedUser.Guild, uow).ConfigureAwait(false);

			if(changedRoles.Contains(mutedRole)) {
				var       gc        = uow.GuildConfigs.For(updatedUser.Guild.Id, set => set.Include(g => g.MutedUsers));
				var       mutedUser = gc.MutedUsers.FirstOrDefault(mu => mu.UserId == updatedUser.Id);

				if(mutedUser is null) {
					_ = Task.Run(async () => await MuteUser(updatedUser).ConfigureAwait(false));
				}
			}
		}

		public async Task MuteUser(IGuildUser guildUser) {
			StopUnmuteTimer(guildUser.GuildId, guildUser.Id);
			RemoveUnmuteTimerFromDb(guildUser.GuildId, guildUser.Id);
			
			//Save mute to database.
			using var uow       = _db.UnitOfWork;
			var       gc        = uow.GuildConfigs.For(guildUser.Guild.Id, set => set.Include(g => g.MutedUsers));
			var       mutedUser = gc.MutedUsers.FirstOrDefault(mu => mu.UserId == guildUser.Id);
			if(mutedUser == null) {
				gc.MutedUsers.Add(new MutedUserId { UserId = guildUser.Id });
				await uow.SaveChangesAsync().ConfigureAwait(false);
			}

			//Add muted role to user.
			var muteRole     = await GetMuteRole(guildUser.Guild, uow).ConfigureAwait(false);
			var alreadyMuted = guildUser.RoleIds.Contains(muteRole.Id);
			if(!alreadyMuted)
				await guildUser.AddRoleAsync(muteRole).ConfigureAwait(false);

			if(!alreadyMuted || mutedUser == null)
				UserMuted(guildUser);
		}

		public async Task UnmuteUser(IGuildUser guildUser) {
			StopUnmuteTimer(guildUser.GuildId, guildUser.Id);
			RemoveUnmuteTimerFromDb(guildUser.GuildId, guildUser.Id);
			
			using var uow = _db.UnitOfWork;

			//Remove muted role from user.
			var muteRole     = await GetMuteRole(guildUser.Guild, uow).ConfigureAwait(false);
			var silencedRole = GetSilencedRole(guildUser.Guild, uow);
			if(guildUser.RoleIds.Contains(muteRole.Id))
				await guildUser.RemoveRoleAsync(muteRole).ConfigureAwait(false);
			if(silencedRole is not null && guildUser.RoleIds.Contains(silencedRole.Id)) {
				await guildUser.RemoveRoleAsync(silencedRole).ConfigureAwait(false);
			}

			//Remove mute from database.
			var gc = uow.GuildConfigs.For(guildUser.Guild.Id, set => set.Include(g => g.MutedUsers));
			gc.MutedUsers.RemoveWhere(mu => mu.UserId == guildUser.Id);
			await uow.SaveChangesAsync().ConfigureAwait(false);

			UserUnmuted(guildUser);
		}

		public async Task<IRole> GetMuteRole(IGuild guild) {
			using var uow = _db.UnitOfWork;
			return await GetMuteRole(guild, uow).ConfigureAwait(false);
		}

		public async Task<IRole> GetMuteRole(IGuild guild, IUnitOfWork uow) {
			const string defaultMuteRoleName = "Muted";
			
			var       gc       = uow.GuildConfigs.For(guild.Id);
			var       muteRole = gc.MutedRoleId.HasValue ? guild.Roles.FirstOrDefault(r => r.Id == gc.MutedRoleId) : null;

			if(muteRole == null) {
				var muteRoleName = string.IsNullOrWhiteSpace(gc.MuteRoleName) ? defaultMuteRoleName : gc.MuteRoleName;

				muteRole = guild.Roles.FirstOrDefault(r => r.Name == muteRoleName);

				if(muteRole == null) {
					//TODO: Silently creating the role is not a good design.
					muteRole = await guild.CreateRoleAsync(muteRoleName, GuildPermissions.None, isMentionable: false).ConfigureAwait(false);
				} else {
					gc.MutedRoleId = muteRole.Id;
					await uow.SaveChangesAsync();
				}
			}

			return muteRole;
		}

		public IRole GetSilencedRole(IGuild guild, IUnitOfWork uow) {
			var gc             = uow.GuildConfigs.For(guild.Id);
			var silencedRoleId = gc.SilencedRoleId;

			return silencedRoleId.HasValue ? guild.GetRole(silencedRoleId.Value) : null;
		}

		public async Task TimedMute(IGuildUser guildUser, TimeSpan after) {
			await MuteUser(guildUser).ConfigureAwait(false);

			using var uow = _db.UnitOfWork;
			var gc = uow.GuildConfigs.For(guildUser.GuildId, set => set.Include(x => x.UnmuteTimers));
			gc.UnmuteTimers.Add(new UnmuteTimer {
				UserId = guildUser.Id,
				UnmuteAt = DateTime.UtcNow + after,
			});
			await uow.SaveChangesAsync().ConfigureAwait(false);

			StartUnmuteTimer(guildUser.GuildId, guildUser.Id, after);
		}

		public void StartUnmuteTimer(ulong guildId, ulong userId, TimeSpan after) {
			var guildUpdateTimers = UnmuteTimers.GetOrAdd(guildId, new ConcurrentDictionary<ulong, Timer>());

			var unmuteTimer = new Timer(async _ => {
				try {
					var guild     = _client.GetGuild(guildId);
					var guildUser = guild?.GetUser(userId);
					
					RemoveUnmuteTimerFromDb(guildId, userId);

					if(guild != null) {
						await UnmuteUser(guildUser).ConfigureAwait(false);
					}
				} catch(Exception ex) {
					RemoveUnmuteTimerFromDb(guildId, userId);
					_log.Warn("Couldn't unmute user {0} in guild {1}", userId, guildId);
					_log.Warn(ex);
				}
			}, null, after, Timeout.InfiniteTimeSpan);

			guildUpdateTimers.AddOrUpdate(userId, key => unmuteTimer, (key, old) => {
				old.Dispose();
				return unmuteTimer;
			});
		}

		public void StopUnmuteTimer(ulong guildId, ulong userId) {
			if(UnmuteTimers.TryGetValue(guildId, out var userUnmuteTimers) && userUnmuteTimers.TryRemove(userId, out var removed)) {
				removed.Dispose();
			}
		}

		private void RemoveUnmuteTimerFromDb(ulong guildId, ulong userId) {
			using var uow = _db.UnitOfWork;
			var gc = uow.GuildConfigs.For(guildId, set => set.Include(x => x.UnmuteTimers));
			gc.UnmuteTimers.RemoveWhere(x => x.UserId == userId);
			uow.SaveChanges();
		}
	}
}
