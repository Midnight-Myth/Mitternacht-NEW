using System.Linq;
using System.Threading.Tasks;
using Discord;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Services.Database.Repositories.Impl
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
            _context.SaveChanges();
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

        public async Task<bool> CreateRestrictionAsync(ITextChannel channel)
            => await CreateRestrictionAsync(channel.GuildId, channel.Id);

        public async Task<bool> CreateRestrictionAsync(ulong guildId, ulong channelId) {
            if (await IsRestrictedAsync(guildId, channelId)) return false;
            await _set.AddAsync(new MessageXpRestriction {
                ChannelId = channelId,
                GuildId = guildId
            });
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsRestrictedAsync(ITextChannel channel)
            => await IsRestrictedAsync(channel.GuildId, channel.Id);

        public async Task<bool> IsRestrictedAsync(ulong guildId, ulong channelId)
            => await _set.AnyAsync(m => m.GuildId == guildId && m.ChannelId == channelId);

        public async Task<bool> RemoveRestrictionAsync(ITextChannel channel)
            => await RemoveRestrictionAsync(channel.GuildId, channel.Id);

        public async Task<bool> RemoveRestrictionAsync(ulong guildId, ulong channelId) {
            if (!await IsRestrictedAsync(guildId, channelId)) return false;
            _set.Remove(await _set.FirstAsync(m => m.GuildId == guildId && m.ChannelId == channelId));
            return true;
        }
    }
}