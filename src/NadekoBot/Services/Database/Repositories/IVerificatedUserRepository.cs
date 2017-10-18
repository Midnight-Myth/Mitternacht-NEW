using System.Collections.Generic;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories
{
    public interface IVerificatedUserRepository : IRepository<VerificatedUser>
    {
        void SetVerified(ulong guildId, ulong userId, long forumUserId);
        bool IsDiscordUserVerified(ulong guildId, ulong userId);
        bool IsForumUserVerified(ulong guildId, long forumUserId);
        bool IsVerified(ulong guildId, ulong userId, long forumUserId);
        bool IsForumUserIndependentFromDiscordUser(ulong guildId, ulong userId, long forumUserId);
        bool RemoveVerification(ulong guildId, ulong userId);
        bool RemoveVerification(ulong guildId, long forumUserId);
        IEnumerable<VerificatedUser> GetVerificatedUsers(ulong guildId);
        int GetCount(ulong guildId);
    }
}