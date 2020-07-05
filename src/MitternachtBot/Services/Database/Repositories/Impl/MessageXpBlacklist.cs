using System;
using System.Linq;
using System.Linq.Expressions;
using Discord;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories.Impl
{
    public class MessageXpBlacklist : Repository<MessageXpRestriction>, IMessageXpBlacklist
    {
        public MessageXpBlacklist(DbContext context) : base(context) { }

        public bool CreateRestriction(ITextChannel channel)
            => CreateRestriction(channel.GuildId, channel.Id);

        public bool CreateRestriction(ulong guildId, ulong channelId) {
            if (IsRestricted(guildId, channelId)) return false;
            _set.Add(new MessageXpRestriction {
                ChannelId = channelId,
                GuildId = guildId
            });

            return true;
        }

		public bool IsRestricted(ITextChannel channel)
            => IsRestricted(channel.GuildId, channel.Id);

        public bool IsRestricted(ulong guildId, ulong channelId)
            => _set.Any(m => m.GuildId == guildId && m.ChannelId == channelId);

        public bool RemoveRestriction(ITextChannel channel)
            => RemoveRestriction(channel.GuildId, channel.Id);

        public bool RemoveRestriction(ulong guildId, ulong channelId) {
            if (!IsRestricted(guildId, channelId)) return false;
            _set.Remove(_set.First(m => m.GuildId == guildId && m.ChannelId == channelId));
            return true;
        }

		public ulong[] GetRestrictedChannelsForGuild(ulong guildId)
			=> _set.Where((Expression<Func<MessageXpRestriction, bool>>)(m => m.GuildId == guildId)).Select(m => m.ChannelId).ToArray();
	}
}