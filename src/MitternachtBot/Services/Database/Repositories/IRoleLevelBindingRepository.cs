using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories {
	public interface IRoleLevelBindingRepository : IRepository<RoleLevelBinding> {
		void SetBinding(ulong guildId, ulong roleid, int level);
		bool Remove(ulong guildId, ulong roleid);
	}
}
