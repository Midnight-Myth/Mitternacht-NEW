using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories {
	public interface IRoleLevelBindingRepository : IRepository<RoleLevelBinding> {
		void SetBinding(ulong roleid, int level);
		bool Remove(ulong roleid);
		int GetMinimumLevel(ulong roleid);
	}
}
