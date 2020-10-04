using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories.Impl {
	public class CurrencyRepository : Repository<Currency>, ICurrencyRepository {
		public CurrencyRepository(DbContext context) : base(context) { }

		public Currency GetOrCreate(ulong guildId, ulong userId) {
			var currency = _set.FirstOrDefault(c => c.GuildId == guildId && c.UserId == userId);

			if(currency == null) {
				_set.Add(currency = new Currency() {
					GuildId = guildId,
					UserId = userId,
					Amount = 0,
				});
			}

			return currency;
		}

		public IQueryable<Currency> OrderByAmount(ulong guildId)
			=> _set.AsQueryable().Where(c => c.GuildId == guildId).OrderByDescending(c => c.Amount);

		public long GetUserCurrencyValue(ulong guildId, ulong userId)
			=> GetOrCreate(guildId, userId).Amount;

		public bool TryAddCurrencyValue(ulong guildId, ulong userId, long change) {
			var currency = GetOrCreate(guildId, userId);
			var canApplyChange = change != 0 && (change > 0 || currency.Amount + change >= 0);

			currency.Amount += canApplyChange ? change : 0;

			return canApplyChange;
		}
	}
}
