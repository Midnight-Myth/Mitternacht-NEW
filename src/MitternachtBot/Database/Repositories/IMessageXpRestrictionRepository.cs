using Discord;
using Mitternacht.Database.Models;
using System.Linq;

namespace Mitternacht.Database.Repositories {
	public interface IMessageXpRestrictionRepository : IRepository<MessageXpRestriction> {
		bool CreateRestriction(ITextChannel channel);
		bool CreateRestriction(ulong guildId, ulong channelId);
		bool IsRestricted(ITextChannel channel);
		bool IsRestricted(ulong guildId, ulong channelId);
		bool RemoveRestriction(ITextChannel channel);
		bool RemoveRestriction(ulong guildId, ulong channelId);
		IQueryable<ulong> GetRestrictedChannelsForGuild(ulong guildId);
	}
}