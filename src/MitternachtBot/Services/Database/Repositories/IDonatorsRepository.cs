using System.Linq;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories {
	public interface IDonatorsRepository : IRepository<Donator> {
		IOrderedQueryable<Donator> GetDonatorsOrdered();
		Donator AddOrUpdateDonator(ulong userId, string name, int amount);
	}
}
