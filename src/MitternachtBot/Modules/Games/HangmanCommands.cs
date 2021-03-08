using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Games.Common.Hangman;

namespace Mitternacht.Modules.Games {
	public partial class Games {
		[Group]
		public class HangmanCommands : MitternachtSubmodule {
			private readonly DiscordSocketClient _client;

			public HangmanCommands(DiscordSocketClient client) {
				_client = client;
			}

			public static ConcurrentDictionary<ulong, Hangman> HangmanGames { get; } = new ConcurrentDictionary<ulong, Hangman>();

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task Hangmanlist() {
				await Context.Channel.SendConfirmAsync(Format.Code(GetText("hangman_types", Prefix)) + "\n" + string.Join(", ", TermPool.Data.Keys));
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public Task Hangman([Remainder]TermType type = TermType.Random) {
				var _ = Task.Run(async () => await PerformHangmanGame(type));
				return Task.CompletedTask;
			}

			Task Hm_OnGameEnded(Hangman game, string winner) {
				if(winner == null) {
					var loseEmbed = new EmbedBuilder().WithTitle($"Hangman Game ({game.TermType}) - Ended")
													.WithDescription(Format.Bold("You lose."))
													.AddField(efb => efb.WithName("It was").WithValue(CultureInfo.InvariantCulture.TextInfo.ToTitleCase(game.Term.Word)))
													.WithFooter(efb => efb.WithText(string.Join(" ", game.PreviousGuesses)))
													.WithErrorColor();

					if(Uri.IsWellFormedUriString(game.Term.ImageUrl, UriKind.Absolute))
						loseEmbed.WithImageUrl(game.Term.ImageUrl);

					return Context.Channel.EmbedAsync(loseEmbed);
				}

				var winEmbed = new EmbedBuilder().WithTitle($"Hangman Game ({game.TermType}) - Ended")
												.WithDescription(Format.Bold($"{winner} Won."))
												.AddField(efb => efb.WithName("It was").WithValue(CultureInfo.InvariantCulture.TextInfo.ToTitleCase(game.Term.Word)))
												.WithFooter(efb => efb.WithText(string.Join(" ", game.PreviousGuesses)))
												.WithOkColor();

				if(Uri.IsWellFormedUriString(game.Term.ImageUrl, UriKind.Absolute))
					winEmbed.WithImageUrl(game.Term.ImageUrl);

				return Context.Channel.EmbedAsync(winEmbed);
			}

			private Task Hm_OnLetterAlreadyUsed(Hangman game, string user, char guess)
				=> Context.Channel.SendErrorAsync($"{user} Letter `{guess}` has already been used. You can guess again in 3 seconds.\n{game.ScrambledWordCode}\n{game.GetHangman()}", $"Hangman Game ({game.TermType})", footer: string.Join(" ", game.PreviousGuesses));

			private Task Hm_OnGuessSucceeded(Hangman game, string user, char guess)
				=> Context.Channel.SendConfirmAsync($"{user} guessed a letter `{guess}`!\n{game.ScrambledWordCode}\n{game.GetHangman()}", $"Hangman Game ({game.TermType})");

			private Task Hm_OnGuessFailed(Hangman game, string user, char guess)
				=> Context.Channel.SendErrorAsync($"{user} Letter `{guess}` does not exist. You can guess again in 3 seconds.\n{game.ScrambledWordCode}\n{game.GetHangman()}", $"Hangman Game ({game.TermType})", footer: string.Join(" ", game.PreviousGuesses));

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task HangmanStop() {
				if(HangmanGames.TryRemove(Context.Channel.Id, out var removed)) {
					await removed.Stop().ConfigureAwait(false);
					await ReplyConfirmLocalized("hangman_stopped").ConfigureAwait(false);
				}
			}

			private async Task PerformHangmanGame(TermType type) {
				using var hm = new Hangman(type);

				if(HangmanGames.TryAdd(Context.Channel.Id, hm)) {
					Task _client_MessageReceived(SocketMessage msg) {
						var _ = Task.Run(() => Context.Channel.Id == msg.Channel.Id ? hm.Input(msg.Author.Id, msg.Author.ToString(), msg.Content) : Task.CompletedTask);
						return Task.CompletedTask;
					}

					hm.OnGameEnded          += Hm_OnGameEnded;
					hm.OnGuessFailed        += Hm_OnGuessFailed;
					hm.OnGuessSucceeded     += Hm_OnGuessSucceeded;
					hm.OnLetterAlreadyUsed  += Hm_OnLetterAlreadyUsed;
					_client.MessageReceived += _client_MessageReceived;

					try {
						await Context.Channel.SendConfirmAsync($"{hm.ScrambledWordCode}\n{hm.GetHangman()}", $"{GetText("hangman_game_started")} ({hm.TermType})").ConfigureAwait(false);
					} catch { }

					await hm.EndedTask.ConfigureAwait(false);

					_client.MessageReceived -= _client_MessageReceived;
					HangmanGames.TryRemove(Context.Channel.Id, out var _);
				} else {
					await ReplyErrorLocalized("hangman_running").ConfigureAwait(false);
				}
			}
		}
	}
}
