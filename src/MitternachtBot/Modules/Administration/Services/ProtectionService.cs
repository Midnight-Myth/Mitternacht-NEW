using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Extensions;
using Mitternacht.Modules.Administration.Common;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;
using Mitternacht.Services.Impl;
using NLog;

namespace Mitternacht.Modules.Administration.Services {
	public class ProtectionService : IMService {
		private readonly DbService _db;
		private readonly MuteService _mute;
		private readonly Logger _log;
		
		private readonly ConcurrentDictionary<ulong, AntiRaidStats> _antiRaidGuilds = new ConcurrentDictionary<ulong, AntiRaidStats>();
		private readonly ConcurrentDictionary<ulong, AntiSpamStats> _antiSpamGuilds = new ConcurrentDictionary<ulong, AntiSpamStats>();

		public event Func<PunishmentAction, ProtectionType, IGuildUser[], Task> OnAntiProtectionTriggered = delegate { return Task.CompletedTask; };

		public ProtectionService(DiscordSocketClient client, DbService db, MuteService mute) {
			_db = db;
			_mute = mute;
			_log = LogManager.GetCurrentClassLogger();

			client.UserJoined += UserJoinedAntiRaidHandler;
			client.MessageReceived += MessageReceivedAntiSpamHandler;
		}

		public void ResetSpamForGuild(ulong guildId) {
			if(_antiSpamGuilds.TryGetValue(guildId, out var antiSpamStats)) {
				foreach(var userSpamStats in antiSpamStats.UserStats) {
					userSpamStats.Value.Dispose();
				}
			}
		}

		private Task UserJoinedAntiRaidHandler(SocketGuildUser user) {
			if(user.IsBot || !_antiRaidGuilds.TryGetOrCreate(user.Guild.Id, out var antiRaidStats, new AntiRaidStats()) || !antiRaidStats.RaidUsers.Add(user))
				return Task.CompletedTask;

			var _ = Task.Run(async () => {
				try {
					using var uow = _db.UnitOfWork;
					var settings = uow.GuildConfigs.For(user.Guild.Id, set => set.Include(x => x.AntiRaidSetting)).AntiRaidSetting;

					++antiRaidStats.UsersCount;

					if(antiRaidStats.UsersCount >= settings.UserThreshold) {
						var users = antiRaidStats.RaidUsers.ToArray();
						antiRaidStats.RaidUsers.Clear();

						await PunishUsers(settings.Action, ProtectionType.Raiding, 0, users).ConfigureAwait(false);
					}
					await Task.Delay(1000 * settings.Seconds).ConfigureAwait(false);

					antiRaidStats.RaidUsers.TryRemove(user);
					--antiRaidStats.UsersCount;
				} catch { }
			});
			return Task.CompletedTask;
		}

		private Task MessageReceivedAntiSpamHandler(SocketMessage message) {
			if(!(message is IUserMessage userMessage) || userMessage.Author.IsBot || !(userMessage.Channel is ITextChannel channel) || !_antiSpamGuilds.TryGetValue(channel.Guild.Id, out var antiSpamStats))
				return Task.CompletedTask;

			var _ = Task.Run(async () => {
				try {
					using var uow = _db.UnitOfWork;
					var settings = uow.GuildConfigs.For(channel.GuildId, set => set.Include(x => x.AntiSpamSetting).ThenInclude(x => x.IgnoredChannels)).AntiSpamSetting;

					if(settings.IgnoredChannels.Any(x => x.ChannelId == channel.Id))
						return;

					var stats = antiSpamStats.UserStats.AddOrUpdate(userMessage.Author.Id, (id) => new UserSpamStats(userMessage), (id, old) => {
						old.ApplyNextMessage(userMessage);
						return old;
					});

					if(stats.Count >= settings.MessageThreshold && antiSpamStats.UserStats.TryRemove(userMessage.Author.Id, out stats)) {
						stats.Dispose();
						await PunishUsers(settings.Action, ProtectionType.Spamming, settings.MuteTime, (IGuildUser)userMessage.Author).ConfigureAwait(false);
					}
				} catch { }
			});
			return Task.CompletedTask;
		}


		private async Task PunishUsers(PunishmentAction action, ProtectionType pt, int muteTime, params IGuildUser[] gus) {
			_log.Info($"[{pt}] - Punishing [{gus.Length}] users with [{action}] in {gus[0].Guild.Name} guild");
			foreach(var gu in gus) {
				switch(action) {
					case PunishmentAction.Mute:
						try {
							if(muteTime <= 0)
								await _mute.MuteUser(gu).ConfigureAwait(false);
							else
								await _mute.TimedMute(gu, TimeSpan.FromSeconds(muteTime)).ConfigureAwait(false);
						} catch(Exception ex) { _log.Warn(ex, "I can't apply punishment"); }
						break;
					case PunishmentAction.Kick:
						try {
							await gu.KickAsync().ConfigureAwait(false);
						} catch(Exception ex) { _log.Warn(ex, "I can't apply punishment"); }
						break;
					case PunishmentAction.Softban:
						try {
							await gu.Guild.AddBanAsync(gu, 7).ConfigureAwait(false);
							try {
								await gu.Guild.RemoveBanAsync(gu).ConfigureAwait(false);
							} catch {
								await gu.Guild.RemoveBanAsync(gu).ConfigureAwait(false);
							}
						} catch(Exception ex) { _log.Warn(ex, "I can't apply punishment"); }
						break;
					case PunishmentAction.Ban:
						try {
							await gu.Guild.AddBanAsync(gu, 7).ConfigureAwait(false);
						} catch(Exception ex) { _log.Warn(ex, "I can't apply punishment"); }
						break;
				}
			}

			await OnAntiProtectionTriggered(action, pt, gus).ConfigureAwait(false);
		}
	}
}
