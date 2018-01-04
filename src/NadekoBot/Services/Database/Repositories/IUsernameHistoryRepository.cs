using System.Collections.Generic;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories
{
    public interface IUsernameHistoryRepository : IRepository<UsernameHistoryModel>
    {
        IEnumerable<UsernameHistoryModel> GetGuildUserNames(ulong guildId, ulong userId);
        IEnumerable<UsernameHistoryModel> GetUserNames(ulong userId);
        IEnumerable<UsernameHistoryModel> GetGuildNames(ulong guildId);

        bool AddUsername(ulong guildId, ulong userId, string username, bool isNick);
    }
}