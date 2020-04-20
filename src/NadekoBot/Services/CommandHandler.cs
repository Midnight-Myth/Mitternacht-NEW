using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Mitternacht.Common.Collections;
using Mitternacht.Common.ModuleBehaviors;
using Mitternacht.Extensions;
using Mitternacht.Services.Database.Models;
using NLog;

namespace Mitternacht.Services {
	public class GuildUserComparer : IEqualityComparer<IGuildUser> {
		public bool Equals(IGuildUser x, IGuildUser y)
			=> x?.Id == y?.Id;

		public int GetHashCode(IGuildUser obj)
			=> obj.Id.GetHashCode();
	}

	public class CommandHandler : IMService {
		public const int GlobalCommandsCooldown = 750;

		private readonly DiscordSocketClient                 _client;
		private readonly CommandService                      _commandService;
		private readonly Logger                              _log;
		private readonly MitternachtBot                      _bot;
		private readonly IBotConfigProvider                  _bcp;
		private readonly IBotCredentials                     _bc;
		private          INServiceProvider                   _services;
		public           string                              DefaultPrefix => _bcp.BotConfig.DefaultPrefix;
		private          ConcurrentDictionary<ulong, string> Prefixes      { get; }

		public event Func<SocketUserMessage, Task>                 OnValidMessage     = delegate { return Task.CompletedTask; };
		public event Func<IUserMessage, CommandInfo, Task>         CommandExecuted    = delegate { return Task.CompletedTask; };
		public event Func<CommandInfo, ITextChannel, string, Task> CommandErrored     = delegate { return Task.CompletedTask; };
		public event Func<IUserMessage, Task>                      OnMessageNoTrigger = delegate { return Task.CompletedTask; };

		//userid/msg count
		public ConcurrentDictionary<ulong, uint> UserMessagesSent     { get; } = new ConcurrentDictionary<ulong, uint>();
		public ConcurrentHashSet<ulong>          UsersOnShortCooldown { get; } = new ConcurrentHashSet<ulong>();

		private readonly Timer _clearUsersOnShortCooldown;

		public CommandHandler(DiscordSocketClient client, DbService db, IBotConfigProvider bcp, IEnumerable<GuildConfig> gcs, CommandService commandService, MitternachtBot bot, IBotCredentials bc) {
			_client         = client;
			_commandService = commandService;
			_bot            = bot;
			_db             = db;
			_bcp            = bcp;
			_bc             = bc;

			_log = LogManager.GetCurrentClassLogger();

			_clearUsersOnShortCooldown = new Timer(_ => { UsersOnShortCooldown.Clear(); }, null, GlobalCommandsCooldown, GlobalCommandsCooldown);

			Prefixes = gcs.Where(x => x.Prefix != null).ToDictionary(x => x.GuildId, x => x.Prefix).ToConcurrent();
		}

		public string GetPrefix(IGuild guild)
			=> GetPrefix(guild?.Id);

		public string GetPrefix(ulong? id)
			=> id == null || !Prefixes.TryGetValue(id.Value, out var prefix) ? DefaultPrefix : prefix;

		public string SetDefaultPrefix(string prefix) {
			if(string.IsNullOrWhiteSpace(prefix)) throw new ArgumentNullException(nameof(prefix));

			prefix = prefix.ToLowerInvariant();

			using var uow = _db.UnitOfWork;
			uow.BotConfig.GetOrCreate(set => set).DefaultPrefix = prefix;
			uow.Complete();

			_bcp.Reload();

			return prefix;
		}

		public string SetPrefix(IGuild guild, string prefix) {
			if(string.IsNullOrWhiteSpace(prefix)) throw new ArgumentNullException(nameof(prefix));
			if(guild == null) throw new ArgumentNullException(nameof(guild));

			prefix = prefix.ToLowerInvariant();

			using(var uow = _db.UnitOfWork) {
				var gc = uow.GuildConfigs.For(guild.Id, set => set);
				gc.Prefix = prefix;
				uow.Complete();
			}

			Prefixes.AddOrUpdate(guild.Id, prefix, (key, old) => prefix);

			return prefix;
		}

		public void AddServices(INServiceProvider services) {
			_services = services;
		}

		public async Task ExecuteExternal(ulong? guildId, ulong channelId, string commandText) {
			if(guildId != null) {
				var guild = _client.GetGuild(guildId.Value);
				if(guild?.GetChannel(channelId) is SocketTextChannel channel) {
					try {
						IUserMessage msg = await channel.SendMessageAsync(commandText).ConfigureAwait(false);
						msg = (IUserMessage)await channel.GetMessageAsync(msg.Id).ConfigureAwait(false);
						await TryRunCommand(guild, channel, msg).ConfigureAwait(false);
					} catch { /*ignored*/ }
				} else {
					_log.Warn("Channel for external execution not found.");
				}
			}
		}

		public void StartHandling() {
			_client.MessageReceived += async msg => { await MessageReceivedHandler(msg).ConfigureAwait(false); };
		}

		private const    float     OneThousandth = 0.001f;
		private readonly DbService _db;

		private void LogSuccessfulExecution(IMessage userMsg, IGuildChannel channel, params int[] execPoints) {
			_log.Info(GetLogMessageString(userMsg, channel, execPoints, "Executed"));
		}

		private void LogErroredExecution(string errorMessage, IMessage userMsg, IGuildChannel channel, params int[] execPoints) {
			_log.Warn(GetLogMessageString(userMsg, channel, execPoints, "Errored", $"Error: {errorMessage}"));
		}

		private string GetLogMessageString(IMessage userMsg, IGuildChannel channel, int[] execPoints, string state, params string[] additionalStrings) {
			var executionTime = string.Join("/", execPoints.Select(x => x * OneThousandth));
			var userString    = $"{userMsg.Author} [{userMsg.Author.Id}]";
			var guildString   = channel == null ? "PRIVATE" : $"{channel.Guild.Name} [{channel.Guild.Id}]";
			var channelString = channel == null ? "PRIVATE" : $"{channel.Name} [{channel.Id}]";

			var strings = new[] {$"Command {state} after {executionTime}s", $"User: {userString}", $"Server: {guildString}", $"Channel: {channelString}", $"Message: {userMsg.Content}"}.Concat(additionalStrings).ToArray();
			return string.Join("\n\t", strings);
		}

		private async Task MessageReceivedHandler(SocketMessage msg) {
			try {
				//no bots, wait until bot connected and initialized
				if(msg.Author.IsBot || !_bot.Ready.Task.IsCompleted || !(msg is SocketUserMessage usrMsg)) return;

				UserMessagesSent.AddOrUpdate(usrMsg.Author.Id, 1, (key, old) => ++old);

				var channel = msg.Channel;
				var guild   = (msg.Channel as SocketTextChannel)?.Guild;

				if(_bcp.BotConfig.DmCommandsOwnerOnly && !_bc.IsOwner(msg.Author) && guild == null) return;

				if(OnValidMessage != null) await OnValidMessage.Invoke(usrMsg);

				await TryRunCommand(guild, channel, usrMsg);
			} catch(Exception ex) {
				_log.Warn(ex, $"Error in CommandHandler. Stacktrace:\n{ex.StackTrace}");
				if(ex.InnerException != null) _log.Warn(ex.InnerException, $"Inner Exception of the error in CommandHandler. Stacktrace:\n{ex.InnerException.StackTrace}");
			}
		}

		public async Task TryRunCommand(SocketGuild guild, ISocketMessageChannel channel, IUserMessage userMsg) {
			var startTickCount = Environment.TickCount;

			//its nice to have early blockers and early blocking executors separate, but
			//i could also have one interface with priorities, and just put early blockers on
			//highest priority. :thinking:
			foreach(var svc in _services) {
				if(!(svc is IEarlyBlocker blocker) || !await blocker.TryBlockEarly(guild, userMsg).ConfigureAwait(false)) continue;
				_log.Info($"Blocked User: [{userMsg.Author}] Message: [{userMsg.Content}] Service: [{svc.GetType().Name}]");
				return;
			}

			var afterEarlyBlockerTicks = Environment.TickCount - startTickCount;

			foreach(var svc in _services) {
				if(!(svc is IEarlyBlockingExecutor exec) || !await exec.TryExecuteEarly(_client, guild, userMsg).ConfigureAwait(false)) continue;
				_log.Info("User [{0}] executed [{1}] in [{2}]", userMsg.Author, userMsg.Content, svc.GetType().Name);
				return;
			}

			var afterEarlyBlockingExecutorTicks = Environment.TickCount - startTickCount;

			var messageContent = userMsg.Content;
			foreach(var svc in _services) {
				string newContent;
				if(!(svc is IInputTransformer exec) || (newContent = await exec.TransformInput(guild, userMsg.Channel, userMsg.Author, messageContent).ConfigureAwait(false)) == messageContent.ToLowerInvariant()) continue;
				messageContent = newContent;
				break;
			}

			var prefix          = GetPrefix(guild?.Id);
			var isPrefixCommand = messageContent.StartsWith(".prefix");
			// execute the command and measure the time it took
			if(messageContent.StartsWith(prefix) || isPrefixCommand) {
				var (success, error, info) = await ExecuteCommandAsync(new CommandContext(_client, userMsg), messageContent, isPrefixCommand ? 1 : prefix.Length, _services, MultiMatchHandling.Best);
				startTickCount = Environment.TickCount - startTickCount;

				if(success) {
					LogSuccessfulExecution(userMsg, channel as ITextChannel, afterEarlyBlockerTicks, afterEarlyBlockingExecutorTicks, startTickCount);
					await CommandExecuted(userMsg, info).ConfigureAwait(false);
					return;
				}

				if(error != null) {
					LogErroredExecution(error, userMsg, channel as ITextChannel, afterEarlyBlockerTicks, afterEarlyBlockingExecutorTicks, startTickCount);
					if(guild != null) await CommandErrored(info, channel as ITextChannel, error);
				}
			} else {
				await OnMessageNoTrigger(userMsg).ConfigureAwait(false);
			}

			foreach(var svc in _services) {
				if(svc is ILateExecutor exec) {
					await exec.LateExecute(_client, guild, userMsg).ConfigureAwait(false);
				}
			}
		}

		public async Task<(bool Success, string Error, CommandInfo Info)> ExecuteCommandAsync(CommandContext context, string message, int argPos, IServiceProvider services, MultiMatchHandling multiMatchHandling = MultiMatchHandling.Exception) {
			var input        = message.Substring(argPos);
			var searchResult = _commandService.Search(context, input);
			if(!searchResult.IsSuccess) return (false, null, null);

			var commands            = searchResult.Commands;
			var preconditionResults = new Dictionary<CommandMatch, PreconditionResult>();

			foreach(var match in commands) {
				preconditionResults[match] = await match.Command.CheckPreconditionsAsync(context, services).ConfigureAwait(false);
			}

			var successfulPreconditions = preconditionResults.Where(x => x.Value.IsSuccess).ToList();

			if(!successfulPreconditions.Any()) {
				//All preconditions failed, return the one from the highest priority command
				var bestCandidate = preconditionResults.OrderByDescending(x => x.Key.Command.Priority).FirstOrDefault(x => !x.Value.IsSuccess);
				return (false, bestCandidate.Value.ErrorReason, commands[0].Command);
			}

			var parseResultsDict = new Dictionary<CommandMatch, ParseResult>();
			foreach(var (commandMatch, preconditionResult) in successfulPreconditions) {
				var parseResult = await commandMatch.ParseAsync(context, searchResult, preconditionResult, services).ConfigureAwait(false);

				if(parseResult.Error == CommandError.MultipleMatches) {
					if(multiMatchHandling == MultiMatchHandling.Best) {
						IReadOnlyList<TypeReaderValue> argList   = parseResult.ArgValues.Select(x => x.Values.OrderByDescending(y => y.Score).First()).ToImmutableArray();
						IReadOnlyList<TypeReaderValue> paramList = parseResult.ParamValues.Select(x => x.Values.OrderByDescending(y => y.Score).First()).ToImmutableArray();
						parseResult = ParseResult.FromSuccess(argList, paramList);
					}
				}

				parseResultsDict[commandMatch] = parseResult;
			}

			// Calculates the 'score' of a command given a parse result
			float CalculateScore(CommandMatch match, ParseResult parseResult) {
				float argValuesScore = 0, paramValuesScore = 0;

				if(match.Command.Parameters.Count > 0) {
					var argValuesSum   = parseResult.ArgValues?.Sum(x => x.Values.OrderByDescending(y => y.Score).FirstOrDefault().Score) ?? 0;
					var paramValuesSum = parseResult.ParamValues?.Sum(x => x.Values.OrderByDescending(y => y.Score).FirstOrDefault().Score) ?? 0;

					argValuesScore   = argValuesSum / match.Command.Parameters.Count;
					paramValuesScore = paramValuesSum / match.Command.Parameters.Count;
				}

				var totalArgsScore = (argValuesScore + paramValuesScore) / 2;
				return match.Command.Priority + totalArgsScore * 0.99f;
			}

			//Order the parse results by their score so that we choose the most likely result to execute
			var parseResults = parseResultsDict.OrderByDescending(x => CalculateScore(x.Key, x.Value)).ToList();

			var successfulParses = parseResults.Where(x => x.Value.IsSuccess).ToArray();

			if(successfulParses.Length == 0) {
				//All parses failed, return the one from the highest priority command, using score as a tie breaker
				var bestMatch = parseResults.FirstOrDefault(x => !x.Value.IsSuccess);
				return (false, bestMatch.Value.ErrorReason, commands[0].Command);
			}

			var cmd = successfulParses[0].Key.Command;

			// Bot will ignore commands which are ran more often than what specified by
			// GlobalCommandsCooldown constant (milliseconds)
			if(!UsersOnShortCooldown.Add(context.Message.Author.Id)) return (false, null, cmd);

			var commandName = cmd.Aliases.First();
			foreach(var svc in _services) {
				if(!(svc is ILateBlocker exec) || !await exec.TryBlockLate(_client, context.Message, context.Guild, context.Channel, context.User, cmd.Module.GetTopLevelModule().Name, commandName).ConfigureAwait(false)) continue;
				_log.Info("Late blocking User [{0}] Command: [{1}] in [{2}]", context.User, commandName, svc.GetType().Name);
				return (false, null, cmd);
			}

			//If we get this far, at least one parse was successful. Execute the most likely overload.
			var chosenOverload = successfulParses[0];
			var execResult     = (ExecuteResult)await chosenOverload.Key.ExecuteAsync(context, chosenOverload.Value, services).ConfigureAwait(false);

			if(execResult.Exception == null || execResult.Exception is HttpException he && he.DiscordCode == 50013) return (true, null, cmd);
			lock(_errorLogLock) {
				var now = DateTime.Now;
				File.AppendAllText($"./command_errors_{now:yyyy-MM-dd}.txt", $"[{now:HH:mm-yyyy-MM-dd}]{Environment.NewLine}{execResult.Exception}{Environment.NewLine}------{Environment.NewLine}");
				_log.Warn(execResult.Exception);
			}

			return (true, null, cmd);
		}

		public async Task<bool> WouldGetExecuted(IMessage msg) {
			try {
				if(msg.Author.IsBot || !_bot.Ready.Task.IsCompleted) return false;
				if(!(msg is SocketUserMessage userMsg)) return false;

				var guild = (msg.Channel as SocketTextChannel)?.Guild;

				foreach(var svc in _services) {
					if(!(svc is IEarlyBlocker blocker) || !await blocker.TryBlockEarly(guild, userMsg, false).ConfigureAwait(false)) continue;
					return true;
				}

				foreach(var svc in _services) {
					if(!(svc is IEarlyBlockingExecutor exec) || !await exec.TryExecuteEarly(_client, guild, userMsg, false).ConfigureAwait(false)) continue;
					return true;
				}

				var messageContent = userMsg.Content;
				foreach(var svc in _services) {
					string newContent;
					if(!(svc is IInputTransformer exec) || (newContent = await exec.TransformInput(guild, userMsg.Channel, userMsg.Author, messageContent, false).ConfigureAwait(false)) == messageContent.ToLowerInvariant()) continue;
					messageContent = newContent;
					break;
				}

				var prefix          = GetPrefix(guild?.Id);
				var isPrefixCommand = messageContent.StartsWith(".prefix");

				return messageContent.StartsWith(prefix) || isPrefixCommand;
			} catch(Exception) {
				return false;
			}
		}

		private readonly object _errorLogLock = new object();
	}
}
