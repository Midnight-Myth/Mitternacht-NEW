using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Mitternacht.Common.ModuleBehaviors;
using Mitternacht.Extensions;
using Mitternacht.Modules.Games.Common.ChatterBot;
using Mitternacht.Modules.Permissions.Common;
using Mitternacht.Modules.Permissions.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;
using Mitternacht.Services.Impl;
using NLog;

namespace Mitternacht.Modules.Games.Services
{
    public class ChatterBotService : IEarlyBlockingExecutor, IMService
    {
        private readonly DiscordSocketClient _client;
        private readonly Logger _log;
        private readonly PermissionService _perms;
        private readonly CommandHandler _cmd;
        private readonly StringService _strings;
        private readonly IBotCredentials _creds;

        public ConcurrentDictionary<ulong, Lazy<IChatterBotSession>> ChatterBotGuilds { get; }

        public ChatterBotService(DiscordSocketClient client, PermissionService perms, IEnumerable<GuildConfig> gcs, CommandHandler cmd, StringService strings, IBotCredentials creds)
        {
            _client = client;
            _log = LogManager.GetCurrentClassLogger();
            _perms = perms;
            _cmd = cmd;
            _strings = strings;
            _creds = creds;

            ChatterBotGuilds = new ConcurrentDictionary<ulong, Lazy<IChatterBotSession>>(
                    gcs.Where(gc => gc.CleverbotEnabled)
                        .ToDictionary(gc => gc.GuildId, gc => new Lazy<IChatterBotSession>(CreateSession, true)));
        }

        public IChatterBotSession CreateSession() 
            => string.IsNullOrWhiteSpace(_creds.CleverbotApiKey) ? (IChatterBotSession) new ChatterBotSession() : new OfficialCleverbotSession(_creds.CleverbotApiKey);

        public string PrepareMessage(IUserMessage msg, out IChatterBotSession cleverbot)
        {
            var channel = msg.Channel as ITextChannel;
            cleverbot = null;

            if (channel == null) return null;

            if (!ChatterBotGuilds.TryGetValue(channel.Guild.Id, out var lazyCleverbot)) return null;

            cleverbot = lazyCleverbot.Value;

            var nadekoId = _client.CurrentUser.Id;
            var normalMention = $"<@{nadekoId}> ";
            var nickMention = $"<@!{nadekoId}> ";
            string message;
            if (msg.Content.StartsWith(normalMention))
            {
                message = msg.Content.Substring(normalMention.Length).Trim();
            }
            else if (msg.Content.StartsWith(nickMention))
            {
                message = msg.Content.Substring(nickMention.Length).Trim();
            }
            else
            {
                return null;
            }

            return message;
        }

        public async Task<bool> TryAsk(IChatterBotSession cleverbot, ITextChannel channel, string message)
        {
            await channel.TriggerTypingAsync().ConfigureAwait(false);

            var response = await cleverbot.Think(message).ConfigureAwait(false);
            try
            {
                await channel.SendConfirmAsync(response.SanitizeMentions()).ConfigureAwait(false);
            }
            catch
            {
                await channel.SendConfirmAsync(response.SanitizeMentions()).ConfigureAwait(false); // try twice :\
            }
            return true;
        }

        public async Task<bool> TryExecuteEarly(DiscordSocketClient client, IGuild guild, IUserMessage usrMsg, bool realExecution = true)
        {
            if (!(guild is SocketGuild))return false;

            try {
                var message = PrepareMessage(usrMsg, out var cbs);
                if (message == null || cbs == null) return false;
                if (!realExecution) return true;

                var pc = _perms.GetCache(guild.Id);
                if (!pc.Permissions.CheckPermissions(usrMsg, "cleverbot", "Games".ToLowerInvariant(), out var index)) {
                    if (!pc.Verbose) return true;
                    var returnMsg = _strings.GetText("Permissions".ToLowerInvariant(), "trigger", guild.Id, index + 1, Format.Bold(pc.Permissions[index].GetCommand(_cmd.GetPrefix(guild), (SocketGuild)guild)));
                    try {
                        await usrMsg.Channel.SendErrorAsync(returnMsg).ConfigureAwait(false);
                    }
                    catch {
                        /*ignored*/
                    }
                    _log.Info(returnMsg);
                    return true;
                }

                var cleverbotExecuted = await TryAsk(cbs, (ITextChannel) usrMsg.Channel, message).ConfigureAwait(false);
                if (cleverbotExecuted) {
                    _log.Info($"CleverBot Executed\nServer: {guild.Name} [{guild.Id}]\nChannel: {usrMsg.Channel?.Name} [{usrMsg.Channel?.Id}]\nUserId: {usrMsg.Author} [{usrMsg.Author.Id}]\nMessage: {usrMsg.Content}");
                    return true;
                }
            }
            catch (Exception ex) {
                if(realExecution) _log.Warn(ex, "Error in cleverbot");
            }
            return false;
        }
    }
}
