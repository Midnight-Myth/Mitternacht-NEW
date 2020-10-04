using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories.Impl {
	public class RoleLevelBindingRepository : Repository<RoleLevelBinding>, IRoleLevelBindingRepository {
		public RoleLevelBindingRepository(DbContext context) : base(context) { }

		public bool Remove(ulong guildId, ulong roleId) {
			var rl = _set.FirstOrDefault(r => r.GuildId == guildId && r.RoleId == roleId);
			
			if(rl != null) {
				_set.Remove(rl);

				return true;
			} else {
				return false;
			}
		}

		public void SetBinding(ulong guildId, ulong roleId, int level) {
			var rl = _set.FirstOrDefault(r => r.GuildId == guildId && r.RoleId == roleId);
			
			if(rl == null) {
				_set.Add(rl = new RoleLevelBinding {
					GuildId      = guildId,
					RoleId       = roleId,
					MinimumLevel = level,
				});
			} else {
				rl.MinimumLevel = level;
			}
		}
	}
}
