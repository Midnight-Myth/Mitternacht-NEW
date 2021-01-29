using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Utility.Services;

namespace Mitternacht.Modules.Utility {
	public partial class Utility {
		[Group]
		public class PollCommands : MitternachtSubmodule<PollService> {
			private readonly DiscordSocketClient _client;

			public PollCommands(DiscordSocketClient client) {
				_client = client;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireUserPermission(GuildPermission.ManageMessages)]
			[RequireContext(ContextType.Guild)]
			public Task Poll([Remainder] string arg = null)
				=> InternalStartPoll(arg);

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireUserPermission(GuildPermission.ManageMessages)]
			[RequireContext(ContextType.Guild)]
			public async Task PollStats() {
				if(!Service.ActivePolls.TryGetValue(Context.Guild.Id, out var poll))
					return;
				await Context.Channel.EmbedAsync(poll.GetStats(GetText("poll_current_results")));
			}

			private async Task InternalStartPoll(string arg) {
				if(await Service.StartPoll((ITextChannel)Context.Channel, Context.Message, arg) == false)
					await ReplyErrorLocalized("poll_already_running").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireUserPermission(GuildPermission.ManageMessages)]
			[RequireContext(ContextType.Guild)]
			public async Task Pollend() {
				var channel = (ITextChannel)Context.Channel;

				Service.ActivePolls.TryRemove(channel.Guild.Id, out var poll);
				await poll.StopPoll().ConfigureAwait(false);
			}
		}


	}
}