﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NadekoBot.Common;
using NadekoBot.Common.Collections;
using NadekoBot.Extensions;
using NadekoBot.Modules.Games.Common;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NadekoBot.Services.Impl;
using Newtonsoft.Json;
using NLog;

namespace NadekoBot.Modules.Games.Services
{
    public class GamesService : INService
    {
        private readonly IBotConfigProvider _bc;

        public readonly ConcurrentDictionary<ulong, GirlRating> GirlRatings = new ConcurrentDictionary<ulong, GirlRating>();
        public readonly ImmutableArray<string> EightBallResponses;

        private readonly Timer _t;
        private readonly DiscordSocketClient _client;
        private readonly NadekoStrings _strings;
        private readonly IImagesService _images;
        private readonly Logger _log;

        public readonly string TypingArticlesPath = "data/typing_articles2.json";
        private readonly CommandHandler _cmdHandler;

        public List<TypingArticle> TypingArticles { get; } = new List<TypingArticle>();

        public GamesService(DiscordSocketClient client, IBotConfigProvider bc, IEnumerable<GuildConfig> gcs, 
            NadekoStrings strings, IImagesService images, CommandHandler cmdHandler)
        {
            _bc = bc;
            _client = client;
            _strings = strings;
            _images = images;
            _cmdHandler = cmdHandler;
            _log = LogManager.GetCurrentClassLogger();

            //8ball
            EightBallResponses = _bc.BotConfig.EightBallResponses.Select(ebr => ebr.Text).ToImmutableArray();

            //girl ratings
            _t = new Timer((_) =>
            {
                GirlRatings.Clear();

            }, null, TimeSpan.FromDays(1), TimeSpan.FromDays(1));

            //plantpick
            client.MessageReceived += PotentialFlowerGeneration;
            GenerationChannels = new ConcurrentHashSet<ulong>(gcs
                .SelectMany(c => c.GenerateCurrencyChannelIds.Select(obj => obj.ChannelId)));

            try
            {
                TypingArticles = JsonConvert.DeserializeObject<List<TypingArticle>>(File.ReadAllText(TypingArticlesPath));
            }
            catch (Exception ex)
            {
                _log.Warn("Error while loading typing articles {0}", ex.ToString());
                TypingArticles = new List<TypingArticle>();
            }
        }

        public void AddTypingArticle(IUser user, string text)
        {
            TypingArticles.Add(new TypingArticle
            {
                Title = $"Text added on {DateTime.UtcNow} by {user}",
                Text = text.SanitizeMentions(),
            });

            File.WriteAllText(TypingArticlesPath, JsonConvert.SerializeObject(TypingArticles));
        }

        public ConcurrentHashSet<ulong> GenerationChannels { get; }
        //channelid/message
        public ConcurrentDictionary<ulong, List<IUserMessage>> PlantedFlowers { get; } = new ConcurrentDictionary<ulong, List<IUserMessage>>();
        //channelId/last generation
        public ConcurrentDictionary<ulong, DateTime> LastGenerations { get; } = new ConcurrentDictionary<ulong, DateTime>();

        private ConcurrentDictionary<ulong, object> _locks { get; } = new ConcurrentDictionary<ulong, object>();
        
        public (string Name, ImmutableArray<byte> Data) GetRandomCurrencyImage()
        {
            var rng = new NadekoRandom();
            return _images.Currency[rng.Next(0, _images.Currency.Length)];
        }

        private string GetText(ITextChannel ch, string key, params object[] rep)
            => _strings.GetText(key, ch.GuildId, "Games".ToLowerInvariant(), rep);

        private Task PotentialFlowerGeneration(SocketMessage imsg)
        {
            var msg = imsg as SocketUserMessage;
            if (msg == null || msg.Author.IsBot)
                return Task.CompletedTask;

            var channel = imsg.Channel as ITextChannel;
            if (channel == null)
                return Task.CompletedTask;

            if (!GenerationChannels.Contains(channel.Id))
                return Task.CompletedTask;

            var _ = Task.Run(async () =>
            {
                try
                {
                    var lastGeneration = LastGenerations.GetOrAdd(channel.Id, DateTime.MinValue);
                    var rng = new NadekoRandom();

                    if (DateTime.UtcNow - TimeSpan.FromSeconds(_bc.BotConfig.CurrencyGenerationCooldown) < lastGeneration) //recently generated in this channel, don't generate again
                        return;

                    var num = rng.Next(1, 101) + _bc.BotConfig.CurrencyGenerationChance * 100;
                    if (num > 100 && LastGenerations.TryUpdate(channel.Id, DateTime.UtcNow, lastGeneration))
                    {
                        var dropAmount = _bc.BotConfig.CurrencyDropAmount;
                        var dropAmountMax = _bc.BotConfig.CurrencyDropAmountMax;

                        if (dropAmountMax != null && dropAmountMax > dropAmount)
                            dropAmount = new NadekoRandom().Next(dropAmount, dropAmountMax.Value + 1);

                        if (dropAmount > 0)
                        {
                            var msgs = new IUserMessage[dropAmount];
                            var prefix = _cmdHandler.GetPrefix(channel.Guild.Id);
                            var toSend = dropAmount == 1
                                ? GetText(channel, "curgen_sn", _bc.BotConfig.CurrencySign)
                                    + " " + GetText(channel, "pick_sn", prefix)
                                : GetText(channel, "curgen_pl", dropAmount, _bc.BotConfig.CurrencySign)
                                    + " " + GetText(channel, "pick_pl", prefix);
                            var file = GetRandomCurrencyImage();
                            using (var fileStream = file.Data.ToStream())
                            {
                                var sent = await channel.SendFileAsync(
                                    fileStream,
                                    file.Name,
                                    toSend).ConfigureAwait(false);

                                msgs[0] = sent;
                            }

                            PlantedFlowers.AddOrUpdate(channel.Id, msgs.ToList(), (id, old) => { old.AddRange(msgs); return old; });
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.GetCurrentClassLogger().Warn(ex);
                }
            });
            return Task.CompletedTask;
        }
    }
}
