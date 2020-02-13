using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Mitternacht.Common.ModuleBehaviors;
using Mitternacht.Modules.Games.Common;
using Mitternacht.Services;
using Mitternacht.Services.Impl;
using NLog;

namespace Mitternacht.Modules.Games.Services
{
    public class PollService : IEarlyBlockingExecutor, IMService
    {
        public ConcurrentDictionary<ulong, Poll> ActivePolls = new ConcurrentDictionary<ulong, Poll>();
        private readonly Logger _log;
        private readonly StringService _strings;

        public PollService(StringService strings)
        {
            _log = LogManager.GetCurrentClassLogger();
            _strings = strings;
        }

        public async Task<bool?> StartPoll(ITextChannel channel, IUserMessage msg, string arg)
        {
            if (string.IsNullOrWhiteSpace(arg) || !arg.Contains(";")) return null;
            var data = (from choice in arg.Split(';') where !string.IsNullOrWhiteSpace(choice) select choice).ToArray();
            if (data.Length < 3) return null;

            var poll = new Poll(_strings, msg, data[0], data.Skip(1));
            if (!ActivePolls.TryAdd(channel.Guild.Id, poll)) return false;
            poll.OnEnded += gid =>
            {
                ActivePolls.TryRemove(gid, out _);
            };

            await poll.StartPoll().ConfigureAwait(false);
            return true;
        }

        public async Task<bool> TryExecuteEarly(DiscordSocketClient client, IGuild guild, IUserMessage msg, bool realExecution = true)
        {
            if (guild == null || !ActivePolls.TryGetValue(guild.Id, out var poll)) return false;
            if (!realExecution) return true;

            try
            {
                return await poll.TryVote(msg).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
            }

            return false;
        }
    }
}
