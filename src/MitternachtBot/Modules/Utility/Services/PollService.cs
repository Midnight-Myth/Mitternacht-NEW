using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Mitternacht.Common.ModuleBehaviors;
using Mitternacht.Modules.Utility.Common;
using Mitternacht.Services;
using Mitternacht.Services.Impl;
using NLog;

namespace Mitternacht.Modules.Utility.Services {
	public class PollService : IEarlyBlockingExecutor, IMService {
		public ConcurrentDictionary<ulong, Poll> ActivePolls = new ConcurrentDictionary<ulong, Poll>();
		private readonly Logger _log;
		private readonly StringService _strings;

		public PollService(StringService strings) {
			_log = LogManager.GetCurrentClassLogger();
			_strings = strings;
		}

		public async Task<bool?> StartPoll(ITextChannel channel, IUserMessage msg, string arg) {
			if(!string.IsNullOrWhiteSpace(arg) && arg.Contains(";")) {
				var data = (from choice in arg.Split(';') where !string.IsNullOrWhiteSpace(choice) select choice).ToArray();

				if(data.Length >= 3) {
					var poll = new Poll(_strings, msg, data[0], data.Skip(1).ToArray());

					if(ActivePolls.TryAdd(channel.Guild.Id, poll)) {
						poll.OnEnded += gid => {
							ActivePolls.TryRemove(gid, out _);
						};

						await poll.StartPoll().ConfigureAwait(false);
						
						return true;
					} else {
						return false;
					}
				} else {
					return null;
				}
			} else {
				return null;
			}
		}

		public async Task<bool> TryExecuteEarly(DiscordSocketClient client, IGuild guild, IUserMessage msg, bool realExecution = true) {
			if(guild != null && ActivePolls.TryGetValue(guild.Id, out var poll)) {
				if(realExecution) {
					try {
						return await poll.TryVote(msg).ConfigureAwait(false);
					} catch(Exception e) {
						_log.Warn(e);
						return false;
					}
				} else {
					return true;
				}
			} else {
				return false;
			}
		}
	}
}
