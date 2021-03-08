using System.Linq;
using System.Threading.Tasks;
using Discord;
using Mitternacht.Common.ModuleBehaviors;
using Mitternacht.Services;
using Mitternacht.Common;

namespace Mitternacht.Modules.Permissions.Services {
	public class BlacklistService : IEarlyBlocker, IMService {
		private readonly IBotCredentials    _creds;
		private readonly IBotConfigProvider _bc;

		public ulong[] BlacklistedUsers    => _bc.BotConfig.Blacklist.Where(bi => bi.Type == BlacklistType.User   ).Select(c => c.ItemId).ToArray();
		public ulong[] BlacklistedGuilds   => _bc.BotConfig.Blacklist.Where(bi => bi.Type == BlacklistType.Server ).Select(c => c.ItemId).ToArray();
		public ulong[] BlacklistedChannels => _bc.BotConfig.Blacklist.Where(bi => bi.Type == BlacklistType.Channel).Select(c => c.ItemId).ToArray();

		public BlacklistService(IBotCredentials creds, IBotConfigProvider bc) {
			_creds = creds;
			_bc    = bc;
		}

		public Task<bool> TryBlockEarly(IGuild guild, IUserMessage userMessage, bool realExecution = true)
			=> Task.FromResult(!_creds.IsOwner(userMessage.Author) && (guild != null && BlacklistedGuilds.Contains(guild.Id) || BlacklistedChannels.Contains(userMessage.Channel.Id) || BlacklistedUsers.Contains(userMessage.Author.Id)));
	}
}
