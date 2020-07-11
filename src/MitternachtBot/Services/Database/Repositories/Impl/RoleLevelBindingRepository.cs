using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories.Impl {
	public class RoleLevelBindingRepository : Repository<RoleLevelBinding>, IRoleLevelBindingRepository {
		public RoleLevelBindingRepository(DbContext context) : base(context) { }

		public bool Remove(ulong roleid) {
			var rl = _set.FirstOrDefault(r => r.RoleId == roleid);
			
			if(rl != null) {
				_set.Remove(rl);

				return true;
			} else {
				return false;
			}
		}

		public void SetBinding(ulong roleid, int level) {
			var rl = _set.FirstOrDefault(r => r.RoleId == roleid);
			
			if(rl == null) {
				_set.Add(rl = new RoleLevelBinding {
					RoleId       = roleid,
					MinimumLevel = level,
				});
			} else {
				rl.MinimumLevel = level;
			}
		}
	}
}
