using System.Linq;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories {
	public interface ICurrencyRepository : IRepository<Currency> {
		Currency GetOrCreate(ulong guildId, ulong userId);
		long GetUserCurrencyValue(ulong guildId, ulong userId);
		bool TryAddCurrencyValue(ulong guildId, ulong userId, long change);
		IQueryable<Currency> OrderByAmount(ulong guildId);
	}
}
