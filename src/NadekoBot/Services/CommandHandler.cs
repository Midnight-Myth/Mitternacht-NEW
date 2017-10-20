﻿using System;
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

namespace Mitternacht.Services
{
    public class GuildUserComparer : IEqualityComparer<IGuildUser>
    {
        public bool Equals(IGuildUser x, IGuildUser y) => x.Id == y.Id;

        public int GetHashCode(IGuildUser obj) => obj.Id.GetHashCode();
    }

    public class CommandHandler : INService
    {
        public const int GlobalCommandsCooldown = 750;

        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;
        private readonly Logger _log;
        private readonly MitternachtBot _bot;
        private INServiceProvider _services;
        public string DefaultPrefix { get; private set; }
        private ConcurrentDictionary<ulong, string> Prefixes { get; }

        public event Func<IUserMessage, CommandInfo, Task> CommandExecuted = delegate { return Task.CompletedTask; };
        public event Func<CommandInfo, ITextChannel, string, Task> CommandErrored = delegate { return Task.CompletedTask; };
        public event Func<IUserMessage, Task> OnMessageNoTrigger = delegate { return Task.CompletedTask; };

        //userid/msg count
        public ConcurrentDictionary<ulong, uint> UserMessagesSent { get; } = new ConcurrentDictionary<ulong, uint>();

        public ConcurrentHashSet<ulong> UsersOnShortCooldown { get; } = new ConcurrentHashSet<ulong>();
        private readonly Timer _clearUsersOnShortCooldown;

        public CommandHandler(DiscordSocketClient client, DbService db, IBotConfigProvider bc, IEnumerable<GuildConfig> gcs, CommandService commandService, MitternachtBot bot)
        {
            _client = client;
            _commandService = commandService;
            _bot = bot;
            _db = db;

            _log = LogManager.GetCurrentClassLogger();

            _clearUsersOnShortCooldown = new Timer(_ =>
            {
                UsersOnShortCooldown.Clear();
            }, null, GlobalCommandsCooldown, GlobalCommandsCooldown);

            DefaultPrefix = bc.BotConfig.DefaultPrefix;
            Prefixes = gcs
                .Where(x => x.Prefix != null)
                .ToDictionary(x => x.GuildId, x => x.Prefix)
                .ToConcurrent();
        }

        public string GetPrefix(IGuild guild) => GetPrefix(guild?.Id);

        public string GetPrefix(ulong? id)
        {
            if (id == null || !Prefixes.TryGetValue(id.Value, out var prefix))
                return DefaultPrefix;

            return prefix;
        }

        public string SetDefaultPrefix(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentNullException(nameof(prefix));

            prefix = prefix.ToLowerInvariant();

            using (var uow = _db.UnitOfWork)
            {
                uow.BotConfig.GetOrCreate(set => set).DefaultPrefix = prefix;
                uow.Complete();
            }

            return DefaultPrefix = prefix;
        }
        public string SetPrefix(IGuild guild, string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentNullException(nameof(prefix));
            if (guild == null)
                throw new ArgumentNullException(nameof(guild));

            prefix = prefix.ToLowerInvariant();

            using (var uow = _db.UnitOfWork)
            {
                var gc = uow.GuildConfigs.For(guild.Id, set => set);
                gc.Prefix = prefix;
                uow.Complete();
            }
            Prefixes.AddOrUpdate(guild.Id, prefix, (key, old) => prefix);

            return prefix;
        }


        public void AddServices(INServiceProvider services)
        {
            _services = services;
        }

        public async Task ExecuteExternal(ulong? guildId, ulong channelId, string commandText)
        {
            if (guildId != null)
            {
                var guild = _client.GetGuild(guildId.Value);
                if (!(guild?.GetChannel(channelId) is SocketTextChannel channel))
                {
                    _log.Warn("Channel for external execution not found.");
                    return;
                }

                try
                {
                    IUserMessage msg = await channel.SendMessageAsync(commandText).ConfigureAwait(false);
                    msg = (IUserMessage)await channel.GetMessageAsync(msg.Id).ConfigureAwait(false);
                    await TryRunCommand(guild, channel, msg).ConfigureAwait(false);
                    //msg.DeleteAfter(5);
                }
                catch { }
            }
        }

        public Task StartHandling()
        {
            _client.MessageReceived += (msg) => { var _ = Task.Run(() => MessageReceivedHandler(msg)); return Task.CompletedTask; };
            return Task.CompletedTask;
        }

        private const float OneThousandth = 1.0f / 1000;
        private readonly DbService _db;

        private Task LogSuccessfulExecution(IMessage usrMsg, IGuildChannel channel, params int[] execPoints)
        {
            _log.Info("Command Executed after " + string.Join("/", execPoints.Select(x => x * OneThousandth)) + "s\n\t" +
                        "User: {0}\n\t" +
                        "Server: {1}\n\t" +
                        "Channel: {2}\n\t" +
                        "Message: {3}",
                        usrMsg.Author + " [" + usrMsg.Author.Id + "]", // {0}
                        (channel == null ? "PRIVATE" : channel.Guild.Name + " [" + channel.Guild.Id + "]"), // {1}
                        (channel == null ? "PRIVATE" : channel.Name + " [" + channel.Id + "]"), // {2}
                        usrMsg.Content // {3}
                        );
            return Task.CompletedTask;
        }

        private void LogErroredExecution(string errorMessage, IMessage usrMsg, IGuildChannel channel, params int[] execPoints)
        {
            _log.Warn("Command Errored after " + string.Join("/", execPoints.Select(x => x * OneThousandth)) + "s\n\t" +
                        "User: {0}\n\t" +
                        "Server: {1}\n\t" +
                        "Channel: {2}\n\t" +
                        "Message: {3}\n\t" +
                        "Error: {4}",
                        usrMsg.Author + " [" + usrMsg.Author.Id + "]", // {0}
                        (channel == null ? "PRIVATE" : channel.Guild.Name + " [" + channel.Guild.Id + "]"), // {1}
                        (channel == null ? "PRIVATE" : channel.Name + " [" + channel.Id + "]"), // {2}
                        usrMsg.Content,// {3}
                        errorMessage
                        //exec.Result.ErrorReason // {4}
                        );
        }

        private async Task MessageReceivedHandler(SocketMessage msg)
        {
            try
            {
                if (msg.Author.IsBot || !_bot.Ready.Task.IsCompleted) //no bots, wait until bot connected and initialized
                    return;

                if (!(msg is SocketUserMessage usrMsg))
                    return;
#if !GLOBAL_NADEKO
                // track how many messagges each user is sending
                UserMessagesSent.AddOrUpdate(usrMsg.Author.Id, 1, (key, old) => ++old);
#endif

                var channel = msg.Channel;
                var guild = (msg.Channel as SocketTextChannel)?.Guild;

                await TryRunCommand(guild, channel, usrMsg);
            }
            catch (Exception ex)
            {
                _log.Warn("Error in CommandHandler");
                _log.Warn(ex);
                if (ex.InnerException != null)
                {
                    _log.Warn("Inner Exception of the error in CommandHandler");
                    _log.Warn(ex.InnerException);
                }
            }
        }

        public async Task TryRunCommand(SocketGuild guild, ISocketMessageChannel channel, IUserMessage usrMsg)
        {
            var execTime = Environment.TickCount;

            //its nice to have early blockers and early blocking executors separate, but
            //i could also have one interface with priorities, and just put early blockers on
            //highest priority. :thinking:
            foreach (var svc in _services)
            {
                if (!(svc is IEarlyBlocker blocker) || !await blocker.TryBlockEarly(guild, usrMsg).ConfigureAwait(false)) continue;
                _log.Info("Blocked User: [{0}] Message: [{1}] Service: [{2}]", usrMsg.Author, usrMsg.Content, svc.GetType().Name);
                return;
            }

            var exec2 = Environment.TickCount - execTime;

            foreach (var svc in _services)
            {
                if (!(svc is IEarlyBlockingExecutor exec) || !await exec.TryExecuteEarly(_client, guild, usrMsg).ConfigureAwait(false)) continue;
                _log.Info("User [{0}] executed [{1}] in [{2}]", usrMsg.Author, usrMsg.Content, svc.GetType().Name);
                return;
            }

            var exec3 = Environment.TickCount - execTime;

            var messageContent = usrMsg.Content;
            foreach (var svc in _services)
            {
                string newContent;
                if (!(svc is IInputTransformer exec) || (newContent = await exec.TransformInput(guild, usrMsg.Channel, usrMsg.Author, messageContent).ConfigureAwait(false)) == messageContent.ToLowerInvariant()) continue;
                messageContent = newContent;
                break;
            }
            var prefix = GetPrefix(guild?.Id);
            var isPrefixCommand = messageContent.StartsWith(".prefix");
            // execute the command and measure the time it took
            if (messageContent.StartsWith(prefix) || isPrefixCommand)
            {
                var result = await ExecuteCommandAsync(new CommandContext(_client, usrMsg), messageContent, isPrefixCommand ? 1 : prefix.Length, _services, MultiMatchHandling.Best);
                execTime = Environment.TickCount - execTime;

                if (result.Success)
                {
                    await LogSuccessfulExecution(usrMsg, channel as ITextChannel, exec2, exec3, execTime).ConfigureAwait(false);
                    await CommandExecuted(usrMsg, result.Info).ConfigureAwait(false);
                    return;
                }
                else if (result.Error != null)
                {
                    LogErroredExecution(result.Error, usrMsg, channel as ITextChannel, exec2, exec3, execTime);
                    if (guild != null)
                        await CommandErrored(result.Info, channel as ITextChannel, result.Error);
                }
            }
            else
            {
                await OnMessageNoTrigger(usrMsg).ConfigureAwait(false);
            }

            foreach (var svc in _services)
            {
                if (svc is ILateExecutor exec)
                {
                    await exec.LateExecute(_client, guild, usrMsg).ConfigureAwait(false);
                }
            }

        }

        public Task<(bool Success, string Error, CommandInfo Info)> ExecuteCommandAsync(CommandContext context, string input, int argPos, IServiceProvider serviceProvider, MultiMatchHandling multiMatchHandling = MultiMatchHandling.Exception)
            => ExecuteCommand(context, input.Substring(argPos), serviceProvider, multiMatchHandling);


        public async Task<(bool Success, string Error, CommandInfo Info)> ExecuteCommand(CommandContext context, string input, IServiceProvider services, MultiMatchHandling multiMatchHandling = MultiMatchHandling.Exception)
        {
            var searchResult = _commandService.Search(context, input);
            if (!searchResult.IsSuccess)
                return (false, null, null);

            var commands = searchResult.Commands;
            var preconditionResults = new Dictionary<CommandMatch, PreconditionResult>();

            foreach (var match in commands)
            {
                preconditionResults[match] = await match.Command.CheckPreconditionsAsync(context, services).ConfigureAwait(false);
            }

            var successfulPreconditions = preconditionResults
                .Where(x => x.Value.IsSuccess)
                .ToArray();

            if (successfulPreconditions.Length == 0)
            {
                //All preconditions failed, return the one from the highest priority command
                var bestCandidate = preconditionResults
                    .OrderByDescending(x => x.Key.Command.Priority)
                    .FirstOrDefault(x => !x.Value.IsSuccess);
                return (false, bestCandidate.Value.ErrorReason, commands[0].Command);
            }

            var parseResultsDict = new Dictionary<CommandMatch, ParseResult>();
            foreach (var pair in successfulPreconditions)
            {
                var parseResult = await pair.Key.ParseAsync(context, searchResult, pair.Value, services).ConfigureAwait(false);

                if (parseResult.Error == CommandError.MultipleMatches)
                {
                    if (multiMatchHandling == MultiMatchHandling.Best)
                    {
                        IReadOnlyList<TypeReaderValue> argList = parseResult.ArgValues.Select(x => x.Values.OrderByDescending(y => y.Score).First())
                            .ToImmutableArray();
                        IReadOnlyList<TypeReaderValue> paramList = parseResult.ParamValues
                            .Select(x => x.Values.OrderByDescending(y => y.Score).First()).ToImmutableArray();
                        parseResult = ParseResult.FromSuccess(argList, paramList);
                    }
                }

                parseResultsDict[pair.Key] = parseResult;
            }
            // Calculates the 'score' of a command given a parse result
            float CalculateScore(CommandMatch match, ParseResult parseResult)
            {
                float argValuesScore = 0, paramValuesScore = 0;

                if (match.Command.Parameters.Count > 0)
                {
                    var argValuesSum = parseResult.ArgValues?.Sum(x => x.Values.OrderByDescending(y => y.Score).FirstOrDefault().Score) ?? 0;
                    var paramValuesSum = parseResult.ParamValues?.Sum(x => x.Values.OrderByDescending(y => y.Score).FirstOrDefault().Score) ?? 0;

                    argValuesScore = argValuesSum / match.Command.Parameters.Count;
                    paramValuesScore = paramValuesSum / match.Command.Parameters.Count;
                }

                var totalArgsScore = (argValuesScore + paramValuesScore) / 2;
                return match.Command.Priority + totalArgsScore * 0.99f;
            }

            //Order the parse results by their score so that we choose the most likely result to execute
            var parseResults = parseResultsDict
                .OrderByDescending(x => CalculateScore(x.Key, x.Value));

            var successfulParses = parseResults
                .Where(x => x.Value.IsSuccess)
                .ToArray();

            if (successfulParses.Length == 0)
            {
                //All parses failed, return the one from the highest priority command, using score as a tie breaker
                var bestMatch = parseResults
                    .FirstOrDefault(x => !x.Value.IsSuccess);
                return (false, bestMatch.Value.ErrorReason, commands[0].Command);
            }

            var cmd = successfulParses[0].Key.Command;

            // Bot will ignore commands which are ran more often than what specified by
            // GlobalCommandsCooldown constant (miliseconds)
            if (!UsersOnShortCooldown.Add(context.Message.Author.Id))
                return (false, null, cmd);
            //return SearchResult.FromError(CommandError.Exception, "You are on a global cooldown.");

            var commandName = cmd.Aliases.First();
            foreach (var svc in _services)
            {
                if (!(svc is ILateBlocker exec) || !await exec
                        .TryBlockLate(_client, context.Message, context.Guild, context.Channel, context.User,
                            cmd.Module.GetTopLevelModule().Name, commandName).ConfigureAwait(false)) continue;
                _log.Info("Late blocking User [{0}] Command: [{1}] in [{2}]", context.User, commandName, svc.GetType().Name);
                return (false, null, cmd);
            }

            //If we get this far, at least one parse was successful. Execute the most likely overload.
            var chosenOverload = successfulParses[0];
            var execResult = (ExecuteResult)await chosenOverload.Key.ExecuteAsync(context, chosenOverload.Value, services).ConfigureAwait(false);

            if (execResult.Exception == null || (execResult.Exception is HttpException he && he.DiscordCode == 50013))
                return (true, null, cmd);
            lock (_errorLogLock)
            {
                var now = DateTime.Now;
                File.AppendAllText($"./command_errors_{now:yyyy-MM-dd}.txt",
                    $"[{now:HH:mm-yyyy-MM-dd}]" + Environment.NewLine
                    + execResult.Exception + Environment.NewLine
                    + "------" + Environment.NewLine);
                _log.Warn(execResult.Exception);
            }

            return (true, null, cmd);
        }

        private readonly object _errorLogLock = new object();
    }
}