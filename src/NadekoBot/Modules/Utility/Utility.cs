using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using NadekoBot.Extensions;
using System.Reflection;
using NadekoBot.Services.Impl;
using System.Net.Http;
using System.Collections.Concurrent;
using System.Threading;
using ImageSharp;
using System.Collections.Generic;
using Newtonsoft.Json;
using Discord.WebSocket;
using System.Diagnostics;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using Color = Discord.Color;
using NadekoBot.Services;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility : NadekoTopLevelModule
    {
        private static ConcurrentDictionary<ulong, Timer> _rotatingRoleColors = new ConcurrentDictionary<ulong, Timer>();
        private readonly DiscordSocketClient _client;
        private readonly IStatsService _stats;
        private readonly IBotCredentials _creds;
        private readonly ShardsCoordinator _shardCoord;

        public Utility(NadekoBot nadeko, DiscordSocketClient client, IStatsService stats, IBotCredentials creds)
        {
            _client = client;
            _stats = stats;
            _creds = creds;
            _shardCoord = nadeko.ShardCoord;
        }        

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [OwnerOnly]
        public async Task RotateRoleColor(int timeout, IRole role, params string[] hexes)
        {
            var channel = (ITextChannel)Context.Channel;

            if ((timeout < 60 && timeout != 0) || timeout > 3600)
                return;

            Timer t;
            if (timeout == 0 || hexes.Length == 0)
            {
                if (_rotatingRoleColors.TryRemove(role.Id, out t))
                {
                    t.Change(Timeout.Infinite, Timeout.Infinite);
                    await ReplyConfirmLocalized("rrc_stop", Format.Bold(role.Name)).ConfigureAwait(false);
                }
                return;
            }

            var hexColors = hexes.Select(hex =>
            {
                try { return (Rgba32?)Rgba32.FromHex(hex.Replace("#", "")); } catch { return null; }
            })
            .Where(c => c != null)
            .Select(c => c.Value)
            .ToArray();

            if (!hexColors.Any())
            {
                await ReplyErrorLocalized("rrc_no_colors").ConfigureAwait(false);
                return;
            }

            var images = hexColors.Select(color =>
            {
                var img = new ImageSharp.Image<Rgba32>(50, 50);
                img.BackgroundColor(color);
                return img;
            }).Merge().ToStream();

            var i = 0;
            t = new Timer(async (_) =>
            {
                try
                {
                    var color = hexColors[i];
                    await role.ModifyAsync(r => r.Color = new Color(color.R, color.G, color.B)).ConfigureAwait(false);
                    ++i;
                    if (i >= hexColors.Length)
                        i = 0;
                }
                catch { }
            }, null, 0, timeout * 1000);

            _rotatingRoleColors.AddOrUpdate(role.Id, t, (key, old) =>
            {
                old.Change(Timeout.Infinite, Timeout.Infinite);
                return t;
            });
            await channel.SendFileAsync(images, "magicalgirl.jpg", GetText("rrc_start", Format.Bold(role.Name))).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task TogetherTube()
        {
            Uri target;
            using (var http = new HttpClient())
            {
                var res = await http.GetAsync("https://togethertube.com/room/create").ConfigureAwait(false);
                target = res.RequestMessage.RequestUri;
            }

            await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                .WithAuthor(eab => eab.WithIconUrl("https://togethertube.com/assets/img/favicons/favicon-32x32.png")
                .WithName("Together Tube")
                .WithUrl("https://togethertube.com/"))
                .WithDescription(Context.User.Mention + " " + GetText("togtub_room_link") +  "\n" + target));
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task WhosPlaying([Remainder] string game)
        {
            game = game?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(game))
                return;

            var socketGuild = Context.Guild as SocketGuild;
            if (socketGuild == null)
            {
                _log.Warn("Can't cast guild to socket guild.");
                return;
            }
            var rng = new NadekoRandom();
            var arr = await Task.Run(() => socketGuild.Users
                    .Where(u => u.Game?.Name?.ToUpperInvariant() == game)
                    .Select(u => u.Username)
                    .OrderBy(x => rng.Next())
                    .Take(60)
                    .ToArray()).ConfigureAwait(false);

            int i = 0;
            if (arr.Length == 0)
                await ReplyErrorLocalized("nobody_playing_game").ConfigureAwait(false);
            else
            {
                await Context.Channel.SendConfirmAsync("```css\n" + string.Join("\n", arr.GroupBy(item => (i++) / 2)
                                                                                 .Select(ig => string.Concat(ig.Select(el => $"• {el,-27}")))) + "\n```")
                                                                                 .ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task InRole([Remainder] IRole role)
        {
            var rng = new NadekoRandom();
            var usrs = (await Context.Guild.GetUsersAsync()).ToArray();
            var roleUsers = usrs.Where(u => u.RoleIds.Contains(role.Id)).Select(u => u.ToString())
                .ToArray();
            var inroleusers = string.Join(", ", roleUsers
                    .OrderBy(x => rng.Next())
                    .Take(50));
            var embed = new EmbedBuilder().WithOkColor()
                .WithTitle("ℹ️ " + Format.Bold(GetText("inrole_list", Format.Bold(role.Name))) + $" - {roleUsers.Length}")
                .WithDescription($"```css\n[{role.Name}]\n{inroleusers}```");
            await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task CheckMyPerms()
        {

            StringBuilder builder = new StringBuilder();
            var user = (IGuildUser) Context.User;
            var perms = user.GetPermissions((ITextChannel)Context.Channel);
            foreach (var p in perms.GetType().GetProperties().Where(p => !p.GetGetMethod().GetParameters().Any()))
            {
                builder.AppendLine($"{p.Name} : {p.GetValue(perms, null)}");
            }
            await Context.Channel.SendConfirmAsync(builder.ToString());
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task UserId([Remainder] IGuildUser target = null)
        {
            var usr = target ?? Context.User;
            await ReplyConfirmLocalized("userid", "🆔", Format.Bold(usr.ToString()),
                Format.Code(usr.Id.ToString())).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task ChannelId()
        {
            await ReplyConfirmLocalized("channelid", "🆔", Format.Code(Context.Channel.Id.ToString()))
                .ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ServerId()
        {
            await ReplyConfirmLocalized("serverid", "🆔", Format.Code(Context.Guild.Id.ToString()))
                .ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Roles(IGuildUser target, int page = 1)
        {
            var channel = (ITextChannel)Context.Channel;
            var guild = channel.Guild;

            const int rolesPerPage = 20;

            if (page < 1 || page > 100)
                return;

            if (target != null)
            {
                var roles = target.GetRoles().Except(new[] { guild.EveryoneRole }).OrderBy(r => -r.Position).Skip((page - 1) * rolesPerPage).Take(rolesPerPage).ToArray();
                if (!roles.Any())
                {
                    await ReplyErrorLocalized("no_roles_on_page").ConfigureAwait(false);
                }
                else
                {
                    
                    await channel.SendConfirmAsync(GetText("roles_page", page, Format.Bold(target.ToString())), 
                        "\n• " + string.Join("\n• ", (IEnumerable<IRole>)roles).SanitizeMentions()).ConfigureAwait(false);
                }
            }
            else
            {
                var roles = guild.Roles.Except(new[] { guild.EveryoneRole }).OrderBy(r => -r.Position).Skip((page - 1) * rolesPerPage).Take(rolesPerPage).ToArray();
                if (!roles.Any())
                {
                    await ReplyErrorLocalized("no_roles_on_page").ConfigureAwait(false);
                }
                else
                {
                    await channel.SendConfirmAsync(GetText("roles_all_page", page),
                        "\n• " + string.Join("\n• ", (IEnumerable<IRole>)roles).SanitizeMentions()).ConfigureAwait(false);
                }
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task Roles(int page = 1) =>
            Roles(null, page);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ChannelTopic([Remainder]ITextChannel channel = null)
        {
            if (channel == null)
                channel = (ITextChannel)Context.Channel;

            var topic = channel.Topic;
            if (string.IsNullOrWhiteSpace(topic))
                await ReplyErrorLocalized("no_topic_set").ConfigureAwait(false);
            else
                await Context.Channel.SendConfirmAsync(GetText("channel_topic"), topic).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireBotPermission(ChannelPermission.CreateInstantInvite)]
        [RequireUserPermission(ChannelPermission.CreateInstantInvite)]
        public async Task CreateInvite()
        {
            var invite = await ((ITextChannel)Context.Channel).CreateInviteAsync(0, null, isUnique: true);

            await Context.Channel.SendConfirmAsync($"{Context.User.Mention} https://discord.gg/{invite.Code}");
        }

        [NadekoCommand, Usage, Description, Aliases]
        [Shard0Precondition]
        public async Task ShardStats(int page = 1)
        {
            if (--page < 0)
                return;
            var statuses = _shardCoord.Statuses.ToArray()
                .Where(x => x != null);

            var status = string.Join(", ", statuses
                .GroupBy(x => x.ConnectionState)
                .Select(x => $"{x.Count()} {x.Key}")
                .ToArray());

            var allShardStrings = statuses
                .Select(x =>
                {
                    var timeDiff = DateTime.UtcNow - x.Time;
                    if (timeDiff > TimeSpan.FromSeconds(20))
                        return $"Shard #{Format.Bold(x.ShardId.ToString())} **UNRESPONSIVE** for {timeDiff.ToString(@"hh\:mm\:ss")}";
                    return GetText("shard_stats_txt", x.ShardId.ToString(),
                        Format.Bold(x.ConnectionState.ToString()), Format.Bold(x.Guilds.ToString()), timeDiff.ToString(@"hh\:mm\:ss"));
                        })
                .ToArray();

            await Context.Channel.SendPaginatedConfirmAsync(_client, page, (curPage) =>
            {

                var str = string.Join("\n", allShardStrings.Skip(25 * curPage).Take(25));

                if (string.IsNullOrWhiteSpace(str))
                    str = GetText("no_shards_on_page");

                return new EmbedBuilder()
                    .WithAuthor(a => a.WithName(GetText("shard_stats")))
                    .WithTitle(status)
                    .WithOkColor()
                    .WithDescription(str);
            }, allShardStrings.Length / 25);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Stats()
        {            
            await Context.Channel.EmbedAsync(
                new EmbedBuilder().WithOkColor()
                    .WithAuthor(eab => eab.WithName($"NadekoBot v{StatsService.BotVersion}")
                                          .WithUrl("http://nadekobot.readthedocs.io/en/latest/")
                                          .WithIconUrl("https://cdn.discordapp.com/avatars/116275390695079945/b21045e778ef21c96d175400e779f0fb.jpg"))
                    .AddField(efb => efb.WithName(GetText("author")).WithValue(_stats.Author).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("botid")).WithValue(_client.CurrentUser.Id.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("shard")).WithValue($"#{_client.ShardId} / {_creds.TotalShards}").WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("commands_ran")).WithValue(_stats.CommandsRan.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("messages")).WithValue($"{_stats.MessageCounter} ({_stats.MessagesPerSecond:F2}/sec)").WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("memory")).WithValue($"{_stats.Heap} MB").WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("owner_ids")).WithValue(string.Join("\n", _creds.OwnerIds)).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("uptime")).WithValue(_stats.GetUptimeString("\n")).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("presence")).WithValue(
                        GetText("presence_txt",
                            _stats.GuildCount, _stats.TextChannels, _stats.VoiceChannels)).WithIsInline(true)));
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Showemojis([Remainder] string emojis)
        {
            var tags = Context.Message.Tags.Where(t => t.Type == TagType.Emoji).Select(t => (Emote)t.Value);

            var result = string.Join("\n", tags.Select(m => GetText("showemojis", m, m.Url)));

            if (string.IsNullOrWhiteSpace(result))
                await ReplyErrorLocalized("showemojis_none").ConfigureAwait(false);
            else
                await Context.Channel.SendMessageAsync(result).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [OwnerOnly]
        public async Task ListServers(int page = 1)
        {
            page -= 1;

            if (page < 0)
                return;

            var guilds = await Task.Run(() => _client.Guilds.OrderBy(g => g.Name).Skip((page) * 15).Take(15)).ConfigureAwait(false);

            if (!guilds.Any())
            {
                await ReplyErrorLocalized("listservers_none").ConfigureAwait(false);
                return;
            }

            await Context.Channel.EmbedAsync(guilds.Aggregate(new EmbedBuilder().WithOkColor(),
                                     (embed, g) => embed.AddField(efb => efb.WithName(g.Name)
                                                                           .WithValue(
                                             GetText("listservers", g.Id, g.Users.Count,
                                                 g.OwnerId))
                                                                           .WithIsInline(false))))
                         .ConfigureAwait(false);
        }


        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task SaveChat(int cnt)
        {
            var msgs = new List<IMessage>(cnt);
            await Context.Channel.GetMessagesAsync(cnt).ForEachAsync(dled => msgs.AddRange(dled)).ConfigureAwait(false);

            var title = $"Chatlog-{Context.Guild.Name}/#{Context.Channel.Name}-{DateTime.Now}.txt";
            var grouping = msgs.GroupBy(x => $"{x.CreatedAt.Date:dd.MM.yyyy}")
                .Select(g => new
                {
                    date = g.Key,
                    messages = g.OrderBy(x => x.CreatedAt).Select(s =>
                    {
                        var msg = $"【{s.Timestamp:HH:mm:ss}】{s.Author}:";
                        if (string.IsNullOrWhiteSpace(s.ToString()))
                        {
                            if (s.Attachments.Any())
                            {
                                msg += "FILES_UPLOADED: " + string.Join("\n", s.Attachments.Select(x => x.Url));
                            }
                            else if (s.Embeds.Any())
                            {
                                msg += "EMBEDS: " + string.Join("\n--------\n", s.Embeds.Select(x => $"Description: {x.Description}"));
                            }
                        }
                        else
                        {
                            msg += s.ToString();
                        }
                        return msg;
                    })
                });
            await Context.User.SendFileAsync(
                await JsonConvert.SerializeObject(grouping, Formatting.Indented).ToStream().ConfigureAwait(false), title, title).ConfigureAwait(false);
        }
        [NadekoCommand, Usage, Description, Aliases]
        public async Task Ping()
        {
            var sw = Stopwatch.StartNew();
            var msg = await Context.Channel.SendMessageAsync("🏓").ConfigureAwait(false);
            sw.Stop();
            msg.DeleteAfter(0);

            await Context.Channel.SendConfirmAsync($"{Format.Bold(Context.User.ToString())} 🏓 {(int)sw.Elapsed.TotalMilliseconds}ms").ConfigureAwait(false);
        }
    }
}