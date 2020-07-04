using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Services;

namespace Mitternacht.Modules.Level {
	public partial class Level {
		[Group]
		public class MessageXpRestrictionCommands : MitternachtSubmodule {
			private readonly DbService _db;

			public MessageXpRestrictionCommands(DbService db) {
				_db = db;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOnly]
			public async Task MsgXpRestrictionAdd(ITextChannel channel) {
				using var uow = _db.UnitOfWork;
				var success = uow.MessageXpBlacklist.CreateRestriction(channel);
				await uow.SaveChangesAsync().ConfigureAwait(false);

				if(success)
					await ConfirmLocalized("msgxpr_add_success", channel.Mention).ConfigureAwait(false);
				else
					await ErrorLocalized("msgxpr_add_fail", channel.Mention).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOnly]
			public async Task MsgXpRestrictionRemove(ITextChannel channel) {
				using var uow = _db.UnitOfWork;
				var success = uow.MessageXpBlacklist.RemoveRestriction(channel);
				await uow.SaveChangesAsync().ConfigureAwait(false);

				if(success)
					await ConfirmLocalized("msgxpr_remove_success", channel.Mention).ConfigureAwait(false);
				else
					await ErrorLocalized("msgxpr_remove_fail", channel.Mention).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task MsgXpRestrictions() {
				using var uow = _db.UnitOfWork;
				var blacklistedChannelsString = uow.MessageXpBlacklist.GetRestrictedChannelsForGuild(Context.Guild.Id).Aggregate("", (s, channelId) => $"{s}{MentionUtils.MentionChannel(channelId)}, ", s => s[0..^2]);

				if(blacklistedChannelsString.Length > 0) {
					await Context.Channel.SendConfirmAsync(blacklistedChannelsString, GetText("msgxpr_title")).ConfigureAwait(false);
				} else {
					await ErrorLocalized("msgxpr_none").ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOrGuildPermission(GuildPermission.BanMembers)]
			public async Task MsgXpRestrictionsClean() {
				using var uow = _db.UnitOfWork;
				var channelIds = uow.MessageXpBlacklist.GetRestrictedChannelsForGuild(Context.Guild.Id);
				foreach(var cid in channelIds) {
					var channel = await Context.Guild.GetChannelAsync(cid).ConfigureAwait(false);
					if(channel == null) {
						uow.MessageXpBlacklist.RemoveRestriction(Context.Guild.Id, cid);
					}
				}
				await uow.SaveChangesAsync().ConfigureAwait(false);
			}
		}
	}
}