﻿using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Modules.Forum.Services;
using Mitternacht.Services;
using System;
using System.Threading.Tasks;

namespace Mitternacht.Modules.Forum
{
    public partial class Forum
    {
        public class TeamUpdateCommands : MitternachtSubmodule<TeamUpdateService>
        {
            private readonly DbService _db;

            public TeamUpdateCommands(DbService db)
            {
                _db = db;
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOrGuildPermission(GuildPermission.Administrator)]
            public async Task TeamUpdateMessagePrefix([Remainder]string prefix = null)
            {
                using(var uow = _db.UnitOfWork)
                {
                    var gc = uow.GuildConfigs.For(Context.Guild.Id);
                    var tump = gc.TeamUpdateMessagePrefix;
                    if (string.IsNullOrWhiteSpace(prefix))
                    {
                        if (string.IsNullOrWhiteSpace(tump))
                            await ReplyErrorLocalized("teamupdate_prefix_already_not_set").ConfigureAwait(false);
                        else
                        {
                            gc.TeamUpdateMessagePrefix = null;
                            uow.GuildConfigs.Update(gc);
                            await ReplyConfirmLocalized("teamupdate_prefix_removed").ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        if (string.Equals(tump, prefix, StringComparison.Ordinal))
                            await ReplyErrorLocalized("teamupdate_prefix_already_set", tump).ConfigureAwait(false);
                        else
                        {
                            gc.TeamUpdateMessagePrefix = prefix;
                            uow.GuildConfigs.Update(gc);
                            if(string.IsNullOrWhiteSpace(tump))
                                await ReplyConfirmLocalized("teamupdate_prefix_set", prefix).ConfigureAwait(false);
                            else
                                await ReplyConfirmLocalized("teamupdate_prefix_changed", tump, prefix).ConfigureAwait(false);
                        }
                    }
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOrGuildPermission(GuildPermission.Administrator)]
            public async Task TeamUpdateChannel(ITextChannel channel = null)
            {
                using(var uow = _db.UnitOfWork)
                {
                    var gc = uow.GuildConfigs.For(Context.Guild.Id);
                    var tchId = gc.TeamUpdateChannelId;
                    if (channel == null)
                    {
                        if(!tchId.HasValue)
                            await ReplyErrorLocalized("teamupdate_channel_already_not_set").ConfigureAwait(false);
                        else
                        {
                            gc.TeamUpdateChannelId = null;
                            uow.GuildConfigs.Update(gc);
                            await ReplyConfirmLocalized("teamupdate_channel_removed").ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        if (tchId.HasValue && tchId.Value == channel.Id)
                            await ReplyErrorLocalized("teamupdate_channel_already_set", channel.Mention).ConfigureAwait(false);
                        else
                        {
                            gc.TeamUpdateChannelId = channel.Id;
                            uow.GuildConfigs.Update(gc);

                            if (tchId.HasValue)
                            {
                                var tch = await Context.Guild.GetChannelAsync(tchId.Value).ConfigureAwait(false) as ITextChannel;
                                await ReplyConfirmLocalized("teamupdate_channel_changed", tch != null ? tch.Mention : tchId.ToString(), channel.Mention).ConfigureAwait(false);
                            }
                            else
                                await ReplyConfirmLocalized("teamupdate_channel_set", channel.Mention).ConfigureAwait(false);
                        }
                    }

                    await uow.CompleteAsync().ConfigureAwait(false);
                }
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOrGuildPermission(GuildPermission.Administrator)]
            public async Task TeamUpdateRankAdd(string rank)
            {
                using(var uow = _db.UnitOfWork)
                {
                    var success = uow.TeamUpdateRank.AddRank(Context.Guild.Id, rank);
                    if (success)
                        await ReplyConfirmLocalized("teamupdate_rank_added", rank).ConfigureAwait(false);
                    else
                        await ReplyErrorLocalized("teamupdate_rank_already_added", rank).ConfigureAwait(false);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOrGuildPermission(GuildPermission.Administrator)]
            public async Task TeamUpdateRankRemove(string rank)
            {
                using (var uow = _db.UnitOfWork)
                {
                    var success = uow.TeamUpdateRank.DeleteRank(Context.Guild.Id, rank);
                    if (success)
                        await ReplyConfirmLocalized("teamupdate_rank_removed", rank).ConfigureAwait(false);
                    else
                        await ReplyErrorLocalized("teamupdate_rank_not_existing", rank).ConfigureAwait(false);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
