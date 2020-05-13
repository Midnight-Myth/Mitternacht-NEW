using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Searches.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class StreamNotificationCommands : MitternachtSubmodule<StreamNotificationService>
        {
            private readonly DbService _db;

            public StreamNotificationCommands(DbService db)
            {
                _db = db;
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task Smashcast([Remainder] string username) =>
                await TrackStream((ITextChannel)Context.Channel, username, FollowedStream.FollowedStreamType.Smashcast)
                    .ConfigureAwait(false);

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task Twitch([Remainder] string username) =>
                await TrackStream((ITextChannel)Context.Channel, username, FollowedStream.FollowedStreamType.Twitch)
                    .ConfigureAwait(false);

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task Mixer([Remainder] string username) =>
                await TrackStream((ITextChannel)Context.Channel, username, FollowedStream.FollowedStreamType.Mixer)
                    .ConfigureAwait(false);

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task ListStreams()
            {
                IEnumerable<FollowedStream> streams;
                using (var uow = _db.UnitOfWork)
                {
                    streams = uow.GuildConfigs
                                 .For(Context.Guild.Id,
                                      set => set.Include(gc => gc.FollowedStreams))
                                 .FollowedStreams;
                }

                if (!streams.Any())
                {
                    await ReplyErrorLocalized("streams_none").ConfigureAwait(false);
                    return;
                }

                var text = string.Join("\n", await Task.WhenAll(streams.Select(async snc =>
                {
                    var ch = await Context.Guild.GetTextChannelAsync(snc.ChannelId);
                    return string.Format("{0}'s stream on {1} channel. 【{2}】",
                        Format.Code(snc.Username),
                        Format.Bold(ch?.Name ?? "deleted-channel"),
                        Format.Code(snc.Type.ToString()));
                })));

                await Context.Channel.SendConfirmAsync(GetText("streams_following", streams.Count()) + "\n\n" + text)
                    .ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task RemoveStream(FollowedStream.FollowedStreamType type, [Remainder] string username)
            {
                username = username.ToLowerInvariant().Trim();

                var fs = new FollowedStream()
                {
                    ChannelId = Context.Channel.Id,
                    Username = username,
                    Type = type
                };

                bool removed;
                using (var uow = _db.UnitOfWork)
                {
                    var config = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(gc => gc.FollowedStreams));
                    removed = config.FollowedStreams.Remove(fs);
                    if (removed)
                        await uow.CompleteAsync().ConfigureAwait(false);
                }
                if (!removed)
                {
                    await ReplyErrorLocalized("stream_no").ConfigureAwait(false);
                    return;
                }

                await ReplyConfirmLocalized("stream_removed",
                    Format.Code(username),
                    type).ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task CheckStream(FollowedStream.FollowedStreamType platform, [Remainder] string username)
            {
                var stream = username?.Trim();
                if (string.IsNullOrWhiteSpace(stream))
                    return;
                try
                {
                    var streamStatus = (await Service.GetStreamStatus(new FollowedStream
                    {
                        Username = stream,
                        Type = platform,
                    }));
                    if (streamStatus.IsLive)
                    {
                        await ReplyConfirmLocalized("streamer_online",
                                username,
                                streamStatus.Views)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        await ReplyConfirmLocalized("streamer_offline",
                            username).ConfigureAwait(false);
                    }
                }
                catch
                {
                    await ReplyErrorLocalized("no_channel_found").ConfigureAwait(false);
                }
            }

            private async Task TrackStream(ITextChannel channel, string username, FollowedStream.FollowedStreamType type)
            {
                username = username.ToLowerInvariant().Trim();
                var fs = new FollowedStream
                {
                    GuildId = channel.Guild.Id,
                    ChannelId = channel.Id,
                    Username = username,
                    Type = type,
                };

                StreamStatus status;
                try
                {
                    status = await Service.GetStreamStatus(fs).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalized("stream_not_exist").ConfigureAwait(false);
                    return;
                }

                using (var uow = _db.UnitOfWork)
                {
                    uow.GuildConfigs.For(channel.Guild.Id, set => set.Include(gc => gc.FollowedStreams))
                                    .FollowedStreams
                                    .Add(fs);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                await channel.EmbedAsync(Service.GetEmbed(fs, status, Context.Guild.Id), GetText("stream_tracked")).ConfigureAwait(false);
            }
        }
    }
}