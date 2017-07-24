﻿using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord.WebSocket;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Administration.Services;
using NadekoBot.Modules.Music.Services;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class SelfCommands : NadekoSubmodule<SelfService>
        {
            private readonly DbService _db;

            private static readonly object _locker = new object();
            private readonly DiscordSocketClient _client;
            private readonly IImagesService _images;
            private readonly MusicService _music;
            private readonly IBotConfigProvider _bc;

            public SelfCommands(DbService db, DiscordSocketClient client,
                MusicService music, IImagesService images, IBotConfigProvider bc)
            {
                _db = db;
                _client = client;
                _images = images;
                _music = music;
                _bc = bc;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task StartupCommandAdd([Remainder] string cmdText)
            {
                var guser = ((IGuildUser)Context.User);
                var cmd = new StartupCommand()
                {
                    CommandText = cmdText,
                    ChannelId = Context.Channel.Id,
                    ChannelName = Context.Channel.Name,
                    GuildId = Context.Guild?.Id,
                    GuildName = Context.Guild?.Name,
                    VoiceChannelId = guser.VoiceChannel?.Id,
                    VoiceChannelName = guser.VoiceChannel?.Name,
                };
                using (var uow = _db.UnitOfWork)
                {
                    uow.BotConfig
                       .GetOrCreate(set => set.Include(x => x.StartupCommands))
                       .StartupCommands.Add(cmd);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("scadd"))
                    .AddField(efb => efb.WithName(GetText("server"))
                        .WithValue(cmd.GuildId == null ? $"-" : $"{cmd.GuildName}/{cmd.GuildId}").WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("channel"))
                        .WithValue($"{cmd.ChannelName}/{cmd.ChannelId}").WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("command_text"))
                        .WithValue(cmdText).WithIsInline(false)));
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task StartupCommands(int page = 1)
            {
                if (page < 1)
                    return;
                page -= 1;
                IEnumerable<StartupCommand> scmds;
                using (var uow = _db.UnitOfWork)
                {
                    scmds = uow.BotConfig
                       .GetOrCreate(set => set.Include(x => x.StartupCommands))
                       .StartupCommands
                       .OrderBy(x => x.Id)
                       .ToArray();
                }
                scmds = scmds.Skip(page * 5).Take(5);
                if (!scmds.Any())
                {
                    await ReplyErrorLocalized("startcmdlist_none").ConfigureAwait(false);
                }
                else
                {
                    await Context.Channel.SendConfirmAsync("", string.Join("\n", scmds.Select(x =>
                    {
                        string str = $"```css\n[{GetText("server") + "]: " + (x.GuildId == null ? "-" : x.GuildName + " #" + x.GuildId)}";

                        str += $@"
[{GetText("channel")}]: {x.ChannelName} #{x.ChannelId}
[{GetText("command_text")}]: {x.CommandText}```";
                        return str;
                    })), footer: GetText("page", page + 1))
                         .ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task Wait(int miliseconds)
            {
                if (miliseconds <= 0)
                    return;
                Context.Message.DeleteAfter(0);
                try
                {
                    var msg = await Context.Channel.SendConfirmAsync($"⏲ {miliseconds}ms")
                   .ConfigureAwait(false);
                    msg.DeleteAfter(miliseconds / 1000);
                }
                catch { }

                await Task.Delay(miliseconds);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task StartupCommandRemove([Remainder] string cmdText)
            {
                StartupCommand cmd;
                using (var uow = _db.UnitOfWork)
                {
                    var cmds = uow.BotConfig
                       .GetOrCreate(set => set.Include(x => x.StartupCommands))
                       .StartupCommands;
                    cmd = cmds
                       .FirstOrDefault(x => x.CommandText.ToLowerInvariant() == cmdText.ToLowerInvariant());

                    if (cmd != null)
                    {
                        cmds.Remove(cmd);
                        await uow.CompleteAsync().ConfigureAwait(false);
                    }
                }

                if (cmd == null)
                    await ReplyErrorLocalized("scrm_fail").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("scrm").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task StartupCommandsClear()
            {
                using (var uow = _db.UnitOfWork)
                {
                    uow.BotConfig
                       .GetOrCreate(set => set.Include(x => x.StartupCommands))
                       .StartupCommands
                       .Clear();
                    uow.Complete();
                }

                await ReplyConfirmLocalized("startcmds_cleared").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task ForwardMessages()
            {
                using (var uow = _db.UnitOfWork)
                {
                    var config = uow.BotConfig.GetOrCreate();
                    config.ForwardMessages = !config.ForwardMessages;
                    uow.Complete();
                }
                _bc.Reload();
                
                if (_service.ForwardDMs)
                    await ReplyConfirmLocalized("fwdm_start").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("fwdm_stop").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task ForwardToAll()
            {
                using (var uow = _db.UnitOfWork)
                {
                    var config = uow.BotConfig.GetOrCreate();
                    lock (_locker)
                        config.ForwardToAllOwners = !config.ForwardToAllOwners;
                    uow.Complete();
                }
                _bc.Reload();

                if (_service.ForwardDMsToAllOwners)
                    await ReplyConfirmLocalized("fwall_start").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("fwall_stop").ConfigureAwait(false);

            }

            //todo 2 shard commands
            //[NadekoCommand, Usage, Description, Aliases]
            //[Shard0Precondition]
            //[OwnerOnly]
            //public async Task RestartShard(int shardid)
            //{
            //    if (shardid == 0 || shardid > b)
            //    {
            //        await ReplyErrorLocalized("no_shard_id").ConfigureAwait(false);
            //        return;
            //    }
            //    try
            //    {
            //        await ReplyConfirmLocalized("shard_reconnecting", Format.Bold("#" + shardid)).ConfigureAwait(false);
            //        await shard.StartAsync().ConfigureAwait(false);
            //        await ReplyConfirmLocalized("shard_reconnected", Format.Bold("#" + shardid)).ConfigureAwait(false);
            //    }
            //    catch (Exception ex)
            //    {
            //        _log.Warn(ex);
            //    }
            //}

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task Leave([Remainder] string guildStr)
            {
                guildStr = guildStr.Trim().ToUpperInvariant();
                var server = _client.Guilds.FirstOrDefault(g => g.Id.ToString() == guildStr) ??
                    _client.Guilds.FirstOrDefault(g => g.Name.Trim().ToUpperInvariant() == guildStr);

                if (server == null)
                {
                    await ReplyErrorLocalized("no_server").ConfigureAwait(false);
                    return;
                }
                if (server.OwnerId != _client.CurrentUser.Id)
                {
                    await server.LeaveAsync().ConfigureAwait(false);
                    await ReplyConfirmLocalized("left_server", Format.Bold(server.Name)).ConfigureAwait(false);
                }
                else
                {
                    await server.DeleteAsync().ConfigureAwait(false);
                    await ReplyConfirmLocalized("deleted_server", Format.Bold(server.Name)).ConfigureAwait(false);
                }
            }


            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task Die()
            {
                try
                {
                    await ReplyConfirmLocalized("shutting_down").ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
                await Task.Delay(2000).ConfigureAwait(false);
                try { await _music.DestroyAllPlayers().ConfigureAwait(false); } catch { }
                Environment.Exit(0);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task SetName([Remainder] string newName)
            {
                if (string.IsNullOrWhiteSpace(newName))
                    return;

                await _client.CurrentUser.ModifyAsync(u => u.Username = newName).ConfigureAwait(false);

                await ReplyConfirmLocalized("bot_name", Format.Bold(newName)).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireUserPermission(GuildPermission.ManageNicknames)]
            [Priority(0)]
            public async Task SetNick([Remainder] string newNick = null)
            {
                if (string.IsNullOrWhiteSpace(newNick))
                    return;
                var curUser = await Context.Guild.GetCurrentUserAsync();
                await curUser.ModifyAsync(u => u.Nickname = newNick).ConfigureAwait(false);

                await ReplyConfirmLocalized("bot_nick", Format.Bold(newNick) ?? "-").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireBotPermission(GuildPermission.ManageNicknames)]
            [RequireUserPermission(GuildPermission.ManageNicknames)]
            [Priority(1)]
            public async Task SetNick(IGuildUser gu, [Remainder] string newNick = null)
            {
                await gu.ModifyAsync(u => u.Nickname = newNick).ConfigureAwait(false);

                await ReplyConfirmLocalized("user_nick", Format.Bold(gu.ToString()), Format.Bold(newNick) ?? "-").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task SetStatus([Remainder] SettableUserStatus status)
            {
                await _client.SetStatusAsync(SettableUserStatusToUserStatus(status)).ConfigureAwait(false);

                await ReplyConfirmLocalized("bot_status", Format.Bold(status.ToString())).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task SetAvatar([Remainder] string img = null)
            {
                if (string.IsNullOrWhiteSpace(img))
                    return;

                using (var http = new HttpClient())
                {
                    using (var sr = await http.GetStreamAsync(img))
                    {
                        var imgStream = new MemoryStream();
                        await sr.CopyToAsync(imgStream);
                        imgStream.Position = 0;

                        await _client.CurrentUser.ModifyAsync(u => u.Avatar = new Image(imgStream)).ConfigureAwait(false);
                    }
                }

                await ReplyConfirmLocalized("set_avatar").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task SetGame([Remainder] string game = null)
            {
                await _client.SetGameAsync(game).ConfigureAwait(false);

                await ReplyConfirmLocalized("set_game").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task SetStream(string url, [Remainder] string name = null)
            {
                name = name ?? "";

                await _client.SetGameAsync(name, url, StreamType.Twitch).ConfigureAwait(false);

                await ReplyConfirmLocalized("set_stream").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task Send(string where, [Remainder] string msg = null)
            {
                if (string.IsNullOrWhiteSpace(msg))
                    return;

                var ids = where.Split('|');
                if (ids.Length != 2)
                    return;
                var sid = ulong.Parse(ids[0]);
                var server = _client.Guilds.FirstOrDefault(s => s.Id == sid);

                if (server == null)
                    return;

                if (ids[1].ToUpperInvariant().StartsWith("C:"))
                {
                    var cid = ulong.Parse(ids[1].Substring(2));
                    var ch = server.TextChannels.FirstOrDefault(c => c.Id == cid);
                    if (ch == null)
                    {
                        return;
                    }
                    await ch.SendMessageAsync(msg).ConfigureAwait(false);
                }
                else if (ids[1].ToUpperInvariant().StartsWith("U:"))
                {
                    var uid = ulong.Parse(ids[1].Substring(2));
                    var user = server.Users.FirstOrDefault(u => u.Id == uid);
                    if (user == null)
                    {
                        return;
                    }
                    await user.SendMessageAsync(msg).ConfigureAwait(false);
                }
                else
                {
                    await ReplyErrorLocalized("invalid_format").ConfigureAwait(false);
                    return;
                }
                await ReplyConfirmLocalized("message_sent").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task Announce([Remainder] string message)
            {
                var channels = _client.Guilds.Select(g => g.DefaultChannel).ToArray();
                if (channels == null)
                    return;
                await Task.WhenAll(channels.Where(c => c != null).Select(c => c.SendConfirmAsync(GetText("message_from_bo", Context.User.ToString()), message)))
                        .ConfigureAwait(false);

                await ReplyConfirmLocalized("message_sent").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task ReloadImages()
            {
                var sw = Stopwatch.StartNew();
                _images.Reload();
                sw.Stop();
                await ReplyConfirmLocalized("images_loaded", sw.Elapsed.TotalSeconds.ToString("F3")).ConfigureAwait(false);
            }

            private static UserStatus SettableUserStatusToUserStatus(SettableUserStatus sus)
            {
                switch (sus)
                {
                    case SettableUserStatus.Online:
                        return UserStatus.Online;
                    case SettableUserStatus.Invisible:
                        return UserStatus.Invisible;
                    case SettableUserStatus.Idle:
                        return UserStatus.AFK;
                    case SettableUserStatus.Dnd:
                        return UserStatus.DoNotDisturb;
                }

                return UserStatus.Online;
            }

            public enum SettableUserStatus
            {
                Online,
                Invisible,
                Idle,
                Dnd
            }
        }
    }
}