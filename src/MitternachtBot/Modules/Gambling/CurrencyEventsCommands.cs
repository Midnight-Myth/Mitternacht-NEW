using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Common;
using Mitternacht.Common.Attributes;
using Mitternacht.Common.Collections;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;
using Mitternacht.Services.Impl;
using NLog;

namespace Mitternacht.Modules.Gambling {
	public partial class Gambling {
		[Group]
		public class CurrencyEventsCommands : MitternachtSubmodule {
			public enum CurrencyEvent {
				Reaction,
				SneakyGameStatus
			}
			
			private static readonly ConcurrentHashSet<ulong> SneakyGameAwardedUsers = new ConcurrentHashSet<ulong>();

			private static readonly char[] SneakyGameStatusChars = Enumerable.Range(48, 10)
				.Concat(Enumerable.Range(65, 26))
				.Concat(Enumerable.Range(97, 26))
				.Select(x => (char)x)
				.ToArray();

			private string _secretCode = string.Empty;
			private readonly DiscordSocketClient _client;
			private readonly IBotConfigProvider _bc;
			private readonly CurrencyService _cs;

			public CurrencyEventsCommands(DiscordSocketClient client, IBotConfigProvider bc, CurrencyService cs) {
				_client = client;
				_bc = bc;
				_cs = cs;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOnly]
			public Task StartEvent(CurrencyEvent e, int arg = -1) {
				_ = Task.Run(async () => {
					switch(e) {
						case CurrencyEvent.Reaction:
							await ReactionEvent(arg).ConfigureAwait(false);
							break;
						case CurrencyEvent.SneakyGameStatus:
							await SneakyGameStatusEvent(arg).ConfigureAwait(false);
							break;
					}
				});
				return Task.CompletedTask;
			}

			public async Task SneakyGameStatusEvent(int? arg) {
				var num = arg == null || arg < 5 ? 60 : arg.Value;

				if(_secretCode != string.Empty)
					return;
				var rng = new NadekoRandom();

				for(var i = 0; i < 5; i++) {
					_secretCode += SneakyGameStatusChars[rng.Next(0, SneakyGameStatusChars.Length)];
				}

				await _client.SetGameAsync($"{$"type {_secretCode} for "}{_bc.BotConfig.CurrencyPluralName}").ConfigureAwait(false);
				try {
					var title = GetText("sneakygamestatus_title");
					var desc = GetText("sneakygamestatus_desc", $"{Format.Bold(100.ToString())}{_bc.BotConfig.CurrencySign}", Format.Bold(num.ToString()));
					await Context.Channel.SendConfirmAsync(desc, title).ConfigureAwait(false);
				} catch { }


				_client.MessageReceived += SneakyGameMessageReceivedEventHandler;
				await Task.Delay(num * 1000);
				_client.MessageReceived -= SneakyGameMessageReceivedEventHandler;

				var cnt = SneakyGameAwardedUsers.Count;
				SneakyGameAwardedUsers.Clear();
				_secretCode = string.Empty;

				await _client.SetGameAsync(GetText("sneakygamestatus_end", cnt)).ConfigureAwait(false);
			}

			private Task SneakyGameMessageReceivedEventHandler(SocketMessage arg) {
				if(arg.Author is IGuildUser guildUser && arg.Content == _secretCode && SneakyGameAwardedUsers.Add(arg.Author.Id)) {
					var _ = Task.Run(async () => {
						await _cs.AddAsync(guildUser, "Sneaky Game Event", 100).ConfigureAwait(false);
						
						try {
							await arg.DeleteAsync(new RequestOptions { RetryMode = RetryMode.AlwaysFail }).ConfigureAwait(false);
						} catch { }
					});
				}

				return Task.CompletedTask;
			}

			public async Task ReactionEvent(int amount) {
				if(amount <= 0)
					amount = 100;

				var title = GetText("reaction_title");
				var desc = GetText("reaction_desc", _bc.BotConfig.CurrencySign, $"{Format.Bold(amount.ToString())}{_bc.BotConfig.CurrencySign}");
				var footer = GetText("reaction_footer", 24);
				var msg = await Context.Channel.SendConfirmAsync(desc, title, footer: footer).ConfigureAwait(false);

				await new ReactionEvent(_bc.BotConfig, _client, _cs, Context.Guild, msg, amount).Start();
			}
		}
	}

	public class ReactionEvent {
		private readonly ConcurrentHashSet<ulong> _reactionAwardedUsers = new ConcurrentHashSet<ulong>();
		private readonly BotConfig _bc;
		private readonly Logger _log;
		private readonly DiscordSocketClient _client;

		private readonly IGuild _guild;
		private readonly IUserMessage _reactionMessage;

		private readonly CancellationTokenSource _tokenSource;
		private readonly CancellationToken _cancelToken;

		private readonly ConcurrentQueue<ulong> _toGiveTo = new ConcurrentQueue<ulong>();

		public ReactionEvent(BotConfig bc, DiscordSocketClient client, CurrencyService cs, IGuild guild, IUserMessage reactionMessage, int amount) {
			_bc = bc;
			_log = LogManager.GetCurrentClassLogger();
			_client = client;
			_guild = guild;
			_reactionMessage = reactionMessage;
			_tokenSource = new CancellationTokenSource();
			_cancelToken = _tokenSource.Token;

			var _ = Task.Run(async () => {
				var users = new List<ulong>();
				while (!_cancelToken.IsCancellationRequested) {
					await Task.Delay(1000).ConfigureAwait(false);
					while (_toGiveTo.TryDequeue(out var usrId)) {
						users.Add(usrId);
					}

					if (users.Count > 0) {
						await cs.AddToManyAsync(_guild.Id, "", amount, users.ToArray()).ConfigureAwait(false);
					}

					users.Clear();
				}
			}, _cancelToken);
		}

		private async Task End() {
			if(_reactionMessage != null)
				await _reactionMessage.DeleteAsync().ConfigureAwait(false);

			if(!_tokenSource.IsCancellationRequested)
				_tokenSource.Cancel();

			_client.MessageDeleted -= MessageDeletedEventHandler;
		}

		private Task MessageDeletedEventHandler(Cacheable<IMessage, ulong> msg, ISocketMessageChannel channel) {
			if(_reactionMessage?.Id != msg.Id)
				return Task.CompletedTask;
			_log.Warn("Stopping reaction event because message is deleted.");
			var __ = Task.Run(End, _cancelToken);

			return Task.CompletedTask;
		}

		public async Task Start() {
			_client.MessageDeleted += MessageDeletedEventHandler;

			var iemote = Emote.TryParse(_bc.CurrencySign, out var emote) ? emote : new Emoji(_bc.CurrencySign) as IEmote;

			try {
				await _reactionMessage.AddReactionAsync(iemote).ConfigureAwait(false);
			} catch {
				try {
					await _reactionMessage.AddReactionAsync(iemote = new Emoji("🌸")).ConfigureAwait(false);
				} catch {
					try {
						await _reactionMessage.DeleteAsync().ConfigureAwait(false);
					} catch {
						return;
					}
				}
			}

			using(_reactionMessage.OnReaction(_client, r => {
				try {
					if(r.UserId == _client.CurrentUser.Id)
						return;

					if(string.Equals(r.Emote.Name, iemote.Name, StringComparison.Ordinal) && r.User.IsSpecified && (DateTime.UtcNow - r.User.Value.CreatedAt).TotalDays > 5 && _reactionAwardedUsers.Add(r.User.Value.Id)) {
						_toGiveTo.Enqueue(r.UserId);
					}
				} catch { }
			})) {
				try {
					await Task.Delay(TimeSpan.FromHours(24), _cancelToken).ConfigureAwait(false);
				} catch(OperationCanceledException) {

				}
				if(_cancelToken.IsCancellationRequested)
					return;

				_log.Warn("Stopping reaction event because it expired.");
				await End();
			}
		}
	}
}