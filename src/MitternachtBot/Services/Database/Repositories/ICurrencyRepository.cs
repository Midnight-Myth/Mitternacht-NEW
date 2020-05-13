using System.Collections.Generic;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories
{
    public interface ICurrencyRepository : IRepository<Currency>
    {
        Currency GetOrCreate(ulong userId);
        long GetUserCurrency(ulong userId);
        bool TryUpdateState(ulong userId, long change);
        IEnumerable<Currency> GetTopRichest(int count, int skip);
    }
}
