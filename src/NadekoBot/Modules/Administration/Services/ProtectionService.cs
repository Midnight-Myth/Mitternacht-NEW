﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Mitternacht.Modules.Administration.Common;
using Mitternacht.Modules.Administration.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;
using NLog;

namespace Mitternacht.Modules.Administration.Services
{
    public class ProtectionService : IMService
    {
        public readonly ConcurrentDictionary<ulong, AntiRaidStats> AntiRaidGuilds = new ConcurrentDictionary<ulong, AntiRaidStats>();
        // guildId | (userId|messages)
        public readonly ConcurrentDictionary<ulong, AntiSpamStats> AntiSpamGuilds = new ConcurrentDictionary<ulong, AntiSpamStats>();

        public event Func<PunishmentAction, ProtectionType, IGuildUser[], Task> OnAntiProtectionTriggered = delegate { return Task.CompletedTask; };

        private readonly Logger _log;
        private readonly MuteService _mute;

        public ProtectionService(DiscordSocketClient client, IEnumerable<GuildConfig> gcs, MuteService mute)
        {
            _log = LogManager.GetCurrentClassLogger();
            _mute = mute;

            foreach (var gc in gcs)
            {
                var raid = gc.AntiRaidSetting;
                var spam = gc.AntiSpamSetting;

                if (raid != null)
                {
                    var raidStats = new AntiRaidStats { AntiRaidSettings = raid };
                    AntiRaidGuilds.TryAdd(gc.GuildId, raidStats);
                }

                if (spam != null)
                    AntiSpamGuilds.TryAdd(gc.GuildId, new AntiSpamStats { AntiSpamSettings = spam });
            }

            client.MessageReceived += imsg =>
            {
                if (!(imsg is IUserMessage msg) || msg.Author.IsBot)
                    return Task.CompletedTask;

                if (!(msg.Channel is ITextChannel channel))
                    return Task.CompletedTask;
                var _ = Task.Run(async () =>
                {
                    try
                    {
                        if (!AntiSpamGuilds.TryGetValue(channel.Guild.Id, out var spamSettings) ||
                            spamSettings.AntiSpamSettings.IgnoredChannels.Contains(new AntiSpamIgnore
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
                                await PunishUsers(spamSettings.AntiSpamSettings.Action, ProtectionType.Spamming, spamSettings.AntiSpamSettings.MuteTime, (IGuildUser)msg.Author)
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

            client.UserJoined += usr =>
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

                            await PunishUsers(settings.AntiRaidSettings.Action, ProtectionType.Raiding, 0, users).ConfigureAwait(false);
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


        private async Task PunishUsers(PunishmentAction action, ProtectionType pt, int muteTime, params IGuildUser[] gus)
        {
            _log.Info($"[{pt}] - Punishing [{gus.Length}] users with [{action}] in {gus[0].Guild.Name} guild");
            foreach (var gu in gus)
            {
                switch (action)
                {
                    case PunishmentAction.Mute:
                        try
                        {
                            if (muteTime <= 0)
                                await _mute.MuteUser(gu, MuteType.Chat).ConfigureAwait(false);
                            else
                                await _mute.TimedMute(gu, TimeSpan.FromSeconds(muteTime), MuteType.Chat).ConfigureAwait(false);
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
