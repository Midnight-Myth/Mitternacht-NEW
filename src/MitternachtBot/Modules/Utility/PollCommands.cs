using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Utility.Services;

namespace Mitternacht.Modules.Utility {
	public partial class Utility {
		[Group]
		public class PollCommands : MitternachtSubmodule<PollService> {
			public PollCommands() {}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireUserPermission(GuildPermission.ManageMessages)]
			[RequireContext(ContextType.Guild)]
			public async Task Poll([Remainder] string qa = null) {
				if(await Service.StartPoll((ITextChannel)Context.Channel, Context.Message, qa) == false) {
					await ReplyErrorLocalized("poll_already_running").ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireUserPermission(GuildPermission.ManageMessages)]
			[RequireContext(ContextType.Guild)]
			public async Task PollStats() {
				if(Service.ActivePolls.TryGetValue(Context.Guild.Id, out var poll)){
					await Context.Channel.EmbedAsync(poll.GetStats(GetText("poll_current_results"))).ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireUserPermission(GuildPermission.ManageMessages)]
			[RequireContext(ContextType.Guild)]
			public async Task PollEnd() {
				Service.ActivePolls.TryRemove(Context.Guild.Id, out var poll);
				await poll.StopPoll().ConfigureAwait(false);
			}
		}
	}
}