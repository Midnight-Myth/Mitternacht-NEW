﻿using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Mitternacht.Common;
using Mitternacht.Common.Replacements;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Services.Impl;
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

		private async Task SendGreetMessage(IGuild guild, IUser user, IMessageChannel channel, string messageText, int autoDeleteTimer) {
			if(channel != null) {
				var rep = new ReplacementBuilder()
								.WithDefault(user, channel, guild, _client)
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

		private Task UserLeft(SocketGuild guild, SocketUser user) {
			var _ = Task.Run(async () => {
				try {
					using var uow = _db.UnitOfWork;
					var gc = uow.GuildConfigs.For(guild.Id);

					if(gc.SendChannelByeMessage) {
						await SendGreetMessage(guild, user, guild.GetTextChannel(gc.ByeMessageChannelId), gc.ChannelByeMessageText, gc.AutoDeleteByeMessagesTimer).ConfigureAwait(false);
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
						await SendGreetMessage(guildUser.Guild, guildUser, await guildUser.Guild.GetTextChannelAsync(gc.GreetMessageChannelId).ConfigureAwait(false), gc.ChannelGreetMessageText, gc.AutoDeleteGreetMessagesTimer).ConfigureAwait(false);
					}

					if(gc.SendDmGreetMessage) {
						await SendGreetMessage(guildUser.Guild, guildUser, await guildUser.CreateDMChannelAsync().ConfigureAwait(false), gc.DmGreetMessageText, 0).ConfigureAwait(false);
					}
				} catch { }
			});
			return Task.CompletedTask;
		}
	}
}