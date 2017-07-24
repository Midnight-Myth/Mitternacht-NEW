﻿using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Administration.Common;
using NadekoBot.Modules.Administration.Services;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class ProtectionCommands : NadekoSubmodule<ProtectionService>
        {
            private readonly MuteService _mute;
            private readonly DbService _db;

            public ProtectionCommands(MuteService mute, DbService db)
            {
                _mute = mute;
                _db = db;
            }

            private string GetAntiSpamString(AntiSpamStats stats)
            {
                var ignoredString = string.Join(", ", stats.AntiSpamSettings.IgnoredChannels.Select(c => $"<#{c.ChannelId}>"));

                if (string.IsNullOrWhiteSpace(ignoredString))
                    ignoredString = "none";
                return GetText("spam_stats",
                        Format.Bold(stats.AntiSpamSettings.MessageThreshold.ToString()), 
                        Format.Bold(stats.AntiSpamSettings.Action.ToString()), 
                        ignoredString);
            }

            private string GetAntiRaidString(AntiRaidStats stats) => GetText("raid_stats",
                Format.Bold(stats.AntiRaidSettings.UserThreshold.ToString()),
                Format.Bold(stats.AntiRaidSettings.Seconds.ToString()),
                Format.Bold(stats.AntiRaidSettings.Action.ToString()));

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task AntiRaid(int userThreshold = 5, int seconds = 10, PunishmentAction action = PunishmentAction.Mute)
            {
                if (userThreshold < 2 || userThreshold > 30)
                {
                    await ReplyErrorLocalized("raid_cnt", 2, 30).ConfigureAwait(false);
                    return;
                }

                if (seconds < 2 || seconds > 300)
                {
                    await ReplyErrorLocalized("raid_time", 2, 300).ConfigureAwait(false);
                    return;
                }

                AntiRaidStats throwaway;
                if (_service.AntiRaidGuilds.TryRemove(Context.Guild.Id, out throwaway))
                {
                    using (var uow = _db.UnitOfWork)
                    {
                        var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.AntiRaidSetting));

                        gc.AntiRaidSetting = null;
                        await uow.CompleteAsync().ConfigureAwait(false);
                    }
                    await ReplyConfirmLocalized("prot_disable", "Anti-Raid").ConfigureAwait(false);
                    return;
                }

                try
                {
                    await _mute.GetMuteRole(Context.Guild).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                    await ReplyErrorLocalized("prot_error").ConfigureAwait(false);
                    return;
                }

                var stats = new AntiRaidStats()
                {
                    AntiRaidSettings = new AntiRaidSetting()
                    {
                        Action = action,
                        Seconds = seconds,
                        UserThreshold = userThreshold,
                    }
                };

                _service.AntiRaidGuilds.AddOrUpdate(Context.Guild.Id, stats, (key, old) => stats);

                using (var uow = _db.UnitOfWork)
                {
                    var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.AntiRaidSetting));

                    gc.AntiRaidSetting = stats.AntiRaidSettings;
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                await Context.Channel.SendConfirmAsync(GetText("prot_enable", "Anti-Raid"), $"{Context.User.Mention} {GetAntiRaidString(stats)}")
                        .ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task AntiSpam(int messageCount = 3, PunishmentAction action = PunishmentAction.Mute)
            {
                if (messageCount < 2 || messageCount > 10)
                    return;

                AntiSpamStats throwaway;
                if (_service.AntiSpamGuilds.TryRemove(Context.Guild.Id, out throwaway))
                {
                    throwaway.UserStats.ForEach(x => x.Value.Dispose());
                    using (var uow = _db.UnitOfWork)
                    {
                        var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.AntiSpamSetting)
                            .ThenInclude(x => x.IgnoredChannels));

                        gc.AntiSpamSetting = null;
                        await uow.CompleteAsync().ConfigureAwait(false);
                    }
                    await ReplyConfirmLocalized("prot_disable", "Anti-Spam").ConfigureAwait(false);
                    return;
                }

                try
                {
                    await _mute.GetMuteRole(Context.Guild).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                    await ReplyErrorLocalized("prot_error").ConfigureAwait(false);
                    return;
                }

                var stats = new AntiSpamStats
                {
                    AntiSpamSettings = new AntiSpamSetting()
                    {
                        Action = action,
                        MessageThreshold = messageCount,
                    }
                };

                _service.AntiSpamGuilds.AddOrUpdate(Context.Guild.Id, stats, (key, old) => stats);

                using (var uow = _db.UnitOfWork)
                {
                    var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.AntiSpamSetting));

                    gc.AntiSpamSetting = stats.AntiSpamSettings;
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                await Context.Channel.SendConfirmAsync(GetText("prot_enable", "Anti-Spam"), $"{Context.User.Mention} {GetAntiSpamString(stats)}").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task AntispamIgnore()
            {
                var channel = (ITextChannel)Context.Channel;

                var obj = new AntiSpamIgnore()
                {
                    ChannelId = channel.Id
                };
                bool added;
                using (var uow = _db.UnitOfWork)
                {
                    var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.AntiSpamSetting).ThenInclude(x => x.IgnoredChannels));
                    var spam = gc.AntiSpamSetting;
                    if (spam == null)
                    {
                        return;
                    }

                    if (spam.IgnoredChannels.Add(obj))
                    {
                        AntiSpamStats temp;
                        if (_service.AntiSpamGuilds.TryGetValue(Context.Guild.Id, out temp))
                            temp.AntiSpamSettings.IgnoredChannels.Add(obj);
                        added = true;
                    }
                    else
                    {
                        spam.IgnoredChannels.Remove(obj);
                        AntiSpamStats temp;
                        if (_service.AntiSpamGuilds.TryGetValue(Context.Guild.Id, out temp))
                            temp.AntiSpamSettings.IgnoredChannels.Remove(obj);
                        added = false;
                    }

                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                if (added)
                    await ReplyConfirmLocalized("spam_ignore", "Anti-Spam").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("spam_not_ignore", "Anti-Spam").ConfigureAwait(false);

            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task AntiList()
            {
                AntiSpamStats spam;
                _service.AntiSpamGuilds.TryGetValue(Context.Guild.Id, out spam);

                AntiRaidStats raid;
                _service.AntiRaidGuilds.TryGetValue(Context.Guild.Id, out raid);

                if (spam == null && raid == null)
                {
                    await ReplyConfirmLocalized("prot_none").ConfigureAwait(false);
                    return;
                }

                var embed = new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("prot_active"));

                if (spam != null)
                    embed.AddField(efb => efb.WithName("Anti-Spam")
                        .WithValue(GetAntiSpamString(spam))
                        .WithIsInline(true));

                if (raid != null)
                    embed.AddField(efb => efb.WithName("Anti-Raid")
                        .WithValue(GetAntiRaidString(raid))
                        .WithIsInline(true));

                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }
        }
    }
}