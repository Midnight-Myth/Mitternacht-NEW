using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories.Impl {
	public class CurrencyRepository : Repository<Currency>, ICurrencyRepository {
		public CurrencyRepository(DbContext context) : base(context) { }

		public Currency GetOrCreate(ulong userId) {
			var currency = _set.FirstOrDefault(c => c.UserId == userId);

			if(currency == null) {
				_set.Add(currency = new Currency() {
					UserId = userId,
					Amount = 0,
				});
			}

			return currency;
		}

		public IQueryable<Currency> GetTopRichest(int count, int skip = 0)
			=> _set.AsQueryable().OrderByDescending(c => c.Amount).Skip(skip).Take(count);

		public long GetUserCurrencyValue(ulong userId)
			=> GetOrCreate(userId).Amount;

		public bool TryAddCurrencyValue(ulong userId, long change) {
			var currency = GetOrCreate(userId);
			var canApplyChange = change != 0 && (change > 0 || currency.Amount + change >= 0);

			currency.Amount += canApplyChange ? change : 0;

			return canApplyChange;
		}
	}
}
