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
				await uow.CompleteAsync().ConfigureAwait(false);

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
				await uow.CompleteAsync().ConfigureAwait(false);

				if(success)
					await ConfirmLocalized("msgxpr_remove_success", channel.Mention).ConfigureAwait(false);
				else
					await ErrorLocalized("msgxpr_remove_fail", channel.Mention).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task MsgXpRestrictions() {
				using var uow = _db.UnitOfWork;
				var blacklistedChannelsString = uow.MessageXpBlacklist
						.GetAll()
						.OrderByDescending(m => m.ChannelId)
						.Aggregate("", (s, m) => $"{s}{MentionUtils.MentionChannel(m.ChannelId)}, ", s => s[0..^2]);

				if(blacklistedChannelsString.Length > 0) {
					await Context.Channel.SendConfirmAsync(GetText("msgxpr_title"), blacklistedChannelsString).ConfigureAwait(false);
				} else {
					await ErrorLocalized("msgxpr_none").ConfigureAwait(false);
				}
			}
		}
	}
}