using System.Collections.Generic;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories
{
    public interface IUsernameHistoryRepository : IRepository<UsernameHistoryModel>
    {
        IEnumerable<UsernameHistoryModel> GetUserNames(ulong userId);
        bool AddUsername(ulong userId, string username);
    }
}