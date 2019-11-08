using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Mitternacht.Services;

namespace Mitternacht.Modules.Utility.Services {
	public class CountToNumberService : INService {
		private readonly DiscordSocketClient _client;
		private readonly DbService _db;

		private Dictionary<ulong, (ulong? ChannelId, double MessageChance)> _countChannelIds;

		private readonly Random _random = new Random();

		public CountToNumberService(DiscordSocketClient client, DbService db) {
			_client = client;
			_db = db;

			ReloadGuildConfigs();
			_client.MessageReceived += CountUpRandom;
		}

		public void ReloadGuildConfigs() {
			using var uow = _db.UnitOfWork;
			_countChannelIds = uow.GuildConfigs.GetAll().ToDictionary(gc => gc.GuildId, gc => (gc.CountToNumberChannelId, gc.CountToNumberMessageChance));
		}

		public bool SetCountToNumberChannel(IGuild guild, ITextChannel channel) {
			if(_countChannelIds.TryGetValue(guild.Id, out var guildCountToNumberItem) && guildCountToNumberItem.ChannelId == channel?.Id)
				return false;

			using(var uow = _db.UnitOfWork) {
				var gc = uow.GuildConfigs.For(guild.Id);
				gc.CountToNumberChannelId = channel?.Id;
				uow.GuildConfigs.Update(gc);
				uow.Complete();
			}
			_countChannelIds[guild.Id] = (channel?.Id, _countChannelIds[guild.Id].MessageChance);
			return true;
		}

		public ulong? GetCountToNumberChannelId(ulong guildId)
			=> _countChannelIds.TryGetValue(guildId, out var guildCountToNumberItem) ? guildCountToNumberItem.ChannelId : null;


		public bool SetCountToNumberMessageChance(ulong guildId, double chance) {
			if(_countChannelIds.TryGetValue(guildId, out var guildCountToNumberItem) && Math.Abs(guildCountToNumberItem.MessageChance - chance) < double.Epsilon)
				return false;

			using(var uow = _db.UnitOfWork) {
				var gc = uow.GuildConfigs.For(guildId);
				gc.CountToNumberMessageChance = chance;
				uow.GuildConfigs.Update(gc);
				uow.Complete();
			}
			_countChannelIds[guildId] = (_countChannelIds[guildId].ChannelId, chance);
			return true;
		}

		public double GetCountToNumberMessageChance(ulong guildId)
			=> _countChannelIds.TryGetValue(guildId, out var guildCountToNumberItem) ? guildCountToNumberItem.MessageChance : 0;


		public async Task CountUpRandom(SocketMessage msg) {
			if(!msg.Author.IsBot) {
				var guildChannels = _countChannelIds.Where(gc => gc.Value.ChannelId != null).Select(gc => (gc.Key, gc.Value.ChannelId.Value, gc.Value.MessageChance)).ToList();
				if(guildChannels.Any(gc => gc.Value == msg.Channel.Id)) {
					var match = Regex.Match(msg.Content.Trim(), "\\A(\\d+)");
					if(match.Success) {
						var (guildId, channelId, messageChance) = guildChannels.First(gc => gc.Value == msg.Channel.Id);
						var guild = _client.GetGuild(guildId);
						if(guild != null) {
							var channel = guild.GetTextChannel(channelId);
							var currentnumber = ulong.Parse(match.Groups[1].Value);

							if(_random.NextDouble() < messageChance) {
								await channel.SendMessageAsync($"{currentnumber + 1}").ConfigureAwait(false);
							}
						}
					}
				}
			}
		}
	}
}
