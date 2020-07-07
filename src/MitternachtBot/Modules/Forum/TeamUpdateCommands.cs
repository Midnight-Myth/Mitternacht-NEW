using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Forum.Services;
using Mitternacht.Services.Database;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mitternacht.Modules.Forum {
	public partial class Forum {
		[Group]
		public class TeamUpdateCommands : MitternachtSubmodule<TeamUpdateService> {
			private readonly IUnitOfWork uow;

			public TeamUpdateCommands(IUnitOfWork uow) {
				this.uow = uow;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOrGuildPermission(GuildPermission.Administrator)]
			public async Task TeamUpdateMessagePrefix([Remainder]string prefix = null) {
				var gc = uow.GuildConfigs.For(Context.Guild.Id);
				var tump = gc.TeamUpdateMessagePrefix;
				if(string.IsNullOrWhiteSpace(prefix)) {
					if(string.IsNullOrWhiteSpace(tump))
						await ReplyErrorLocalized("teamupdate_prefix_already_not_set").ConfigureAwait(false);
					else {
						gc.TeamUpdateMessagePrefix = null;
						uow.GuildConfigs.Update(gc);
						await ReplyConfirmLocalized("teamupdate_prefix_removed").ConfigureAwait(false);
					}
				} else {
					if(string.Equals(tump, prefix, StringComparison.Ordinal))
						await ReplyErrorLocalized("teamupdate_prefix_already_set", tump).ConfigureAwait(false);
					else {
						gc.TeamUpdateMessagePrefix = prefix;
						uow.GuildConfigs.Update(gc);
						if(string.IsNullOrWhiteSpace(tump))
							await ReplyConfirmLocalized("teamupdate_prefix_set", prefix).ConfigureAwait(false);
						else
							await ReplyConfirmLocalized("teamupdate_prefix_changed", tump, prefix).ConfigureAwait(false);
					}
				}
				await uow.SaveChangesAsync(false).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOrGuildPermission(GuildPermission.Administrator)]
			public async Task TeamUpdateChannel(ITextChannel channel = null) {
				var gc = uow.GuildConfigs.For(Context.Guild.Id);
				var tchId = gc.TeamUpdateChannelId;
				if(channel == null) {
					if(!tchId.HasValue)
						await ReplyErrorLocalized("teamupdate_channel_already_not_set").ConfigureAwait(false);
					else {
						gc.TeamUpdateChannelId = null;
						uow.GuildConfigs.Update(gc);
						await ReplyConfirmLocalized("teamupdate_channel_removed").ConfigureAwait(false);
					}
				} else {
					if(tchId.HasValue && tchId.Value == channel.Id)
						await ReplyErrorLocalized("teamupdate_channel_already_set", channel.Mention).ConfigureAwait(false);
					else {
						gc.TeamUpdateChannelId = channel.Id;
						uow.GuildConfigs.Update(gc);

						if(tchId.HasValue) {
							await ReplyConfirmLocalized("teamupdate_channel_changed", await Context.Guild.GetChannelAsync(tchId.Value).ConfigureAwait(false) is ITextChannel tch ? tch.Mention : tchId.ToString(), channel.Mention).ConfigureAwait(false);
						} else
							await ReplyConfirmLocalized("teamupdate_channel_set", channel.Mention).ConfigureAwait(false);
					}
				}

				await uow.SaveChangesAsync(false).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOrGuildPermission(GuildPermission.Administrator)]
			public async Task TeamUpdateRank(string rank, string prefix = null) {
				var success = uow.TeamUpdateRank.AddRank(Context.Guild.Id, rank, prefix) || uow.TeamUpdateRank.UpdateMessagePrefix(Context.Guild.Id, rank, prefix);

				if(success) {
					if(prefix == null) {
						await ReplyConfirmLocalized("teamupdate_rank_receives_updates", rank).ConfigureAwait(false);
					} else {
						await ReplyConfirmLocalized("teamupdate_rank_receives_updates_with_prefix", rank, prefix).ConfigureAwait(false);
					}
				} else {
					await ReplyErrorLocalized("teamupdate_rank_already_receives_updates", rank).ConfigureAwait(false);
				}
				
				await uow.SaveChangesAsync(false).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOrGuildPermission(GuildPermission.Administrator)]
			public async Task TeamUpdateRankRemove(string rank) {
				var success = uow.TeamUpdateRank.DeleteRank(Context.Guild.Id, rank);
				
				if(success)
					await ReplyConfirmLocalized("teamupdate_rank_receives_no_updates", rank).ConfigureAwait(false);
				else
					await ReplyErrorLocalized("teamupdate_rank_already_receives_no_updates", rank).ConfigureAwait(false);
				await uow.SaveChangesAsync(false).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task TeamUpdateRanks() {
				var ranks = uow.TeamUpdateRank.GetGuildRanks(Context.Guild.Id);
				var embed = new EmbedBuilder()
						.WithOkColor()
						.WithDescription(string.Join("\n", ranks.Select(r => $"- {(r.MessagePrefix != null ? $"`{r.MessagePrefix}` " : "")}{r.Rankname}")))
						.WithTitle(GetText("teamupdate_ranks"))
						.Build();
				await ReplyAsync(embed: embed).ConfigureAwait(false);
			}
		}
	}
}
