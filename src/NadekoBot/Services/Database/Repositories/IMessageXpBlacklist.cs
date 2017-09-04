using System.Threading.Tasks;
using Discord;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Services.Database.Repositories
{
    public interface IMessageXpBlacklist : IRepository<MessageXpRestriction>
    {
        bool CreateRestriction(ITextChannel channel);
        bool CreateRestriction(ulong guildId, ulong channelId);
        bool IsRestricted(ITextChannel channel);
        bool IsRestricted(ulong guildId, ulong channelId);
        bool RemoveRestriction(ITextChannel channel);
        bool RemoveRestriction(ulong guildId, ulong channelId);

        Task<bool> CreateRestrictionAsync(ITextChannel channel);
        Task<bool> CreateRestrictionAsync(ulong guildId, ulong channelId);
        Task<bool> IsRestrictedAsync(ITextChannel channel);
        Task<bool> IsRestrictedAsync(ulong guildId, ulong channelId);
        Task<bool> RemoveRestrictionAsync(ITextChannel channel);
        Task<bool> RemoveRestrictionAsync(ulong guildId, ulong channelId);
    }
}