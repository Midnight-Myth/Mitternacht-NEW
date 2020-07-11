using System.Linq;
using Discord;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories.Impl {
	public class MessageXpRestrictionRepository : Repository<MessageXpRestriction>, IMessageXpRestrictionRepository {
		public MessageXpRestrictionRepository(DbContext context) : base(context) { }

		public bool CreateRestriction(ITextChannel channel)
			=> CreateRestriction(channel.GuildId, channel.Id);

		public bool CreateRestriction(ulong guildId, ulong channelId) {
			if(!IsRestricted(guildId, channelId)) {
				_set.Add(new MessageXpRestriction {
					ChannelId = channelId,
					GuildId = guildId,
				});

				return true;
			} else {
				return false;
			}
		}

		public bool IsRestricted(ITextChannel channel)
			=> IsRestricted(channel.GuildId, channel.Id);

		public bool IsRestricted(ulong guildId, ulong channelId)
			=> _set.Any(m => m.GuildId == guildId && m.ChannelId == channelId);

		public bool RemoveRestriction(ITextChannel channel)
			=> RemoveRestriction(channel.GuildId, channel.Id);

		public bool RemoveRestriction(ulong guildId, ulong channelId) {
			var restrictions = _set.AsQueryable().Where(r => r.GuildId == guildId && r.ChannelId == channelId).ToList();
			
			if(restrictions.Any()) {
				_set.RemoveRange(restrictions);
				return true;
			} else {
				return false;
			}
		}

		public IQueryable<ulong> GetRestrictedChannelsForGuild(ulong guildId)
			=> _set.AsQueryable().Where(m => m.GuildId == guildId).Select(m => m.ChannelId);
	}
}