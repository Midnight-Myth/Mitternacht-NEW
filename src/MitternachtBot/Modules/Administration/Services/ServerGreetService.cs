using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Mitternacht.Common;
using Mitternacht.Common.Replacements;
using Mitternacht.Extensions;
using Mitternacht.Services;
using NLog;

namespace Mitternacht.Modules.Administration.Services {
	public class ServerGreetService : IMService {
		private readonly DiscordSocketClient _client;
		private readonly DbService _db;
		private readonly Logger _log;

		public ServerGreetService(DiscordSocketClient client, DbService db) {
			_client = client;
			_db = db;
			_log = LogManager.GetCurrentClassLogger();

			_client.UserJoined += UserJoined;
			_client.UserLeft += UserLeft;
		}

		private async Task SendGreetMessage(IGuildUser guildUser, IMessageChannel channel, string messageText, int autoDeleteTimer) {
			if(channel != null) {
				var rep = new ReplacementBuilder()
								.WithDefault(guildUser, channel, guildUser.Guild, _client)
								.Build();

				if(CREmbed.TryParse(messageText, out var embedData)) {
					rep.Replace(embedData);
					try {
						var toDelete = await channel.EmbedAsync(embedData.ToEmbedBuilder(), embedData.PlainText?.SanitizeMentions() ?? "").ConfigureAwait(false);

						if(autoDeleteTimer > 0) {
							toDelete.DeleteAfter(autoDeleteTimer);
						}
					} catch(Exception ex) {
						_log.Warn(ex);
					}
				} else {
					var msg = rep.Replace(messageText);

					if(!string.IsNullOrWhiteSpace(msg)) {
						try {
							var toDelete = await channel.SendMessageAsync(msg.SanitizeMentions()).ConfigureAwait(false);

							if(autoDeleteTimer > 0) {
								toDelete.DeleteAfter(autoDeleteTimer);
							}
						} catch(Exception ex) {
							_log.Warn(ex);
						}
					}
				}
			}
		}

		private Task UserLeft(IGuildUser guildUser) {
			var _ = Task.Run(async () => {
				try {
					using var uow = _db.UnitOfWork;
					var gc = uow.GuildConfigs.For(guildUser.GuildId);

					if(gc.SendChannelByeMessage) {
						await SendGreetMessage(guildUser, await guildUser.Guild.GetTextChannelAsync(gc.ByeMessageChannelId).ConfigureAwait(false), gc.ChannelByeMessageText, gc.AutoDeleteByeMessagesTimer).ConfigureAwait(false);
					}
				} catch { }
			});
			return Task.CompletedTask;
		}

		private Task UserJoined(IGuildUser guildUser) {
			var _ = Task.Run(async () => {
				try {
					using var uow = _db.UnitOfWork;
					var gc = uow.GuildConfigs.For(guildUser.GuildId);

					if(gc.SendChannelGreetMessage) {
						await SendGreetMessage(guildUser, await guildUser.Guild.GetTextChannelAsync(gc.GreetMessageChannelId).ConfigureAwait(false), gc.ChannelGreetMessageText, gc.AutoDeleteGreetMessagesTimer).ConfigureAwait(false);
					}

					if(gc.SendDmGreetMessage) {
						await SendGreetMessage(guildUser, await guildUser.GetOrCreateDMChannelAsync().ConfigureAwait(false), gc.DmGreetMessageText, 0).ConfigureAwait(false);
					}
				} catch { }
			});
			return Task.CompletedTask;
		}
	}
}