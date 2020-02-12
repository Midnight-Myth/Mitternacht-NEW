using System.Linq;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories {
	public interface IUsernameHistoryRepository : IRepository<UsernameHistoryModel>
    {
        IOrderedQueryable<UsernameHistoryModel> GetUsernamesDescending(ulong userId);
		string GetLastUsername(ulong userId);
        bool AddUsername(ulong userId, string username, ushort discriminator);
    }
}