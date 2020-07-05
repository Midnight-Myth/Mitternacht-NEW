using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;
using NLog;

namespace Mitternacht.Modules.Administration.Services {
	public class MuteService : IMService {
		public ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, Timer>> UnmuteTimers { get; } = new ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, Timer>>();

		public event Action<IGuildUser> UserMuted   = delegate { };
		public event Action<IGuildUser> UserUnmuted = delegate { };

		private readonly Logger _log = LogManager.GetCurrentClassLogger();
		private readonly DiscordSocketClient _client;
		private readonly DbService _db;

		public MuteService(DiscordSocketClient client, IEnumerable<GuildConfig> gcs, DbService db) {
			_client = client;
			_db = db;

			foreach(var gc in gcs) {
				foreach(var ut in gc.UnmuteTimers) {
					var after = ut.UnmuteAt - TimeSpan.FromMinutes(2) <= DateTime.UtcNow ? TimeSpan.FromMinutes(2) : ut.UnmuteAt - DateTime.UtcNow;
					StartUnmuteTimer(gc.GuildId, ut.UserId, after);
				}
			}

			_client.UserJoined += Client_UserJoined;
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

		public async Task MuteUser(IGuildUser guildUser) {
			StopUnmuteTimer(guildUser.GuildId, guildUser.Id);
			RemoveUnmuteTimerFromDb(guildUser.GuildId, guildUser.Id);

			//Add muted role to user.
			var muteRole = await GetMuteRole(guildUser.Guild).ConfigureAwait(false);
			var alreadyMuted = guildUser.RoleIds.Contains(muteRole.Id);
			if(!alreadyMuted)
				await guildUser.AddRoleAsync(muteRole).ConfigureAwait(false);

			//Save mute to database.
			using var uow = _db.UnitOfWork;
			var gc = uow.GuildConfigs.For(guildUser.Guild.Id, set => set.Include(g => g.MutedUsers));
			var mutedUser = gc.MutedUsers.FirstOrDefault(mu => mu.UserId == guildUser.Id);
			if(mutedUser == null) {
				gc.MutedUsers.Add(new MutedUserId { UserId = guildUser.Id });
				await uow.SaveChangesAsync().ConfigureAwait(false);
			}

			if(!alreadyMuted || mutedUser == null)
				UserMuted(guildUser);
		}

		public async Task UnmuteUser(IGuildUser guildUser) {
			StopUnmuteTimer(guildUser.GuildId, guildUser.Id);
			RemoveUnmuteTimerFromDb(guildUser.GuildId, guildUser.Id);

			//Remove muted role from user.
			var muteRole = await GetMuteRole(guildUser.Guild).ConfigureAwait(false);
			if(guildUser.RoleIds.Contains(muteRole.Id))
				await guildUser.RemoveRoleAsync(muteRole).ConfigureAwait(false);

			//Remove mute from database.
			using var uow = _db.UnitOfWork;
			var gc = uow.GuildConfigs.For(guildUser.Guild.Id, set => set.Include(g => g.MutedUsers));
			gc.MutedUsers.RemoveWhere(mu => mu.UserId == guildUser.Id);
			await uow.SaveChangesAsync().ConfigureAwait(false);

			UserUnmuted(guildUser);
		}

		public async Task<IRole> GetMuteRole(IGuild guild) {
			const string defaultMuteRoleName = "Muted";

			using var uow = _db.UnitOfWork;
			var gc = uow.GuildConfigs.For(guild.Id);
			if(string.IsNullOrWhiteSpace(gc.MuteRoleName)) {
				gc.MuteRoleName = defaultMuteRoleName;
				await uow.SaveChangesAsync().ConfigureAwait(false);
			}

			var muteRole = guild.Roles.FirstOrDefault(r => r.Name == gc.MuteRoleName);
			if(muteRole == null) {
				//TODO: Silently creating the role is not a good design.
				try {
					muteRole = await guild.CreateRoleAsync(gc.MuteRoleName, GuildPermissions.None, isMentionable: false).ConfigureAwait(false);
				} catch {
					muteRole = guild.Roles.FirstOrDefault(r => r.Name == gc.MuteRoleName) ?? await guild.CreateRoleAsync(defaultMuteRoleName, GuildPermissions.None, isMentionable: false).ConfigureAwait(false);
				}
			}

			return muteRole;
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

		public DateTime? GetMuteTime(IGuildUser guildUser) {
			using var uow = _db.UnitOfWork;
			return uow.GuildConfigs.For(guildUser.GuildId, set => set.Include(x => x.UnmuteTimers)).UnmuteTimers.FirstOrDefault(ut => ut.UserId == guildUser.Id)?.UnmuteAt;
		}

		private void RemoveUnmuteTimerFromDb(ulong guildId, ulong userId) {
			using var uow = _db.UnitOfWork;
			var gc = uow.GuildConfigs.For(guildId, set => set.Include(x => x.UnmuteTimers));
			gc.UnmuteTimers.RemoveWhere(x => x.UserId == userId);
			uow.SaveChanges();
		}
	}
}
