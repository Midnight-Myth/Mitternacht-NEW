using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Common;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Utility.Common;
using Mitternacht.Services;
using Mitternacht.Services.Impl;
using Newtonsoft.Json;

namespace Mitternacht.Modules.Utility
{
    public partial class Utility : MitternachtTopLevelModule
    {
        private readonly DiscordSocketClient _client;
        private readonly IStatsService _stats;
        private readonly IBotCredentials _creds;
        private readonly ShardsCoordinator _shardCoord;

        public Utility(MitternachtBot mitternacht, DiscordSocketClient client, IStatsService stats, IBotCredentials creds)
        {
            _client = client;
            _stats = stats;
            _creds = creds;
            _shardCoord = mitternacht.ShardCoord;
        }        

        [MitternachtCommand, Usage, Description, Aliases]
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

        [MitternachtCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task WhosPlaying([Remainder] string game)
        {
			//TODO: avoid upper case comparison.
            game = game?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(game))
                return;

			//TODO: remove cast.
            if (!(Context.Guild is SocketGuild socketGuild))
            {
                _log.Warn("Can't cast guild to socket guild.");
                return;
            }
            var rng = new NadekoRandom();
            var arr = await Task.Run(() => socketGuild.Users
                    .Where(u => u.Activity?.Name?.ToUpperInvariant() == game)
                    .Select(u => u.Username)
                    .OrderBy(x => rng.Next())
                    .Take(60)
                    .ToArray()).ConfigureAwait(false);

            var i = 0;
            if (arr.Length == 0)
                await ReplyErrorLocalized("nobody_playing_game").ConfigureAwait(false);
            else
                await Context.Channel.SendConfirmAsync("```css\n" + string.Join("\n", arr.GroupBy(item => i++ / 2).Select(ig => string.Concat(ig.Select(el => $"• {el,-27}")))) + "\n```").ConfigureAwait(false);
        }

        [MitternachtCommand, Usage, Description, Aliases]
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

        [MitternachtCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task CheckMyPerms()
        {
            var builder = new StringBuilder();
            var user = (IGuildUser) Context.User;
            var perms = user.GetPermissions((ITextChannel)Context.Channel);
            foreach (var p in perms.GetType().GetProperties().Where(p => !p.GetGetMethod().GetParameters().Any()))
            {
                builder.AppendLine($"{p.Name} : {p.GetValue(perms, null)}");
            }
            await Context.Channel.SendConfirmAsync(builder.ToString());
        }

        [MitternachtCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task UserId([Remainder] IGuildUser target = null)
        {
            var usr = target ?? Context.User;
            await ReplyConfirmLocalized("userid", "🆔", Format.Bold(usr.ToString()),
                Format.Code(usr.Id.ToString())).ConfigureAwait(false);
        }

        [MitternachtCommand, Usage, Description, Aliases]
        public async Task ChannelId()
        {
            await ReplyConfirmLocalized("channelid", "🆔", Format.Code(Context.Channel.Id.ToString()))
                .ConfigureAwait(false);
        }

        [MitternachtCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ServerId()
        {
            await ReplyConfirmLocalized("serverid", "🆔", Format.Code(Context.Guild.Id.ToString()))
                .ConfigureAwait(false);
        }

        //todo: DRY
        [MitternachtCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Roles(IGuildUser target, int page = 1)
        {
			const int rolesPerPage = 20;

            if (--page < 0) return;

            if (target != null)
            {
                var roles = target.GetRoles().Except(new[] { Context.Guild.EveryoneRole }).OrderBy(r => -r.Position).ToArray();
                if (!roles.Skip(page * rolesPerPage).Take(rolesPerPage).Any())
                    await ReplyErrorLocalized("no_roles_on_page").ConfigureAwait(false);
                else
                    await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page, currentPage => new EmbedBuilder().WithOkColor()
                        .WithTitle(GetText("roles_page", currentPage + 1, Format.Bold(target.ToString())))
                        .WithDescription("\n• " + string.Join("\n• ", roles.Skip(currentPage * rolesPerPage).Take(rolesPerPage))), (int)Math.Ceiling(roles.Length * 1d / rolesPerPage), reactUsers: new[] { Context.User as IGuildUser });
            }
            else
            {
                var roles = Context.Guild.Roles.Except(new[] { Context.Guild.EveryoneRole }).OrderBy(r => -r.Position).ToArray();
                if (!roles.Skip(page * rolesPerPage).Take(rolesPerPage).Any())
                    await ReplyErrorLocalized("no_roles_on_page").ConfigureAwait(false);
                else
                    await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page, currentPage => new EmbedBuilder().WithOkColor()
                        .WithTitle(GetText("roles_all_page", currentPage + 1))
                        .WithDescription("\n• " + string.Join("\n• ", roles.Skip(currentPage * rolesPerPage).Take(rolesPerPage))), (int)Math.Ceiling(roles.Length * 1d / rolesPerPage), reactUsers: new[] { Context.User as IGuildUser });
            }
        }

        [MitternachtCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task Roles(int page = 1) 
            => Roles(null, page);

        [MitternachtCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ChannelTopic([Remainder]ITextChannel channel = null)
        {
            if (channel == null)
                channel = (ITextChannel)Context.Channel;

            var topic = channel.Topic;
            if (string.IsNullOrWhiteSpace(topic))
                await ReplyErrorLocalized("no_topic_set").ConfigureAwait(false);
            else
                await Context.Channel.SendConfirmAsync(topic, GetText("channel_topic")).ConfigureAwait(false);
        }

        [MitternachtCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireBotPermission(ChannelPermission.CreateInstantInvite)]
        [RequireUserPermission(ChannelPermission.CreateInstantInvite)]
        public async Task CreateInvite()
        {
            var invite = await ((ITextChannel)Context.Channel).CreateInviteAsync(0, null, isUnique: true);

            await Context.Channel.SendConfirmAsync($"{Context.User.Mention} https://discord.gg/{invite.Code}");
        }

        [MitternachtCommand, Usage, Description, Aliases]
        [Shard0Precondition]
        public async Task ShardStats(int page = 1)
        {
            if (--page < 0)
                return;
            var statuses = _shardCoord.Statuses.ToArray()
                .Where(x => x != null).ToArray();

            var status = string.Join(", ", statuses
                .GroupBy(x => x.ConnectionState)
                .Select(x => $"{x.Count()} {x.Key}")
                .ToArray());

            var allShardStrings = statuses
                .Select(x =>
                {
                    var timeDiff = DateTime.UtcNow - x.Time;
                    if (timeDiff > TimeSpan.FromSeconds(20))
                        return $"Shard #{Format.Bold(x.ShardId.ToString())} **UNRESPONSIVE** for {timeDiff:hh\\:mm\\:ss}";
                    return GetText("shard_stats_txt", x.ShardId.ToString(),
                        Format.Bold(x.ConnectionState.ToString()), Format.Bold(x.Guilds.ToString()), timeDiff.ToString(@"hh\:mm\:ss"));
                })
                .ToArray();

			const int elementsPerPage = 25;

            await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page, currentPage =>
            {

                var str = string.Join("\n", allShardStrings.Skip(elementsPerPage * currentPage).Take(elementsPerPage));

                if (string.IsNullOrWhiteSpace(str))
                    str = GetText("no_shards_on_page");

                return new EmbedBuilder()
                    .WithAuthor(a => a.WithName(GetText("shard_stats")))
                    .WithTitle(status)
                    .WithOkColor()
                    .WithDescription(str);
            }, (int)Math.Ceiling(allShardStrings.Length * 1d / elementsPerPage), reactUsers: new[] { Context.User as IGuildUser });
        }

        [MitternachtCommand, Usage, Description, Aliases]
        public async Task Stats() {
            await Context.Channel.EmbedAsync(
                new EmbedBuilder().WithOkColor()
                    .WithAuthor(eab => eab.WithName($"Mitternacht v{StatsService.BotVersion}")
                        .WithUrl("http://nadekobot.readthedocs.io/en/latest/")
                        .WithIconUrl(_client.CurrentUser.GetAvatarUrl()))
                    .AddField(efb => efb.WithName(GetText("author")).WithValue(_stats.Author).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("botid")).WithValue(_client.CurrentUser.Id.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("shard")).WithValue($"#{_client.ShardId} / {_creds.TotalShards}").WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("commands_ran")).WithValue(_stats.CommandsRan.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("messages")).WithValue($"{_stats.MessageCounter} ({_stats.MessagesPerSecond:F2}/sec)").WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("memory")).WithValue($"{_stats.Heap} MB").WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("owner_ids")).WithValue(string.Join("\n", _creds.OwnerIds)).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("uptime")).WithValue(_stats.GetUptimeString("\n")).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("presence")).WithValue(
                        GetText("presence_txt", _stats.GuildCount, _stats.TextChannels, _stats.VoiceChannels)).WithIsInline(true)));
        }

        [MitternachtCommand, Usage, Description, Aliases]
        public async Task Showemojis([Remainder] string emojis)
        {
            var tags = Context.Message.Tags.Where(t => t.Type == TagType.Emoji).Select(t => (Emote)t.Value);

            var result = string.Join("\n", tags.Select(m => GetText("showemojis", m, m.Url)));

            if (string.IsNullOrWhiteSpace(result))
                await ReplyErrorLocalized("showemojis_none").ConfigureAwait(false);
            else
                await Context.Channel.SendMessageAsync(result).ConfigureAwait(false);
        }

        [MitternachtCommand, Usage, Description, Aliases]
        [OwnerOnly]
        public async Task ListServers(int page = 1)
        {
            page -= 1;

            if (page < 0)
                return;

            var guilds = (await Task.Run(() => _client.Guilds.OrderBy(g => g.Name).Skip((page) * 15).Take(15)).ConfigureAwait(false)).ToList();

            if (!guilds.Any())
            {
                await ReplyErrorLocalized("listservers_none").ConfigureAwait(false);
                return;
            }

            await Context.Channel.EmbedAsync(guilds.Aggregate(new EmbedBuilder().WithOkColor(),
                    (embed, g) => embed.AddField(efb => efb.WithName(g.Name)
                        .WithValue(GetText("listservers", g.Id, g.Users.Count, g.OwnerId)).WithIsInline(false))))
                .ConfigureAwait(false);
        }


        [MitternachtCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task SaveChat(int count, IGuildUser user = null) {
            if (count < 1) return;
            var msgs = new List<IMessage>();
            if(user == null) await Context.Channel.GetMessagesAsync(count).ForEachAsync(dled => msgs.AddRange(dled)).ConfigureAwait(false);
            else {
                IMessage last = null;
                while (msgs.Count < count) {
                    var tmpMsgs = (last is null ? await Context.Channel.GetMessagesAsync(count).FlattenAsync().ConfigureAwait(false) : await Context.Channel.GetMessagesAsync(last, Direction.Before, count).FlattenAsync().ConfigureAwait(false)).ToList();
                    msgs.AddRange(tmpMsgs.Where(m => m.Author.Id == user.Id));
                    var beforeLast = last;
                    last = tmpMsgs.OrderBy(m => m.Timestamp).FirstOrDefault();
                    if (beforeLast != null && last != null && beforeLast.Id == last.Id) break;
                }
                if (msgs.Count > count) msgs = msgs.Take(count).ToList();
            }

            var title = $"Chatlog{(user != null ? $"-{user.Username}" : "")}-{Context.Guild.Name}/#{Context.Channel.Name}-{DateTime.Now}.txt";
            var grouping = msgs.GroupBy(x => $"{x.CreatedAt.Date:dd.MM.yyyy}")
                .Select(g => new {
                    date = g.Key,
                    messages = g.OrderByDescending(x => x.CreatedAt).Select(s => new SavechatMessage(s.Author.ToString(), $"{s.Timestamp:HH:mm:ss}", s.ToString(), s.Attachments.Select(a => a.Url).ToList(), s.Embeds.Select(e => $"Description: {e.Description}").ToList()))
                });
            await Context.User.SendFileAsync(await JsonConvert.SerializeObject(grouping, Formatting.Indented).ToStream().ConfigureAwait(false), title, title).ConfigureAwait(false);
        }

        [MitternachtCommand, Usage, Description, Aliases]
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