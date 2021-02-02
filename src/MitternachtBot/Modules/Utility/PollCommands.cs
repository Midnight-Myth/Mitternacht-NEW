using System;
using System.Linq;
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

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireUserPermission(GuildPermission.ManageMessages)]
			[RequireContext(ContextType.Guild)]
			public async Task PollReaction(string question, params string[] answers){
				if(answers.Length <= 26){
					var eb = new EmbedBuilder().WithOkColor().WithTitle(GetText("pollreaction_title", question));

					var emojiAnswers = answers.Select((s, i) => (emoji: IndexToRegionalIndicator(i), answer: s)).ToArray();

					eb.WithDescription(emojiAnswers.Aggregate("", (s, answer) => $"{s}\n{GetText("pollreaction_answer", answer.emoji, answer.answer)}", s => s.Trim()));

					var msg = await Context.Channel.EmbedAsync(eb).ConfigureAwait(false);

					_ = Task.Run(async () => await msg.AddReactionsAsync(emojiAnswers.Select(a => a.emoji).ToArray()).ConfigureAwait(false));
				}
			}

			private static Emoji IndexToRegionalIndicator(int i){
				return i switch {
					>= 0 and <= 26 => new Emoji(char.ConvertFromUtf32(0x1F1E6 + i)),
					_ => throw new ArgumentOutOfRangeException(nameof(i)),
				};
			}
		}
	}
}