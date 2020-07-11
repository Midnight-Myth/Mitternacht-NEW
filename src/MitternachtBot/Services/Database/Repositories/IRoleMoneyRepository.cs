using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories {
	public interface IRoleMoneyRepository : IRepository<RoleMoney> {
		RoleMoney GetOrCreate(ulong roleId);
		void SetMoney(ulong roleId, long money);
		bool MoneyForRoleIsDefined(ulong roleId);
		void SetPriority(ulong roleId, int priority);
		bool Remove(ulong roleId);
	}
}
