﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NadekoBot.Modules.Administration.Common;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NLog;

namespace NadekoBot.Modules.Administration.Services
{
    public class ProtectionService : INService
    {
        public readonly ConcurrentDictionary<ulong, AntiRaidStats> AntiRaidGuilds =
                new ConcurrentDictionary<ulong, AntiRaidStats>();
        // guildId | (userId|messages)
        public readonly ConcurrentDictionary<ulong, AntiSpamStats> AntiSpamGuilds =
                new ConcurrentDictionary<ulong, AntiSpamStats>();
        
        public event Func<PunishmentAction, ProtectionType, IGuildUser[], Task> OnAntiProtectionTriggered = delegate { return Task.CompletedTask; };

        private readonly Logger _log;
        private readonly DiscordSocketClient _client;
        private readonly MuteService _mute;

        public ProtectionService(DiscordSocketClient client, IEnumerable<GuildConfig> gcs, MuteService mute)
        {
            _log = LogManager.GetCurrentClassLogger();
            _client = client;
            _mute = mute;

            foreach (var gc in gcs)
            {
                var raid = gc.AntiRaidSetting;
                var spam = gc.AntiSpamSetting;

                if (raid != null)
                {
                    var raidStats = new AntiRaidStats() { AntiRaidSettings = raid };
                    AntiRaidGuilds.TryAdd(gc.GuildId, raidStats);
                }

                if (spam != null)
                    AntiSpamGuilds.TryAdd(gc.GuildId, new AntiSpamStats() { AntiSpamSettings = spam });
            }

            _client.MessageReceived += (imsg) =>
            {
                var msg = imsg as IUserMessage;
                if (msg == null || msg.Author.IsBot)
                    return Task.CompletedTask;

                var channel = msg.Channel as ITextChannel;
                if (channel == null)
                    return Task.CompletedTask;
                var _ = Task.Run(async () =>
                {
                    try
                    {
                        if (!AntiSpamGuilds.TryGetValue(channel.Guild.Id, out var spamSettings) ||
                            spamSettings.AntiSpamSettings.IgnoredChannels.Contains(new AntiSpamIgnore()
                            {
                                ChannelId = channel.Id
                            }))
                            return;

                        var stats = spamSettings.UserStats.AddOrUpdate(msg.Author.Id, (id) => new UserSpamStats(msg),
                            (id, old) =>
                            {
                                old.ApplyNextMessage(msg); return old;
                            });

                        if (stats.Count >= spamSettings.AntiSpamSettings.MessageThreshold)
                        {
                            if (spamSettings.UserStats.TryRemove(msg.Author.Id, out stats))
                            {
                                stats.Dispose();
                                await PunishUsers(spamSettings.AntiSpamSettings.Action, ProtectionType.Spamming, (IGuildUser)msg.Author)
                                    .ConfigureAwait(false);
                            }
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                });
                return Task.CompletedTask;
            };

            _client.UserJoined += (usr) =>
            {
                if (usr.IsBot)
                    return Task.CompletedTask;
                if (!AntiRaidGuilds.TryGetValue(usr.Guild.Id, out var settings))
                    return Task.CompletedTask;
                if (!settings.RaidUsers.Add(usr))
                    return Task.CompletedTask;

                var _ = Task.Run(async () =>
                {
                    try
                    {
                        ++settings.UsersCount;

                        if (settings.UsersCount >= settings.AntiRaidSettings.UserThreshold)
                        {
                            var users = settings.RaidUsers.ToArray();
                            settings.RaidUsers.Clear();

                            await PunishUsers(settings.AntiRaidSettings.Action, ProtectionType.Raiding, users).ConfigureAwait(false);
                        }
                        await Task.Delay(1000 * settings.AntiRaidSettings.Seconds).ConfigureAwait(false);

                        settings.RaidUsers.TryRemove(usr);
                        --settings.UsersCount;

                    }
                    catch
                    {
                        // ignored
                    }
                });
                return Task.CompletedTask;
            };
        }


        private async Task PunishUsers(PunishmentAction action, ProtectionType pt, params IGuildUser[] gus)
        {
            _log.Info($"[{pt}] - Punishing [{gus.Length}] users with [{action}] in {gus[0].Guild.Name} guild");
            foreach (var gu in gus)
            {
                switch (action)
                {
                    case PunishmentAction.Mute:
                        try
                        {
                            await _mute.MuteUser(gu).ConfigureAwait(false);
                        }
                        catch (Exception ex) { _log.Warn(ex, "I can't apply punishement"); }
                        break;
                    case PunishmentAction.Kick:
                        try
                        {
                            await gu.KickAsync().ConfigureAwait(false);
                        }
                        catch (Exception ex) { _log.Warn(ex, "I can't apply punishement"); }
                        break;
                    case PunishmentAction.Softban:
                        try
                        {
                            await gu.Guild.AddBanAsync(gu, 7).ConfigureAwait(false);
                            try
                            {
                                await gu.Guild.RemoveBanAsync(gu).ConfigureAwait(false);
                            }
                            catch
                            {
                                await gu.Guild.RemoveBanAsync(gu).ConfigureAwait(false);
                                // try it twice, really don't want to ban user if 
                                // only kick has been specified as the punishement
                            }
                        }
                        catch (Exception ex) { _log.Warn(ex, "I can't apply punishment"); }
                        break;
                    case PunishmentAction.Ban:
                        try
                        {
                            await gu.Guild.AddBanAsync(gu, 7).ConfigureAwait(false);
                        }
                        catch (Exception ex) { _log.Warn(ex, "I can't apply punishment"); }
                        break;
                }
            }
            await OnAntiProtectionTriggered(action, pt, gus).ConfigureAwait(false);
        }
    }
}
