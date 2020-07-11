using System.Collections.Generic;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories {
	public interface IDonatorsRepository : IRepository<Donator> {
		IEnumerable<Donator> GetDonatorsOrdered();
		Donator AddOrUpdateDonator(ulong userId, string name, int amount);
	}
}
