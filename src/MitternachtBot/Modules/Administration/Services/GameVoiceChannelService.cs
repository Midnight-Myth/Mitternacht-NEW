using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Services.Impl;
using NLog;

namespace Mitternacht.Modules.Administration.Services {
	public class GameVoiceChannelService : IMService {
		private readonly DbService _db;
		private readonly Logger _log;

		public GameVoiceChannelService(DiscordSocketClient client, DbService db) {
			_db = db;
			_log = LogManager.GetCurrentClassLogger();

			client.UserVoiceStateUpdated += Client_UserVoiceStateUpdated;
		}

		private Task Client_UserVoiceStateUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState) {
			var _ = Task.Run(async () => {
				try {
					if (!(user is SocketGuildUser guildUser))
						return;

					var game = guildUser.Activity?.Name?.TrimTo(50);

					if (oldState.VoiceChannel == newState.VoiceChannel || newState.VoiceChannel == null)
						return;

					using var uow = _db.UnitOfWork;

					if (uow.GuildConfigs.For(guildUser.Guild.Id).GameVoiceChannel == newState.VoiceChannel.Id || string.IsNullOrWhiteSpace(game))
						return;

					var vch = guildUser.Guild.VoiceChannels.FirstOrDefault(x => x.Name.Equals(game, StringComparison.OrdinalIgnoreCase));

					if (vch == null)
						return;

					await Task.Delay(1000).ConfigureAwait(false);
					await guildUser.ModifyAsync(gu => gu.Channel = vch).ConfigureAwait(false);
				} catch (Exception ex) {
					_log.Warn(ex);
				}
			});

			return Task.CompletedTask;
		}
	}
}
