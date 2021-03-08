using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;

namespace Mitternacht.Modules.Administration {
	public partial class Administration {
		[Group]
		public class SlowModeCommands : MitternachtSubmodule {
			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.ManageMessages)]
			public async Task Slowmode(int? messagePerSecond = null) {
				if(Context.Channel is ITextChannel textChannel) {
					if(!messagePerSecond.HasValue)
						messagePerSecond = 0;

					if(textChannel.SlowModeInterval == 0 && messagePerSecond.Value == 0) {
						await ReplyErrorLocalized("slowmode_already_disabled").ConfigureAwait(false);
						return;
					}

					try {
						await textChannel.ModifyAsync((TextChannelProperties tcp) => tcp.SlowModeInterval = messagePerSecond.Value).ConfigureAwait(false);
						await ReplyConfirmLocalized(messagePerSecond.Value == 0 ? "slowmode_disabled" : "slowmode_enabled").ConfigureAwait(false);
					} catch(ArgumentException e) {
						await Context.Channel.SendErrorAsync(e.Message, GetText("invalid_params")).ConfigureAwait(false);
					}
				}
			}
		}
	}
}