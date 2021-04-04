using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories.Impl {
	public class CurrencyTransactionsRepository : Repository<CurrencyTransaction>, ICurrencyTransactionsRepository {
		public CurrencyTransactionsRepository(MitternachtContext context) : base(context) {
		}
	}
}
