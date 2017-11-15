using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Services;

namespace Mitternacht.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class InfoCommands : NadekoSubmodule
        {
            private readonly DiscordSocketClient _client;
            private readonly IStatsService _stats;
            private readonly DbService _db;

            public InfoCommands(DiscordSocketClient client, IStatsService stats, DbService db)
            {
                _client = client;
                _stats = stats;
                _db = db;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task ServerInfo(string guildName = null)
            {
                var channel = (ITextChannel)Context.Channel;
                guildName = guildName?.ToUpperInvariant();
                var guild = string.IsNullOrWhiteSpace(guildName) ? channel.Guild : _client.Guilds.FirstOrDefault(g => string.Equals(g.Name, guildName, StringComparison.InvariantCultureIgnoreCase));
                if (guild == null) return;
                var ownername = await guild.GetUserAsync(guild.OwnerId);
                var textchn = (await guild.GetTextChannelsAsync()).Count;
                var voicechn = (await guild.GetVoiceChannelsAsync()).Count;
                var users = await guild.GetUsersAsync().ConfigureAwait(false);
                var features = guild.Features.Any() ? string.Join("\n", guild.Features) : "-";
                int verified;
                using (var uow = _db.UnitOfWork) {
                    verified = uow.VerifiedUsers.GetCount(Context.Guild.Id);
                }

                var embed = new EmbedBuilder()
                    .WithOkColor()
                    .WithAuthor(eab => eab.WithName(GetText("server_info")))
                    .WithTitle(guild.Name)
                    .AddField(fb => fb.WithName(GetText("id")).WithValue(guild.Id.ToString()).WithIsInline(true))
                    .AddField(fb => fb.WithName(GetText("owner")).WithValue(ownername.ToString()).WithIsInline(true))
                    .AddField(fb => fb.WithName(GetText("members")).WithValue(users.Count.ToString()).WithIsInline(true))
                    .AddField(fb => fb.WithName(GetText("text_channels")).WithValue(textchn.ToString()).WithIsInline(true))
                    .AddField(fb => fb.WithName(GetText("voice_channels")).WithValue(voicechn.ToString()).WithIsInline(true))
                    .AddField(fb => fb.WithName(GetText("created_at")).WithValue($"{guild.CreatedAt:dd.MM.yyyy HH:mm}").WithIsInline(true))
                    .AddField(fb => fb.WithName(GetText("region")).WithValue(guild.VoiceRegionId.ToString()).WithIsInline(true))
                    .AddField(fb => fb.WithName(GetText("roles")).WithValue((guild.Roles.Count - 1).ToString()).WithIsInline(true))
                    .AddField(fb => fb.WithName(GetText("features")).WithValue(features).WithIsInline(true))
                    .AddField(fb => fb.WithName(GetText("verified_members")).WithValue(verified).WithIsInline(true));

                if (Uri.IsWellFormedUriString(guild.IconUrl, UriKind.Absolute)) embed.WithImageUrl(guild.IconUrl);
                if (guild.Emotes.Any())
                {
                    embed.AddField(fb => fb.WithName(GetText("custom_emojis") + $"({guild.Emotes.Count})").WithValue(string.Join(" ", guild.Emotes.Shuffle().Take(20).Select(e => $"{e.Name} <:{e.Name}:{e.Id}>"))));
                }
                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task ChannelInfo(ITextChannel channel = null)
            {
                var ch = channel ?? Context.Channel as ITextChannel;
                if (ch == null) return;
                var usercount = (await ch.GetUsersAsync().Flatten()).Count();
                var embed = new EmbedBuilder()
                    .WithTitle(ch.Name)
                    .WithDescription(ch.Topic?.SanitizeMentions())
                    .AddField(fb => fb.WithName(GetText("id")).WithValue(ch.Id.ToString()).WithIsInline(true))
                    .AddField(fb => fb.WithName(GetText("created_at")).WithValue($"{ch.CreatedAt:dd.MM.yyyy HH:mm}").WithIsInline(true))
                    .AddField(fb => fb.WithName(GetText("users")).WithValue(usercount.ToString()).WithIsInline(true))
                    .WithOkColor();
                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task UserInfo(IGuildUser user = null)
            {
                user = user ?? Context.User as IGuildUser;
                if (user == null) return;

                var embed = new EmbedBuilder()
                    .WithOkColor()
                    .AddInlineField(GetText("name"), $"**{user.Username}**#{user.Discriminator}");
                if (!string.IsNullOrWhiteSpace(user.Nickname))
                    embed.AddInlineField(GetText("nickname"), user.Nickname);
                embed.AddInlineField(GetText("id"), user.Id.ToString())
                    .AddInlineField(GetText("joined_server"), $"{user.JoinedAt?.ToString("dd.MM.yyyy HH:mm") ?? "?"}")
                    .AddInlineField(GetText("joined_discord"), $"{user.CreatedAt:dd.MM.yyyy HH:mm}")
                    .AddInlineField(GetText("roles", user.RoleIds.Count - 1), string.Join("\n", user.GetRoles().OrderByDescending(r => r.Position).Where(r => r.Id != r.Guild.EveryoneRole.Id).Take(10).Select(r => r.Name)).SanitizeMentions());

                if (user.AvatarId != null) embed.WithThumbnailUrl(user.RealAvatarUrl());
                using (var uow = _db.UnitOfWork) {
                    var forumId = uow.VerifiedUsers.GetVerifiedUserForumId(Context.Guild.Id, user.Id);
                    if (forumId != null) embed.AddInlineField(GetText("forumid"), $"[{forumId}](https://gommehd.net/forum/members/{forumId})");
                }
                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task Activity(int page = 1)
            {
                const int activityPerPage = 15;
                page -= 1;

                if (page < 0)
                    return;

                await Context.Channel.SendPaginatedConfirmAsync(_client, page, p => {
                    var startCount = page * activityPerPage;
                    var strng = from kvp in CmdHandler.UserMessagesSent.OrderByDescending(kvp => kvp.Value).Skip(page * activityPerPage).Take(activityPerPage)
                                select GetText("activity_line", ++startCount, Format.Bold(kvp.Key.ToString()), kvp.Value, kvp.Value / _stats.GetUptime().TotalSeconds);
                    return new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle(GetText("activity_page"))
                        .WithFooter(efb => efb.WithText(GetText("activity_users_total", CmdHandler.UserMessagesSent.Count)))
                        .WithDescription(new StringBuilder().AppendJoin('\n', strng).ToString());
                }, CmdHandler.UserMessagesSent.Count / activityPerPage);
            }
        }
    }
}