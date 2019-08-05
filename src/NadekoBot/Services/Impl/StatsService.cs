﻿using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Mitternacht.Extensions;

namespace Mitternacht.Services.Impl
{
    public class StatsService : IStatsService
    {
        private readonly DiscordSocketClient _client;
        private readonly IBotCredentials _creds;
        private readonly DateTime _started;

        public const string BotVersion = "1.10.0";

        public (ulong userId, string backupName)[] AuthorIdBackupNames => new (ulong userId, string backupName)[] {
            (119521688768610304, "Midnight Myth"),
            (240476349116973067, "expeehaa")
        };

        public string Author => string.Join(", ", AuthorIdBackupNames.Select(t => _client.GetUser(t.userId)?.ToString() ?? t.backupName));
        public string Library => "Discord.Net, GommeHDnetForumAPI, MinecraftQuery";
        public string Heap => Math.Round((double)GC.GetTotalMemory(false) / 1.MiB(), 2).ToString(CultureInfo.InvariantCulture);
        public double MessagesPerSecond => MessageCounter / GetUptime().TotalSeconds;

        private long _textChannels;
        public long TextChannels => Interlocked.Read(ref _textChannels);
        private long _voiceChannels;
        public long VoiceChannels => Interlocked.Read(ref _voiceChannels);
        private long _messageCounter;
        public long MessageCounter => Interlocked.Read(ref _messageCounter);
        private long _commandsRan;
        public long CommandsRan => Interlocked.Read(ref _commandsRan);
        
        private readonly ShardsCoordinator _sc;

        public int GuildCount => _sc?.GuildCount ?? _client.Guilds.Count;

        public StatsService(DiscordSocketClient client, CommandHandler cmdHandler, IBotCredentials creds, MitternachtBot mitternacht)
        {
            _client = client;
            _creds = creds;
            _sc = mitternacht.ShardCoord;

            _started = DateTime.UtcNow;
            _client.MessageReceived += _ => Task.FromResult(Interlocked.Increment(ref _messageCounter));
            cmdHandler.CommandExecuted += (_, e) => Task.FromResult(Interlocked.Increment(ref _commandsRan));

            _client.ChannelCreated += c =>
            {
                var _ = Task.Run(() => {
                    switch (c) {
                        case ITextChannel _:
                            Interlocked.Increment(ref _textChannels);
                            break;
                        case IVoiceChannel _:
                            Interlocked.Increment(ref _voiceChannels);
                            break;
                    }
                });

                return Task.CompletedTask;
            };

            _client.ChannelDestroyed += c =>
            {
                var _ = Task.Run(() => {
                    switch (c) {
                        case ITextChannel _:
                            Interlocked.Decrement(ref _textChannels);
                            break;
                        case IVoiceChannel _:
                            Interlocked.Decrement(ref _voiceChannels);
                            break;
                    }
                });

                return Task.CompletedTask;
            };

            _client.GuildAvailable += g =>
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

            _client.JoinedGuild += g =>
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

            _client.GuildUnavailable += g =>
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

            _client.LeftGuild += g =>
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

        public TimeSpan GetUptime() 
            => DateTime.UtcNow - _started;

        public string GetUptimeString(string separator = ", ")
        {
            var time = GetUptime();
            return $"{time.Days} days{separator}{time.Hours} hours{separator}{time.Minutes} minutes";
        }
    }
}