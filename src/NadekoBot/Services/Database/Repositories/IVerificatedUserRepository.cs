using System.Collections.Generic;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories
{
    public interface IVerificatedUserRepository : IRepository<VerificatedUser>
    {
        bool SetVerified(ulong guildId, ulong userId, long forumUserId);
        bool IsDiscordUserVerified(ulong guildId, ulong userId);
        bool IsForumUserVerified(ulong guildId, long forumUserId);
        bool IsVerified(ulong guildId, ulong userId, long forumUserId);
        bool CanVerifyForumAccount(ulong guildId, ulong userId, long forumUserId);
        bool RemoveVerification(ulong guildId, ulong userId);
        bool RemoveVerification(ulong guildId, long forumUserId);
        IEnumerable<VerificatedUser> GetVerifiedUsers(ulong guildId);
        long? GetVerifiedUserForumId(ulong guildId, ulong userId);
        int GetCount(ulong guildId);
    }
}