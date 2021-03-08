using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories.Impl {
	public class DonatorsRepository : Repository<Donator>, IDonatorsRepository {
		public DonatorsRepository(DbContext context) : base(context) { }

		public Donator AddOrUpdateDonator(ulong userId, string name, int amount) {
			var donator = _set.FirstOrDefault(d => d.UserId == userId);

			if(donator == null) {
				_set.Add(donator = new Donator {
					Amount = amount,
					UserId = userId,
					Name = name
				});
			} else {
				donator.Amount += amount;
				donator.Name = name;
			}

			return donator;
		}

		public IOrderedQueryable<Donator> GetDonatorsOrdered()
			=> _set.AsQueryable().OrderByDescending(d => d.Amount);
	}
}
