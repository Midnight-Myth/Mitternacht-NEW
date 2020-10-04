using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories {
	public interface IRoleMoneyRepository : IRepository<RoleMoney> {
		RoleMoney GetOrCreate(ulong guildId, ulong roleId);
		void SetMoney(ulong guildId, ulong roleId, long money);
		bool MoneyForRoleIsDefined(ulong guildId, ulong roleId);
		void SetPriority(ulong guildId, ulong roleId, int priority);
		bool Remove(ulong guildId, ulong roleId);
	}
}
