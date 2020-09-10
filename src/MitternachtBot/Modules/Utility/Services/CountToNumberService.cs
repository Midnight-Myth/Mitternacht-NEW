using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Mitternacht.Services;

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
					
					if(match.Success) {
						var currentnumber = ulong.Parse(match.Groups[1].Value);

						if(_random.NextDouble() < gc.CountToNumberMessageChance) {
							await channel.SendMessageAsync($"{currentnumber + 1}").ConfigureAwait(false);
						}
					} else if(gc.CountToNumberDeleteWrongMessages) {
						await msg.DeleteAsync().ConfigureAwait(false);
					}
				}
			}
		}
	}
}
