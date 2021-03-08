using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories {
	public interface IRoleLevelBindingRepository : IRepository<RoleLevelBinding> {
		void SetBinding(ulong guildId, ulong roleId, int level);
		bool Remove(ulong guildId, ulong roleId);
	}
}
