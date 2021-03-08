using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Mitternacht.Services;
using Mitternacht.Services.Impl;

namespace Mitternacht.Modules.Utility.Services {
	public class CountToNumberService : IMService {
		private readonly DiscordSocketClient _client;
		private readonly DbService _db;

		private readonly Random _random = new Random();

		public CountToNumberService(DiscordSocketClient client, DbService db) {
			_client = client;
			_db = db;

			_client.MessageReceived += CountToMessageReceived;
		}

		public async Task CountToMessageReceived(SocketMessage msg) {
			if(!msg.Author.IsBot && msg.Channel is ITextChannel channel) {
				using var uow = _db.UnitOfWork;
				var gc = uow.GuildConfigs.For(channel.GuildId);

				if(gc.CountToNumberChannelId == channel.Id) {
					var match = Regex.Match(msg.Content.Trim(), "\\A(\\d+)");
					
					if(match.Success && ulong.TryParse(match.Groups[1].Value, out var currentNumber)) {
						var lastMessages = (await channel.GetMessagesAsync(10).FlattenAsync().ConfigureAwait(false)).ToList();
						var messageIndex = lastMessages.FindIndex(m => m.Id == msg.Id);
						var previousMessage = lastMessages[messageIndex+1];
						var previousMatch = Regex.Match(previousMessage.Content.Trim(), "\\A(\\d+)");

						if(gc.CountToNumberDeleteWrongMessages && (previousMatch.Success && ulong.TryParse(previousMatch.Groups[1].Value, out var previousNumberParsingSuccess) && currentNumber - previousNumberParsingSuccess != 1 || previousMessage.Author.Id == msg.Author.Id)) {
							await msg.DeleteAsync().ConfigureAwait(false);
						} else if(_random.NextDouble() < gc.CountToNumberMessageChance) {
							await channel.SendMessageAsync($"{currentNumber + 1}").ConfigureAwait(false);
						}
					} else if(gc.CountToNumberDeleteWrongMessages) {
						await msg.DeleteAsync().ConfigureAwait(false);
					}
				}
			}
		}
	}
}
