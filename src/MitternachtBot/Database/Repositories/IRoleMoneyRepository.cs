using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories {
	public interface IRoleMoneyRepository : IRepository<RoleMoney> {
		RoleMoney GetOrCreate(ulong guildId, ulong roleId);
		void SetMoney(ulong guildId, ulong roleId, long? money = null, int? priority = null);
		bool MoneyForRoleIsDefined(ulong guildId, ulong roleId);
		bool Remove(ulong guildId, ulong roleId);
	}
}
