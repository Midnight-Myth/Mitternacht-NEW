using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Common;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Services.Impl;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Mitternacht.Modules.Games.Services {
	public class PlantAndPickService : IMService {
		private readonly IBotConfigProvider _bcp;
		private readonly DbService _db;
		private readonly CommandHandler _cmdHandler;
		private readonly StringService _strings;
		private readonly IImagesService _images;

		//channelid/message
		public ConcurrentDictionary<ulong, List<IUserMessage>> PlantedFlowers { get; } = new ConcurrentDictionary<ulong, List<IUserMessage>>();
		//channelId/last generation
		public ConcurrentDictionary<ulong, DateTime> LastGenerations { get; } = new ConcurrentDictionary<ulong, DateTime>();

		public PlantAndPickService(DiscordSocketClient client, IBotConfigProvider bcp, DbService db, CommandHandler cmdHandler, StringService strings, IImagesService images) {
			_bcp = bcp;
			_db = db;
			_cmdHandler = cmdHandler;
			_strings = strings;
			_images = images;

			client.MessageReceived += PotentialFlowerGeneration;
		}

		private string GetText(IGuildChannel ch, string key, params object[] rep)
			=> _strings.GetText("games", key, ch.GuildId, rep);

		public (string Name, ImmutableArray<byte> Data) GetRandomCurrencyImage() {
			var rng = new NadekoRandom();
			return _images.Currency[rng.Next(0, _images.Currency.Length)];
		}

		private Task PotentialFlowerGeneration(SocketMessage imsg) {
			if(!(imsg is SocketUserMessage msg) || msg.Author.IsBot)
				return Task.CompletedTask;

			if(!(imsg.Channel is ITextChannel channel))
				return Task.CompletedTask;

			using var uow = _db.UnitOfWork;

			if(!uow.GuildConfigs.For(channel.GuildId, set => set.Include(x => x.GenerateCurrencyChannelIds)).GenerateCurrencyChannelIds.Any(gcc => gcc.ChannelId == channel.Id))
				return Task.CompletedTask;

			var _ = Task.Run(async () => {
				try {
					var lastGeneration = LastGenerations.GetOrAdd(channel.Id, DateTime.MinValue);
					var rng = new NadekoRandom();

					if (DateTime.UtcNow - TimeSpan.FromSeconds(_bcp.BotConfig.CurrencyGenerationCooldown) < lastGeneration) //recently generated in this channel, don't generate again
                        return;

					var num = rng.Next(1, 101) + _bcp.BotConfig.CurrencyGenerationChance * 100;
					if (num > 100 && LastGenerations.TryUpdate(channel.Id, DateTime.UtcNow, lastGeneration)) {
						var dropAmount = _bcp.BotConfig.CurrencyDropAmount;
						var dropAmountMax = _bcp.BotConfig.CurrencyDropAmountMax;

						if (dropAmountMax != null && dropAmountMax > dropAmount)
							dropAmount = new NadekoRandom().Next(dropAmount, dropAmountMax.Value + 1);

						if (dropAmount > 0) {
							var msgs = new IUserMessage[dropAmount];
							var prefix = _cmdHandler.GetPrefix(channel.Guild.Id);
							var toSend = dropAmount == 1
								? $"{GetText(channel, "curgen_sn", _bcp.BotConfig.CurrencySign)} {GetText(channel, "pick_sn", prefix)}"
								: $"{GetText(channel, "curgen_pl", dropAmount, _bcp.BotConfig.CurrencySign)} {GetText(channel, "pick_pl", prefix)}";
							var (Name, Data) = GetRandomCurrencyImage();
							using (var fileStream = Data.ToStream()) {
								var sent = await channel.SendFileAsync(fileStream, Name, toSend).ConfigureAwait(false);
								msgs[0] = sent;
							}

							PlantedFlowers.AddOrUpdate(channel.Id, msgs.ToList(), (id, old) => { old.AddRange(msgs); return old; });
						}
					}
				} catch (Exception ex) {
					LogManager.GetCurrentClassLogger().Warn(ex);
				}
			});
			return Task.CompletedTask;
		}
	}
}
