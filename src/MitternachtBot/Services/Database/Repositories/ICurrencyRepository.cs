using System.Linq;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories {
	public interface ICurrencyRepository : IRepository<Currency> {
		Currency GetOrCreate(ulong userId);
		long GetUserCurrencyValue(ulong userId);
		bool TryAddCurrencyValue(ulong userId, long change);
		IQueryable<Currency> OrderByAmount();
	}
}
