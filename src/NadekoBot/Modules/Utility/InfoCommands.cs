using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using GommeHDnetForumAPI.DataModels.Entities;
using GommeHDnetForumAPI.DataModels.Exceptions;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Services.Impl;

namespace Mitternacht.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class InfoCommands : MitternachtSubmodule
        {
            private readonly DiscordSocketClient _client;
            private readonly IStatsService _stats;
            private readonly DbService _db;
            private readonly ForumService _fs;

            public InfoCommands(DiscordSocketClient client, IStatsService stats, DbService db, ForumService fs)
            {
                _client = client;
                _stats = stats;
                _db = db;
                _fs = fs;
            }

            [MitternachtCommand, Usage, Description, Aliases]
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
                    .AddField(fb => fb.WithName(GetText("features")).WithValue(features).WithIsInline(true));
                if(verified > 0) embed.AddField(fb => fb.WithName(GetText("verified_members")).WithValue(verified).WithIsInline(true));

                if (Uri.IsWellFormedUriString(guild.IconUrl, UriKind.Absolute)) embed.WithImageUrl(guild.IconUrl);
                if (guild.Emotes.Any())
                {
                    embed.AddField(fb => 
                        fb.WithName($"{GetText("custom_emojis")}({guild.Emotes.Count})")
                            .WithValue(string.Join(" ", guild.Emotes.Shuffle().Take(20).Select(e => $"{e.Name} {e.ToString()}"))));
                    _log.Info(string.Join(" ", guild.Emotes.Shuffle().Take(20).Select(e => $"{e.Name} {e.ToString()}")));
                }
                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task ChannelInfo(ITextChannel channel = null)
            {
                var ch = channel ?? Context.Channel as ITextChannel;
                if (ch == null) return;
                var usercount = (await ch.GetUsersAsync().FlattenAsync().ConfigureAwait(false)).Count();
                var embed = new EmbedBuilder()
                    .WithTitle(ch.Name)
                    .WithDescription(ch.Topic?.SanitizeMentions())
                    .AddField(fb => fb.WithName(GetText("id")).WithValue(ch.Id.ToString()).WithIsInline(true))
                    .AddField(fb => fb.WithName(GetText("created_at")).WithValue($"{ch.CreatedAt:dd.MM.yyyy HH:mm}").WithIsInline(true))
                    .AddField(fb => fb.WithName(GetText("users")).WithValue(usercount.ToString()).WithIsInline(true))
                    .WithOkColor();
                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task UserInfo(IGuildUser user = null)
            {
                user = user ?? Context.User as IGuildUser;
                if (user == null) return;

                var embed = new EmbedBuilder()
                    .WithOkColor()
                    .AddField(GetText("name"), $"**{user.Username}**#{user.Discriminator}", true);
                if (!string.IsNullOrWhiteSpace(user.Nickname))
                    embed.AddField(GetText("nickname"), user.Nickname, true);
                embed.AddField(GetText("id"), user.Id.ToString(), true)
                    .AddField(GetText("joined_server"), $"{user.JoinedAt?.ToString("dd.MM.yyyy HH:mm") ?? "?"}", true)
                    .AddField(GetText("joined_discord"), $"{user.CreatedAt:dd.MM.yyyy HH:mm}", true)
                    .AddField(GetText("roles_count", user.RoleIds.Count - 1), string.Join("\n", user.GetRoles().OrderByDescending(r => r.Position).Where(r => r.Id != r.Guild.EveryoneRole.Id).Take(10).Select(r => r.Name)).SanitizeMentions(), true);

                if (user.AvatarId != null) embed.WithThumbnailUrl(user.RealAvatarUrl());
                using (var uow = _db.UnitOfWork) {
                    var forumId = uow.VerifiedUsers.GetVerifiedUserForumId(Context.Guild.Id, user.Id);
                    if (forumId != null) {
                        var username = string.Empty;
                        try {
                            username = _fs.LoggedIn ? (await _fs.Forum.GetUserInfo(forumId.Value).ConfigureAwait(false))?.Username : null;
                        }
                        catch (Exception) { /*ignored*/ }
                        embed.AddField(GetText(string.IsNullOrWhiteSpace(username) ? "forum_id" : "forum_name"), $"[{(string.IsNullOrWhiteSpace(username) ? forumId.Value.ToString() : username)}](https://gommehd.net/forum/members/{forumId})", true);
                    }
                }
                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task UserInfoForum(IGuildUser user = null) {
                user = user ?? Context.User as IGuildUser;
                if (user == null) return;

                UserInfo uinfo = null;
                using (var uow = _db.UnitOfWork) {
                    var forumId = uow.VerifiedUsers.GetVerifiedUserForumId(Context.Guild.Id, user.Id);
                    if (forumId != null) {
                        try {
                            uinfo = await _fs.Forum.GetUserInfo(forumId.Value).ConfigureAwait(false);
                        }
                        catch (UserNotFoundException)
                        {
                            (await ReplyErrorLocalized("forum_user_not_existing", user.ToString()).ConfigureAwait(false)).DeleteAfter(60);
                            return;
                        }
                        catch (UserProfileAccessException)
                        {
                            (await ReplyErrorLocalized("forum_user_not_seeable", user.ToString()).ConfigureAwait(false)).DeleteAfter(60);
                            return;
                        }
                        catch (Exception) { /*ignore other exceptions*/ }
                    }
                }

                if (uinfo == null) {
                    (await ReplyErrorLocalized("forum_user_not_accessible", user.ToString()).ConfigureAwait(false)).DeleteAfter(60);
                    return;
                }

                var embed = ForumUserInfoBuilder(uinfo)
                    .WithTitle(GetText("forumuserinfo_title", user.ToString()));

                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [Priority(0)]
            [RequireContext(ContextType.Guild)]
            public async Task ForumUserInfo(string username) 
                => await PrivateForumUserInfoHandler(username: username).ConfigureAwait(false);

            [MitternachtCommand, Usage, Description, Aliases]
            [Priority(1)]
            [RequireContext(ContextType.Guild)]
            public async Task ForumUserInfo(long userId) 
                => await PrivateForumUserInfoHandler(userId: userId).ConfigureAwait(false);

            private async Task PrivateForumUserInfoHandler(string username = null, long? userId = null)
            {
                if (username == null && !userId.HasValue)
                {
                    (await ReplyErrorLocalized("dev_failed").ConfigureAwait(false)).DeleteAfter(60);
                    return;
                }

                var userText = userId != null ? userId.Value.ToString() : username;
                UserInfo uinfo = null;
                try
                {
                    uinfo = userId.HasValue
                        ? await _fs.Forum.GetUserInfo(userId.Value).ConfigureAwait(false)
                        : await _fs.Forum.GetUserInfo(username).ConfigureAwait(false);
                }
                catch (UserNotFoundException)
                {
                    (await ReplyErrorLocalized("forum_user_not_existing", userText).ConfigureAwait(false)).DeleteAfter(60);
                    return;
                }
                catch (UserProfileAccessException)
                {
                    (await ReplyErrorLocalized("forum_user_not_seeable", userText).ConfigureAwait(false)).DeleteAfter(60);
                    return;
                }
                catch (Exception) { /*ignore other exceptions*/ }

                if (uinfo == null)
                {
                    (await ReplyErrorLocalized("forum_user_not_accessible", userText).ConfigureAwait(false)).DeleteAfter(60);
                    return;
                }

                var embed = ForumUserInfoBuilder(uinfo);
                using (var uow = _db.UnitOfWork)
                {
                    var verifiedUserId = uow.VerifiedUsers.GetVerifiedUserId(Context.Guild.Id, uinfo.Id);
                    if (verifiedUserId != null)
                    {
                        var verifiedUser = await Context.Guild.GetUserAsync(verifiedUserId.Value);
                        embed.WithTitle(GetText("forumuserinfo_title", verifiedUser?.ToString() ?? verifiedUserId.ToString()));
                    }
                }
                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }


            private EmbedBuilder ForumUserInfoBuilder(UserInfo uinfo) {
                var embed = new EmbedBuilder()
                    .WithOkColor()
                    .WithThumbnailUrl(uinfo.AvatarUrl)
                    .AddField(GetText("name"), $"[{uinfo.Username}]({uinfo.UrlPath})", true)
                    .AddField(GetText("id"), uinfo.Id, true)
                    .AddField(GetText("gender"), uinfo.Gender.ToString(), true);
                if (!string.IsNullOrWhiteSpace(uinfo.Status)) embed.AddField(GetText("status"), uinfo.Status, true);
                if (uinfo.PostCount != null) embed.AddField(GetText("posts"), uinfo.PostCount.Value, true);
                if (uinfo.LikeCount != null) embed.AddField(GetText("likes"), uinfo.LikeCount.Value, true);
                if (uinfo.Trophies != null) embed.AddField(GetText("trophies"), uinfo.Trophies.Value, true);
                if (!string.IsNullOrWhiteSpace(uinfo.Location)) embed.AddField(GetText("location"), uinfo.Location, true);
                if (!string.IsNullOrWhiteSpace(uinfo.UserTitle)) embed.AddField(GetText("rank"), uinfo.UserTitle, true);
                return embed;
            }

            [MitternachtCommand, Usage, Description, Aliases]
            public async Task ForumInfo() {
                if (_fs.Forum == null) {
                    await Context.Channel.SendErrorAsync("Forum not instantiated!").ConfigureAwait(false);
                    return;
                }
                var embed = new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle("ForumInfo")
                    .WithImageUrl(_fs.Forum.SelfUser.AvatarUrl)
                    .AddField("Logged In", _fs.LoggedIn, true)
                    .AddField("Selfuser", $"[{_fs.Forum.SelfUser.Username}]({_fs.Forum.SelfUser.UrlPath})", true);
                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
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
                }, CmdHandler.UserMessagesSent.Count / activityPerPage, hasPerms: gp => gp.Administrator);
            }
        }
    }
}