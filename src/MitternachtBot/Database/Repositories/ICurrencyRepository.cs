using System.Linq;
using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories {
	public interface ICurrencyRepository : IRepository<Currency> {
		Currency GetOrCreate(ulong guildId, ulong userId);
		long GetUserCurrencyValue(ulong guildId, ulong userId);
		bool TryAddCurrencyValue(ulong guildId, ulong userId, long change);
		IQueryable<Currency> OrderByAmount(ulong guildId);
	}
}
