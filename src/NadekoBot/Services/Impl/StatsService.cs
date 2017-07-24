﻿using Discord;
using Discord.WebSocket;
using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Services.Impl
{
    public class StatsService : IStatsService
    {
        private readonly DiscordSocketClient _client;
        private readonly IBotCredentials _creds;
        private readonly DateTime _started;

        public const string BotVersion = "1.6";

        public string Author => "Kwoth#2560";
        public string Library => "Discord.Net";
        public string Heap =>
            Math.Round((double)GC.GetTotalMemory(false) / 1.MiB(), 2).ToString(CultureInfo.InvariantCulture);
        public double MessagesPerSecond => MessageCounter / GetUptime().TotalSeconds;

        private long _textChannels;
        public long TextChannels => Interlocked.Read(ref _textChannels);
        private long _voiceChannels;
        public long VoiceChannels => Interlocked.Read(ref _voiceChannels);
        private long _messageCounter;
        public long MessageCounter => Interlocked.Read(ref _messageCounter);
        private long _commandsRan;
        public long CommandsRan => Interlocked.Read(ref _commandsRan);

        private readonly Timer _carbonitexTimer;
        private readonly Timer _dataTimer;
        private readonly ShardsCoordinator _sc;

        public int GuildCount =>
            _sc?.GuildCount ?? _client.Guilds.Count();

        public StatsService(DiscordSocketClient client, CommandHandler cmdHandler, IBotCredentials creds, NadekoBot nadeko)
        {
            _client = client;
            _creds = creds;
            _sc = nadeko.ShardCoord;

            _started = DateTime.UtcNow;
            _client.MessageReceived += _ => Task.FromResult(Interlocked.Increment(ref _messageCounter));
            cmdHandler.CommandExecuted += (_, e) => Task.FromResult(Interlocked.Increment(ref _commandsRan));

            _client.ChannelCreated += (c) =>
            {
                var _ = Task.Run(() =>
                {
                    if (c is ITextChannel)
                        Interlocked.Increment(ref _textChannels);
                    else if (c is IVoiceChannel)
                        Interlocked.Increment(ref _voiceChannels);
                });

                return Task.CompletedTask;
            };

            _client.ChannelDestroyed += (c) =>
            {
                var _ = Task.Run(() =>
                {
                    if (c is ITextChannel)
                        Interlocked.Decrement(ref _textChannels);
                    else if (c is IVoiceChannel)
                        Interlocked.Decrement(ref _voiceChannels);
                });

                return Task.CompletedTask;
            };

            _client.GuildAvailable += (g) =>
            {
                var _ = Task.Run(() =>
                {
                    var tc = g.Channels.Count(cx => cx is ITextChannel);
                    var vc = g.Channels.Count - tc;
                    Interlocked.Add(ref _textChannels, tc);
                    Interlocked.Add(ref _voiceChannels, vc);
                });
                return Task.CompletedTask;
            };

            _client.JoinedGuild += (g) =>
            {
                var _ = Task.Run(() =>
                {
                    var tc = g.Channels.Count(cx => cx is ITextChannel);
                    var vc = g.Channels.Count - tc;
                    Interlocked.Add(ref _textChannels, tc);
                    Interlocked.Add(ref _voiceChannels, vc);
                });
                return Task.CompletedTask;
            };

            _client.GuildUnavailable += (g) =>
            {
                var _ = Task.Run(() =>
                {
                    var tc = g.Channels.Count(cx => cx is ITextChannel);
                    var vc = g.Channels.Count - tc;
                    Interlocked.Add(ref _textChannels, -tc);
                    Interlocked.Add(ref _voiceChannels, -vc);
                });

                return Task.CompletedTask;
            };

            _client.LeftGuild += (g) =>
            {
                var _ = Task.Run(() =>
                {
                    var tc = g.Channels.Count(cx => cx is ITextChannel);
                    var vc = g.Channels.Count - tc;
                    Interlocked.Add(ref _textChannels, -tc);
                    Interlocked.Add(ref _voiceChannels, -vc);
                });

                return Task.CompletedTask;
            };

            if (_sc != null)
            {
                _carbonitexTimer = new Timer(async (state) =>
                {
                    if (string.IsNullOrWhiteSpace(_creds.CarbonKey))
                        return;
                    try
                    {
                        using (var http = new HttpClient())
                        {
                            using (var content = new FormUrlEncodedContent(
                                new Dictionary<string, string> {
                                { "servercount", _sc.GuildCount.ToString() },
                                { "key", _creds.CarbonKey }}))
                            {
                                content.Headers.Clear();
                                content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                                await http.PostAsync("https://www.carbonitex.net/discord/data/botdata.php", content).ConfigureAwait(false);
                            }
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));

                var platform = "other";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    platform = "linux";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    platform = "osx";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    platform = "windows";

                _dataTimer = new Timer(async (state) =>
                {
                    try
                    {
                        using (var http = new HttpClient())
                        {
                            using (var content = new FormUrlEncodedContent(
                                new Dictionary<string, string> {
                                    { "id", string.Concat(MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(_creds.ClientId.ToString())).Select(x => x.ToString("X2"))) },
                                    { "guildCount", _sc.GuildCount.ToString() },
                                    { "version", BotVersion },
                                    { "platform", platform }}))
                            {
                                content.Headers.Clear();
                                content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                                await http.PostAsync("https://selfstats.nadekobot.me/", content).ConfigureAwait(false);
                            }
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }, null, TimeSpan.FromSeconds(1), TimeSpan.FromHours(1));
            }
        }

        public void Initialize()
        {
            var guilds = _client.Guilds.ToArray();
            _textChannels = guilds.Sum(g => g.Channels.Count(cx => cx is ITextChannel));
            _voiceChannels = guilds.Sum(g => g.Channels.Count) - _textChannels;
        }

        public Task<string> Print()
        {
            SocketSelfUser curUser;
            while ((curUser = _client.CurrentUser) == null) Task.Delay(1000).ConfigureAwait(false);

            return Task.FromResult($@"
Author: [{Author}] | Library: [{Library}]
Bot Version: [{BotVersion}]
Bot ID: {curUser.Id}
Owner ID(s): {string.Join(", ", _creds.OwnerIds)}
Uptime: {GetUptimeString()}
Servers: {_client.Guilds.Count} | TextChannels: {TextChannels} | VoiceChannels: {VoiceChannels}
Commands Ran this session: {CommandsRan}
Messages: {MessageCounter} [{MessagesPerSecond:F2}/sec] Heap: [{Heap} MB]");
        }

        public TimeSpan GetUptime() =>
            DateTime.UtcNow - _started;

        public string GetUptimeString(string separator = ", ")
        {
            var time = GetUptime();
            return $"{time.Days} days{separator}{time.Hours} hours{separator}{time.Minutes} minutes";
        }
    }
}