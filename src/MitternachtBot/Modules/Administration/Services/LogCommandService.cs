using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Mitternacht.Common;
using Mitternacht.Extensions;
using Mitternacht.Modules.Administration.Common;
using Mitternacht.Modules.Verification.Common;
using Mitternacht.Modules.Verification.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;
using Mitternacht.Services.Impl;
using NLog;

namespace Mitternacht.Modules.Administration.Services
{
    public class LogCommandService : IMService
    {

        private readonly DiscordSocketClient _client;
        private readonly Logger _log;

        private string PrettyCurrentTime(IGuild g)
        {
            var time = DateTime.UtcNow;
            if (g != null) time = TimeZoneInfo.ConvertTime(time, _tz.GetTimeZoneOrUtc(g.Id));
            return $"【{time:HH:mm:ss}】";
        }
        private string CurrentTime(IGuild g)
        {
            var time = DateTime.UtcNow;
            if (g != null) time = TimeZoneInfo.ConvertTime(time, _tz.GetTimeZoneOrUtc(g.Id));

            return $"{time:HH:mm:ss}";
        }

        public ConcurrentDictionary<ulong, LogSetting> GuildLogSettings { get; }

        private ConcurrentDictionary<ITextChannel, List<string>> PresenceUpdates { get; } = new ConcurrentDictionary<ITextChannel, List<string>>();
        private readonly StringService _strings;
        private readonly DbService _db;
        private readonly GuildTimezoneService _tz;
		private readonly VerificationService _vs;

        public LogCommandService(DiscordSocketClient client, StringService strings, IEnumerable<GuildConfig> gcs, DbService db, MuteService mute, ProtectionService prot, GuildTimezoneService tz, VerificationService vs)
        {
            _client = client;
            _log = LogManager.GetCurrentClassLogger();
            _strings = strings;
            _db = db;
            _tz = tz;
			_vs = vs;

            GuildLogSettings = gcs
                .ToDictionary(g => g.GuildId, g => g.LogSetting)
                .ToConcurrent();

            var timer = new Timer(async state =>
            {
                try
                {
                    var keys = PresenceUpdates.Keys.ToList();

                    await Task.WhenAll(keys.Select(key =>
                    {
                        if (!PresenceUpdates.TryRemove(key, out var msgs)) return Task.CompletedTask;
                        var title = GetText(key.Guild, "presence_updates");
                        var desc = string.Join(Environment.NewLine, msgs);
                        return key.SendConfirmAsync(title, desc.TrimTo(2048));
                    }));
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                }
            }, null, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(15));

			//_client.MessageReceived      += _client_MessageReceived;
			_client.MessageUpdated         += _client_MessageUpdated;
            _client.MessageDeleted         += _client_MessageDeleted;
            _client.UserBanned             += _client_UserBanned;
            _client.UserUnbanned           += _client_UserUnbanned;
            _client.UserJoined             += _client_UserJoined;
            _client.UserLeft               += _client_UserLeft;
            //_client.UserPresenceUpdated  += _client_UserPresenceUpdated;
            _client.UserVoiceStateUpdated  += _client_UserVoiceStateUpdated;
            _client.UserVoiceStateUpdated  += _client_UserVoiceStateUpdated_TTS;
            _client.GuildMemberUpdated     += _client_GuildUserUpdated;
			_client.UserUpdated            += _client_UserUpdated;
			_client.ChannelCreated         += _client_ChannelCreated;
            _client.ChannelDestroyed       += _client_ChannelDestroyed;
            _client.ChannelUpdated         += _client_ChannelUpdated;
			
            mute.UserMuted                 += MuteCommands_UserMuted;
            mute.UserUnmuted               += MuteCommands_UserUnmuted;
			
            prot.OnAntiProtectionTriggered += TriggeredAntiProtection;

			_vs.VerificationStep           += VerificationService_VerificationStep;
			_vs.VerificationMessage        += VerificationService_VerificationMessage;
		}

        private string GetText(IGuild guild, string key, params object[] replacements)
			=> _strings.GetText("administration", key, guild.Id, replacements);

		private Task VerificationService_VerificationStep(VerificationProcess vp, VerificationStep step) {
			var _ = Task.Run(async () => {
				try {
					var guild = vp.GuildUser.Guild;

					if(GuildLogSettings.TryGetValue(guild.Id, out var logSetting) && logSetting.VerificationSteps != null){
						var logChannel = await TryGetLogChannel(guild, logSetting, LogType.VerificationSteps);

						if(logChannel != null){
							var eb = new EmbedBuilder().WithOkColor()
								.WithTitle(GetText(guild, "log_verification_step_title"))
								.WithDescription(vp.GuildUser.ToString())
								.AddField(GetText(guild, "log_verification_step_subtitle"), step.ToString());
							await logChannel.EmbedAsync(eb);
						}
					}
				} catch (Exception e) {
					_log.Warn(e);
				}
			});
			return Task.CompletedTask;
		}

		private Task VerificationService_VerificationMessage(VerificationProcess vp, SocketMessage msg) {
			var _ = Task.Run(async () => {
				try {
					var guild = vp.GuildUser.Guild;

					if(GuildLogSettings.TryGetValue(guild.Id, out var logSetting) && logSetting.VerificationMessages != null){
						var logChannel = await TryGetLogChannel(guild, logSetting, LogType.VerificationMessages);

						if(logChannel != null){
							var eb = new EmbedBuilder().WithOkColor()
								.WithTitle(GetText(guild, "log_verification_message_title"))
								.WithDescription(vp.GuildUser.ToString())
								.AddField(GetText(guild, "log_verification_message_subtitle"), msg.Content);
							await logChannel.EmbedAsync(eb);
						}
					}
				} catch(Exception e){
					_log.Warn(e);
				}
			});
			return Task.CompletedTask;
		}

        private Task _client_UserUpdated(SocketUser before, SocketUser uAfter)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (!(uAfter is SocketGuildUser after))
                        return;

                    var g = after.Guild;

                    if (!GuildLogSettings.TryGetValue(g.Id, out var logSetting)
                        || logSetting.UserUpdatedId == null)
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(g, logSetting, LogType.UserUpdated)) == null)
                        return;

                    var embed = new EmbedBuilder();


                    if (before.Username != after.Username)
                    {
                        embed.WithTitle("👥 " + GetText(g, "username_changed"))
                            .WithDescription($"{before.Username}#{before.Discriminator} | {before.Id}")
                            .AddField(fb => fb.WithName("Old Name").WithValue($"{before.Username}").WithIsInline(true))
                            .AddField(fb => fb.WithName("New Name").WithValue($"{after.Username}").WithIsInline(true))
                            .WithFooter(fb => fb.WithText(CurrentTime(g)))
                            .WithOkColor();
                    }
                    else if (before.AvatarId != after.AvatarId)
                    {
                        embed.WithTitle("👥" + GetText(g, "avatar_changed"))
                            .WithDescription($"{before.Username}#{before.Discriminator} | {before.Id}")
                            .WithFooter(fb => fb.WithText(CurrentTime(g)))
                            .WithOkColor();

                        if (Uri.IsWellFormedUriString(before.GetAvatarUrl(), UriKind.Absolute))
                            embed.WithThumbnailUrl(before.GetAvatarUrl());
                        if (Uri.IsWellFormedUriString(after.GetAvatarUrl(), UriKind.Absolute))
                            embed.WithImageUrl(after.GetAvatarUrl());
                    }
                    else
                    {
                        return;
                    }

                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);

                    //var guildsMemberOf = _client.GetGuilds().Where(g => g.Users.Select(u => u.Id).Contains(before.Id)).ToList();
                    //foreach (var g in guildsMemberOf)
                    //{
                    //    LogSetting logSetting;
                    //    if (!GuildLogSettings.TryGetValue(g.Id, out logSetting)
                    //        || (logSetting.UserUpdatedId == null))
                    //        return;

                    //    ITextChannel logChannel;
                    //    if ((logChannel = await TryGetLogChannel(g, logSetting, LogType.UserUpdated)) == null)
                    //        return;

                    //    try { await logChannel.SendMessageAsync(str).ConfigureAwait(false); } catch { }
                    //}
                }
                catch
                {
                    // ignored
                }
            });
            return Task.CompletedTask;
        }

        private Task _client_UserVoiceStateUpdated_TTS(SocketUser iusr, SocketVoiceState before, SocketVoiceState after)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (!(iusr is IGuildUser usr))
                        return;

                    var beforeVch = before.VoiceChannel;
                    var afterVch = after.VoiceChannel;

                    if (beforeVch == afterVch)
                        return;

                    if (!GuildLogSettings.TryGetValue(usr.Guild.Id, out var logSetting)
                        || logSetting.LogVoicePresenceTTSId == null)
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(usr.Guild, logSetting, LogType.VoicePresenceTTS)) == null)
                        return;

                    var str = "";
                    if (beforeVch?.Guild == afterVch?.Guild)
                    {
                        str = GetText(logChannel.Guild, "moved", usr.Username, beforeVch?.Name, afterVch?.Name);
                    }
                    else if (beforeVch == null)
                    {
                        str = GetText(logChannel.Guild, "joined", usr.Username, afterVch.Name);
                    }
                    else if (afterVch == null)
                    {
                        str = GetText(logChannel.Guild, "left", usr.Username, beforeVch.Name);
                    }
                    var toDelete = await logChannel.SendMessageAsync(str, true).ConfigureAwait(false);
                    toDelete.DeleteAfter(5);
                }
                catch
                {
                    // ignored
                }
            });
            return Task.CompletedTask;
        }

        private void MuteCommands_UserMuted(IGuildUser usr)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (!GuildLogSettings.TryGetValue(usr.Guild.Id, out var logSetting)
                        || logSetting.UserMutedId == null)
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(usr.Guild, logSetting, LogType.UserMuted)) == null)
                        return;
                    var mutedLocalized = GetText(logChannel.Guild, "muted_sn");
					var mutes = $"🔇 {GetText(logChannel.Guild, "xmuted_text_and_voice", mutedLocalized)}";

					var embed = new EmbedBuilder().WithAuthor(eab => eab.WithName(mutes))
                            .WithTitle($"{usr.Username}#{usr.Discriminator} | {usr.Id}")
                            .WithFooter(fb => fb.WithText(CurrentTime(usr.Guild)))
                            .WithOkColor();

                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            });
        }

        private void MuteCommands_UserUnmuted(IGuildUser usr)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (!GuildLogSettings.TryGetValue(usr.Guild.Id, out var logSetting)
                        || logSetting.UserMutedId == null)
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(usr.Guild, logSetting, LogType.UserMuted)) == null)
                        return;

                    var unmutedLocalized = GetText(logChannel.Guild, "unmuted_sn");
                    var mutes = $"🔊 {GetText(logChannel.Guild, "xmuted_text_and_voice", unmutedLocalized)}";

                    var embed = new EmbedBuilder().WithAuthor(eab => eab.WithName(mutes))
                            .WithTitle($"{usr.Username}#{usr.Discriminator} | {usr.Id}")
                            .WithFooter(fb => fb.WithText($"{CurrentTime(usr.Guild)}"))
                            .WithOkColor();

                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            });
        }

        public Task TriggeredAntiProtection(PunishmentAction action, ProtectionType protection, params IGuildUser[] users)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (users.Length == 0)
                        return;

                    if (!GuildLogSettings.TryGetValue(users.First().Guild.Id, out var logSetting)
                        || logSetting.LogOtherId == null)
                        return;
                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(users.First().Guild, logSetting, LogType.Other)) == null)
                        return;

                    var punishment = "";
                    switch (action)
                    {
                        case PunishmentAction.Mute:
                            punishment = "🔇 " + GetText(logChannel.Guild, "muted_pl").ToUpperInvariant();
                            break;
                        case PunishmentAction.Kick:
                            punishment = "👢 " + GetText(logChannel.Guild, "kicked_pl").ToUpperInvariant();
                            break;
                        case PunishmentAction.Softban:
                            punishment = "☣ " + GetText(logChannel.Guild, "soft_banned_pl").ToUpperInvariant();
                            break;
                        case PunishmentAction.Ban:
                            punishment = "⛔️ " + GetText(logChannel.Guild, "banned_pl").ToUpperInvariant();
                            break;
                    }

                    var embed = new EmbedBuilder().WithAuthor(eab => eab.WithName($"🛡 Anti-{protection}"))
                            .WithTitle(GetText(logChannel.Guild, "users") + " " + punishment)
                            .WithDescription(string.Join("\n", users.Select(u => u.ToString())))
                            .WithFooter(fb => fb.WithText(CurrentTime(logChannel.Guild)))
                            .WithOkColor();

                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            });
            return Task.CompletedTask;
        }

        private Task _client_GuildUserUpdated(SocketGuildUser before, SocketGuildUser after)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (!GuildLogSettings.TryGetValue(before.Guild.Id, out var logSetting))
                        return;

                    ITextChannel logChannel;
                    if (logSetting.UserUpdatedId != null && (logChannel = await TryGetLogChannel(before.Guild, logSetting, LogType.UserUpdated)) != null)
                    {
                        var embed = new EmbedBuilder()
                            .WithOkColor()
                            .WithFooter(efb => efb.WithText(CurrentTime(before.Guild)))
                            .WithTitle($"{before.Username}#{before.Discriminator} | {before.Id}");
                        var channel = logChannel;
                        if (before.Nickname != after.Nickname)
                        {
                            
                            embed.WithAuthor(eab => eab.WithName("👥 " + GetText(channel.Guild, "nick_change")))
                                .AddField(efb => efb.WithName(GetText(channel.Guild, "old_nick")).WithValue($"{before.Nickname}#{before.Discriminator}"))
                                .AddField(efb => efb.WithName(GetText(channel.Guild, "new_nick")).WithValue($"{after.Nickname}#{after.Discriminator}"));

                            await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                        }
                        else if (!before.Roles.SequenceEqual(after.Roles))
                        {
                            if (before.Roles.Count < after.Roles.Count)
                            {
                                var diffRoles = after.Roles.Where(r => !before.Roles.Contains(r)).Select(r => r.Name);
                                embed.WithAuthor(eab => eab.WithName("⚔ " + GetText(channel.Guild, "user_role_add")))
                                    .WithDescription(string.Join(", ", diffRoles).SanitizeMentions());
                            }
                            else if (before.Roles.Count > after.Roles.Count)
                            {
                                var diffRoles = before.Roles.Where(r => !after.Roles.Contains(r)).Select(r => r.Name);
                                embed.WithAuthor(eab => eab.WithName("⚔ " + GetText(channel.Guild, "user_role_rem")))
                                    .WithDescription(string.Join(", ", diffRoles).SanitizeMentions());
                            }
                            await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                        }
                    }

                    if (logSetting.LogUserPresenceId != null && (logChannel = await TryGetLogChannel(before.Guild, logSetting, LogType.UserPresence)) != null)
                    {
                        if (before.Status != after.Status)
                        {
                            var str = "🎭" + Format.Code(PrettyCurrentTime(after.Guild)) +
                                  GetText(logChannel.Guild, "user_status_change",
                                        "👤" + Format.Bold(after.Username),
                                        Format.Bold(after.Status.ToString()));
                            PresenceUpdates.AddOrUpdate(logChannel,
                                new List<string> { str }, (id, list) => { list.Add(str); return list; });
                        }
                        else if (before.Activity?.Name != after.Activity?.Name)
                        {
                            var str = $"👾`{PrettyCurrentTime(after.Guild)}`👤__**{after.Username}**__ is now playing **{after.Activity?.Name ?? "-"}**.";
                            PresenceUpdates.AddOrUpdate(logChannel,
                                new List<string> { str }, (id, list) => { list.Add(str); return list; });
                        }
                    }
                }
                catch
                {
                    // ignored
                }
            });
            return Task.CompletedTask;
        }

        private Task _client_ChannelUpdated(IChannel cbefore, IChannel cafter)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (!(cbefore is IGuildChannel before))
                        return;
                    var after = (IGuildChannel)cafter;

                    if (!GuildLogSettings.TryGetValue(before.Guild.Id, out var logSetting)
                        || logSetting.ChannelUpdatedId == null
                        || logSetting.IgnoredChannels.Any(ilc => ilc.ChannelId == after.Id))
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(before.Guild, logSetting, LogType.ChannelUpdated)) == null)
                        return;

                    var embed = new EmbedBuilder().WithOkColor().WithFooter(efb => efb.WithText(CurrentTime(before.Guild)));

                    var beforeTextChannel = cbefore as ITextChannel;
                    var afterTextChannel = cafter as ITextChannel;

                    if (before.Name != after.Name)
                    {
                        embed.WithTitle("ℹ️ " + GetText(logChannel.Guild, "ch_name_change"))
                            .WithDescription($"{after} | {after.Id}")
                            .AddField(efb => efb.WithName(GetText(logChannel.Guild, "ch_old_name")).WithValue(before.Name));
                    }
                    else if (beforeTextChannel?.Topic != afterTextChannel?.Topic)
                    {
                        embed.WithTitle("ℹ️ " + GetText(logChannel.Guild, "ch_topic_change"))
                            .WithDescription($"{after} | {after.Id}")
                            .AddField(efb => efb.WithName(GetText(logChannel.Guild, "old_topic")).WithValue(beforeTextChannel?.Topic ?? "-"))
                            .AddField(efb => efb.WithName(GetText(logChannel.Guild, "new_topic")).WithValue(afterTextChannel?.Topic ?? "-"));
                    }
                    else
                        return;

                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            });
            return Task.CompletedTask;
        }

        private Task _client_ChannelDestroyed(IChannel ich)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (!(ich is IGuildChannel ch))
                        return;

                    if (!GuildLogSettings.TryGetValue(ch.Guild.Id, out var logSetting)
                        || logSetting.ChannelDestroyedId == null
                        || logSetting.IgnoredChannels.Any(ilc => ilc.ChannelId == ch.Id))
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(ch.Guild, logSetting, LogType.ChannelDestroyed)) == null)
                        return;
                    var title = GetText(logChannel.Guild, ch is IVoiceChannel ? "voice_chan_destroyed" : "text_chan_destroyed");
                    await logChannel.EmbedAsync(new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle("🆕 " + title)
                        .WithDescription($"{ch.Name} | {ch.Id}")
                        .WithFooter(efb => efb.WithText(CurrentTime(ch.Guild)))).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            });
            return Task.CompletedTask;
        }

        private Task _client_ChannelCreated(IChannel ich)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (!(ich is IGuildChannel ch))
                        return;

                    if (!GuildLogSettings.TryGetValue(ch.Guild.Id, out var logSetting)
                        || logSetting.ChannelCreatedId == null)
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(ch.Guild, logSetting, LogType.ChannelCreated)) == null)
                        return;
                    var title = GetText(logChannel.Guild, ch is IVoiceChannel ? "voice_chan_created" : "text_chan_created");
                    await logChannel.EmbedAsync(new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle("🆕 " + title)
                        .WithDescription($"{ch.Name} | {ch.Id}")
                        .WithFooter(efb => efb.WithText(CurrentTime(ch.Guild)))).ConfigureAwait(false);
                }
                catch (Exception ex) { _log.Warn(ex); }
            });
            return Task.CompletedTask;
        }

        private Task _client_UserVoiceStateUpdated(SocketUser iusr, SocketVoiceState before, SocketVoiceState after)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (!(iusr is IGuildUser usr))
                        return;

                    var beforeVch = before.VoiceChannel;
                    var afterVch = after.VoiceChannel;

                    if (beforeVch == afterVch)
                        return;

                    if (!GuildLogSettings.TryGetValue(usr.Guild.Id, out var logSetting)
                        || logSetting.LogVoicePresenceId == null)
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(usr.Guild, logSetting, LogType.VoicePresence)) == null)
                        return;

                    string str = null;
                    if (beforeVch?.Guild == afterVch?.Guild)
                    {
                        str = "🎙" + Format.Code(PrettyCurrentTime(usr.Guild)) + GetText(logChannel.Guild, "user_vmoved",
                                "👤" + Format.Bold(usr.Username + "#" + usr.Discriminator),
                                Format.Bold(beforeVch?.Name ?? ""), Format.Bold(afterVch?.Name ?? ""));
                    }
                    else if (beforeVch == null)
                    {
                        str = "🎙" + Format.Code(PrettyCurrentTime(usr.Guild)) + GetText(logChannel.Guild, "user_vjoined",
                                "👤" + Format.Bold(usr.Username + "#" + usr.Discriminator),
                                Format.Bold(afterVch.Name ?? ""));
                    }
                    else if (afterVch == null)
                    {
                        str = "🎙" + Format.Code(PrettyCurrentTime(usr.Guild)) + GetText(logChannel.Guild, "user_vleft",
                                "👤" + Format.Bold(usr.Username + "#" + usr.Discriminator),
                                Format.Bold(beforeVch.Name ?? ""));
                    }
                    if (!string.IsNullOrWhiteSpace(str))
                        PresenceUpdates.AddOrUpdate(logChannel, new List<string>() { str }, (id, list) => { list.Add(str); return list; });
                }
                catch
                {
                    // ignored
                }
            });
            return Task.CompletedTask;
        }

        //private Task _client_UserPresenceUpdated(Optional<SocketGuild> optGuild, SocketUser usr, SocketPresence before, SocketPresence after)
        //{
        //    var _ = Task.Run(async () =>
        //    {
        //        try
        //        {
        //            var guild = optGuild.GetValueOrDefault() ?? (usr as SocketGuildUser)?.Guild;

        //            if (guild == null)
        //                return;

        //            if (!GuildLogSettings.TryGetValue(guild.Id, out LogSetting logSetting)
        //                || (logSetting.LogUserPresenceId == null)
        //                || before.Status == after.Status)
        //                return;

        //            ITextChannel logChannel;
        //            if ((logChannel = await TryGetLogChannel(guild, logSetting, LogType.UserPresence)) == null)
        //                return;
        //            string str = "";
        //            if (before.Status != after.Status)
        //                str = "🎭" + Format.Code(PrettyCurrentTime(g)) +
        //                      GetText(logChannel.Guild, "user_status_change",
        //                            "👤" + Format.Bold(usr.Username),
        //                            Format.Bold(after.Status.ToString()));

        //            //if (before.Game?.Name != after.Game?.Name)
        //            //{
        //            //    if (str != "")
        //            //        str += "\n";
        //            //    str += $"👾`{prettyCurrentTime}`👤__**{usr.Username}**__ is now playing **{after.Game?.Name}**.";
        //            //}

        //            PresenceUpdates.AddOrUpdate(logChannel, new List<string>() { str }, (id, list) => { list.Add(str); return list; });
        //        }
        //        catch
        //        {
        //            // ignored
        //        }
        //    });
        //    return Task.CompletedTask;
        //}

        private Task _client_UserLeft(IGuildUser usr)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (!GuildLogSettings.TryGetValue(usr.Guild.Id, out var logSetting)
                        || logSetting.UserLeftId == null)
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(usr.Guild, logSetting, LogType.UserLeft)) == null)
                        return;
                    var embed = new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle("❌ " + GetText(logChannel.Guild, "user_left"))
                        .WithDescription(usr.ToString())
                        .AddField(efb => efb.WithName("Id").WithValue(usr.Id.ToString()))
                        .WithFooter(efb => efb.WithText(CurrentTime(usr.Guild)));

                    if (Uri.IsWellFormedUriString(usr.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(usr.GetAvatarUrl());

                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            });
            return Task.CompletedTask;
        }

        private Task _client_UserJoined(IGuildUser usr)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (!GuildLogSettings.TryGetValue(usr.Guild.Id, out var logSetting)
                        || logSetting.UserJoinedId == null)
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(usr.Guild, logSetting, LogType.UserJoined)) == null)
                        return;

                    var embed = new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle("✅ " + GetText(logChannel.Guild, "user_joined"))
                        .WithDescription($"{usr.Mention} `{usr}`")
                        .AddField(efb => efb.WithName("Id").WithValue(usr.Id.ToString()))
                        .AddField(GetText(logChannel.Guild, "joined_server"), $"{usr.JoinedAt?.ToString("dd.MM.yyyy HH:mm") ?? "?"}", true)
                        .AddField(GetText(logChannel.Guild, "joined_discord"), $"{usr.CreatedAt:dd.MM.yyyy HH:mm}", true)
                        .WithFooter(efb => efb.WithText(CurrentTime(usr.Guild)));

                    if (Uri.IsWellFormedUriString(usr.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(usr.GetAvatarUrl());

                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch (Exception ex) { _log.Warn(ex); }
            });
            return Task.CompletedTask;
        }

        private Task _client_UserUnbanned(IUser usr, IGuild guild)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (!GuildLogSettings.TryGetValue(guild.Id, out var logSetting)
                        || logSetting.UserUnbannedId == null)
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(guild, logSetting, LogType.UserUnbanned)) == null)
                        return;
                    var embed = new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle("♻️ " + GetText(logChannel.Guild, "user_unbanned"))
                        .WithDescription(usr.ToString())
                        .AddField(efb => efb.WithName("Id").WithValue(usr.Id.ToString()))
                        .WithFooter(efb => efb.WithText(CurrentTime(guild)));

                    if (Uri.IsWellFormedUriString(usr.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(usr.GetAvatarUrl());

                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch (Exception ex) { _log.Warn(ex); }
            });
            return Task.CompletedTask;
        }

        private Task _client_UserBanned(IUser usr, IGuild guild)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (!GuildLogSettings.TryGetValue(guild.Id, out var logSetting)
                        || logSetting.UserBannedId == null)
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(guild, logSetting, LogType.UserBanned)) == null)
                        return;
                    var embed = new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle("🚫 " + GetText(logChannel.Guild, "user_banned"))
                        .WithDescription(usr.ToString())
                        .AddField(efb => efb.WithName("Id").WithValue(usr.Id.ToString()))
                        .WithFooter(efb => efb.WithText(CurrentTime(guild)));

                    var avatarUrl = usr.GetAvatarUrl();

                    if (Uri.IsWellFormedUriString(avatarUrl, UriKind.Absolute))
                        embed.WithThumbnailUrl(usr.GetAvatarUrl());

                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch (Exception ex) { _log.Warn(ex); }
            });
            return Task.CompletedTask;
        }

        private Task _client_MessageDeleted(Cacheable<IMessage, ulong> optMsg, ISocketMessageChannel ch)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (!((optMsg.HasValue ? optMsg.Value : null) is IUserMessage msg) || msg.IsAuthor(_client))
                        return;

                    if (!(ch is ITextChannel channel))
                        return;

                    if (!GuildLogSettings.TryGetValue(channel.Guild.Id, out var logSetting)
                        || logSetting.MessageDeletedId == null
                        || logSetting.IgnoredChannels.Any(ilc => ilc.ChannelId == channel.Id))
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(channel.Guild, logSetting, LogType.MessageDeleted)) == null || logChannel.Id == msg.Id)
                        return;
                    var embed = new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle("🗑 " + GetText(logChannel.Guild, "msg_del", ((ITextChannel)msg.Channel).Name))
                        .WithDescription(msg.Author.ToString())
                        .AddField(efb => efb.WithName(GetText(logChannel.Guild, "content")).WithValue(string.IsNullOrWhiteSpace(msg.Content) ? "-" : msg.Resolve(TagHandling.FullName)).WithIsInline(false))
                        .AddField(efb => efb.WithName("Id").WithValue(msg.Id.ToString()).WithIsInline(false))
                        .WithFooter(efb => efb.WithText(CurrentTime(channel.Guild)));
                    if (msg.Attachments.Any())
                        embed.AddField(efb => efb.WithName(GetText(logChannel.Guild, "attachments")).WithValue(string.Join(", ", msg.Attachments.Select(a => a.Url))).WithIsInline(false));

                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                    // ignored
                }
            });
            return Task.CompletedTask;
        }

        private Task _client_MessageUpdated(Cacheable<IMessage, ulong> optmsg, SocketMessage imsg2, ISocketMessageChannel ch)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (!(imsg2 is IUserMessage after) || after.IsAuthor(_client))
                        return;

                    if (!((optmsg.HasValue ? optmsg.Value : null) is IUserMessage before))
                        return;

                    if (!(ch is ITextChannel channel))
                        return;

                    if (before.Content == after.Content)
                        return;

                    if (!GuildLogSettings.TryGetValue(channel.Guild.Id, out var logSetting)
                        || logSetting.MessageUpdatedId == null
                        || logSetting.IgnoredChannels.Any(ilc => ilc.ChannelId == channel.Id))
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(channel.Guild, logSetting, LogType.MessageUpdated)) == null || logChannel.Id == after.Channel.Id)
                        return;

                    var embed = new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle("📝 " + GetText(logChannel.Guild, "msg_update", ((ITextChannel)after.Channel).Name))
                        .WithDescription(after.Author.ToString())
                        .AddField(efb => efb.WithName(GetText(logChannel.Guild, "old_msg")).WithValue(string.IsNullOrWhiteSpace(before.Content) ? "-" : before.Resolve(TagHandling.FullName)).WithIsInline(false))
                        .AddField(efb => efb.WithName(GetText(logChannel.Guild, "new_msg")).WithValue(string.IsNullOrWhiteSpace(after.Content) ? "-" : after.Resolve(TagHandling.FullName)).WithIsInline(false))
                        .AddField(efb => efb.WithName("Id").WithValue(after.Id.ToString()).WithIsInline(false))
                        .WithFooter(efb => efb.WithText(CurrentTime(channel.Guild)));

                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            });
            return Task.CompletedTask;
        }

        public enum LogType
        {
            Other,
            MessageUpdated,
            MessageDeleted,
            UserJoined,
            UserLeft,
            UserBanned,
            UserUnbanned,
            UserUpdated,
            ChannelCreated,
            ChannelDestroyed,
            ChannelUpdated,
            UserPresence,
            VoicePresence,
            VoicePresenceTTS,
            UserMuted,
			VerificationSteps,
			VerificationMessages
        };

        private async Task<ITextChannel> TryGetLogChannel(IGuild guild, LogSetting logSetting, LogType logChannelType)
        {
            ulong? id = null;
            switch (logChannelType)
            {
                case LogType.Other:
                    id = logSetting.LogOtherId;
                    break;
                case LogType.MessageUpdated:
                    id = logSetting.MessageUpdatedId;
                    break;
                case LogType.MessageDeleted:
                    id = logSetting.MessageDeletedId;
                    break;
                case LogType.UserJoined:
                    id = logSetting.UserJoinedId;
                    break;
                case LogType.UserLeft:
                    id = logSetting.UserLeftId;
                    break;
                case LogType.UserBanned:
                    id = logSetting.UserBannedId;
                    break;
                case LogType.UserUnbanned:
                    id = logSetting.UserUnbannedId;
                    break;
                case LogType.UserUpdated:
                    id = logSetting.UserUpdatedId;
                    break;
                case LogType.ChannelCreated:
                    id = logSetting.ChannelCreatedId;
                    break;
                case LogType.ChannelDestroyed:
                    id = logSetting.ChannelDestroyedId;
                    break;
                case LogType.ChannelUpdated:
                    id = logSetting.ChannelUpdatedId;
                    break;
                case LogType.UserPresence:
                    id = logSetting.LogUserPresenceId;
                    break;
                case LogType.VoicePresence:
                    id = logSetting.LogVoicePresenceId;
                    break;
                case LogType.VoicePresenceTTS:
                    id = logSetting.LogVoicePresenceTTSId;
                    break;
                case LogType.UserMuted:
                    id = logSetting.UserMutedId;
                    break;
				case LogType.VerificationSteps:
					id = logSetting.VerificationSteps;
					break;
				case LogType.VerificationMessages:
					id = logSetting.VerificationMessages;
					break;
            }

            if (!id.HasValue)
            {
                UnsetLogSetting(guild.Id, logChannelType);
                return null;
            }
            var channel = await guild.GetTextChannelAsync(id.Value).ConfigureAwait(false);

            if (channel != null) return channel;
            UnsetLogSetting(guild.Id, logChannelType);
            return null;
        }

        private void UnsetLogSetting(ulong guildId, LogType logChannelType)
        {
            using (var uow = _db.UnitOfWork)
            {
                var newLogSetting = uow.GuildConfigs.LogSettingsFor(guildId).LogSetting;
                switch (logChannelType)
                {
                    case LogType.Other:
                        newLogSetting.LogOtherId = null;
                        break;
                    case LogType.MessageUpdated:
                        newLogSetting.MessageUpdatedId = null;
                        break;
                    case LogType.MessageDeleted:
                        newLogSetting.MessageDeletedId = null;
                        break;
                    case LogType.UserJoined:
                        newLogSetting.UserJoinedId = null;
                        break;
                    case LogType.UserLeft:
                        newLogSetting.UserLeftId = null;
                        break;
                    case LogType.UserBanned:
                        newLogSetting.UserBannedId = null;
                        break;
                    case LogType.UserUnbanned:
                        newLogSetting.UserUnbannedId = null;
                        break;
                    case LogType.UserUpdated:
                        newLogSetting.UserUpdatedId = null;
                        break;
                    case LogType.UserMuted:
                        newLogSetting.UserMutedId = null;
                        break;
                    case LogType.ChannelCreated:
                        newLogSetting.ChannelCreatedId = null;
                        break;
                    case LogType.ChannelDestroyed:
                        newLogSetting.ChannelDestroyedId = null;
                        break;
                    case LogType.ChannelUpdated:
                        newLogSetting.ChannelUpdatedId = null;
                        break;
                    case LogType.UserPresence:
                        newLogSetting.LogUserPresenceId = null;
                        break;
                    case LogType.VoicePresence:
                        newLogSetting.LogVoicePresenceId = null;
                        break;
                    case LogType.VoicePresenceTTS:
                        newLogSetting.LogVoicePresenceTTSId = null;
                        break;
					case LogType.VerificationSteps:
						newLogSetting.VerificationSteps = null;
						break;
					case LogType.VerificationMessages:
						newLogSetting.VerificationMessages = null;
						break;
				}
                GuildLogSettings.AddOrUpdate(guildId, newLogSetting, (gid, old) => newLogSetting);
                uow.Complete();
            }
        }

		public Dictionary<LogType, ITextChannel> GetLogChannelList(IGuild guild) {
			var success = GuildLogSettings.TryGetValue(guild.Id, out var logSetting);

			if(success) {
				var logTypes = (LogType[])Enum.GetValues(typeof(LogType));
				return logTypes.ToDictionary(lt => lt, lt => TryGetLogChannel(guild, logSetting, lt).GetAwaiter().GetResult());
			} else {
				return null;
			}
		}
    }
}