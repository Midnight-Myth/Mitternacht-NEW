using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Modules.Forum.Services;
using Mitternacht.Services;
using System.Threading.Tasks;

namespace Mitternacht.Modules.Forum {
	public partial class Forum {
		[Group]
		public class ForumNotificationCommands : MitternachtSubmodule<ForumNotificationService> {
			private readonly DbService _db;

			public ForumNotificationCommands(DbService db) {
				_db = db;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOrGuildPermission(GuildPermission.Administrator)]
			public async Task ForumNotificationChannel(ITextChannel channel = null) {
				using var uow = _db.UnitOfWork;
				var gc = uow.GuildConfigs.For(Context.Guild.Id);
				var channelId = gc.ForumNotificationChannelId;
				if(channel == null) {
					if(!channelId.HasValue)
						await ReplyErrorLocalized("teamupdate_channel_already_not_set").ConfigureAwait(false);
					else {
						gc.ForumNotificationChannelId = null;
						uow.GuildConfigs.Update(gc);
						await ReplyConfirmLocalized("teamupdate_channel_removed").ConfigureAwait(false);
					}
				} else {
					if(channelId.HasValue && channelId.Value == channel.Id)
						await ReplyErrorLocalized("teamupdate_channel_already_set", channel.Mention).ConfigureAwait(false);
					else {
						gc.ForumNotificationChannelId = channel.Id;
						uow.GuildConfigs.Update(gc);

						if(channelId.HasValue) {
							await ReplyConfirmLocalized("teamupdate_channel_changed", await Context.Guild.GetChannelAsync(channelId.Value).ConfigureAwait(false) is ITextChannel tch ? tch.Mention : channelId.ToString(), channel.Mention).ConfigureAwait(false);
						} else
							await ReplyConfirmLocalized("teamupdate_channel_set", channel.Mention).ConfigureAwait(false);
					}
				}

				await uow.SaveChangesAsync().ConfigureAwait(false);
			}
		}
	}
}
