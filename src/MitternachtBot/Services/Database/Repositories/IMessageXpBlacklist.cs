using Discord;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories {
	public interface IMessageXpBlacklist : IRepository<MessageXpRestriction> {
		bool CreateRestriction(ITextChannel channel);
		bool CreateRestriction(ulong guildId, ulong channelId);
		bool IsRestricted(ITextChannel channel);
		bool IsRestricted(ulong guildId, ulong channelId);
		bool RemoveRestriction(ITextChannel channel);
		bool RemoveRestriction(ulong guildId, ulong channelId);
		ulong[] GetRestrictedChannelsForGuild(ulong guildId);
	}
}