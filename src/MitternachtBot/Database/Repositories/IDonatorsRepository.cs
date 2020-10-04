using System.Linq;
using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories {
	public interface IDonatorsRepository : IRepository<Donator> {
		IOrderedQueryable<Donator> GetDonatorsOrdered();
		Donator AddOrUpdateDonator(ulong userId, string name, int amount);
	}
}
