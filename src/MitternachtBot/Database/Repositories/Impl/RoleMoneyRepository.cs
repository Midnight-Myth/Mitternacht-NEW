using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories.Impl {
	public class RoleMoneyRepository : Repository<RoleMoney>, IRoleMoneyRepository {
		public RoleMoneyRepository(DbContext context) : base(context) { }

		public RoleMoney GetOrCreate(ulong guildId, ulong roleId) {
			var rm = _set.FirstOrDefault(m => m.GuildId == guildId && m.RoleId == roleId);

			if(rm == null) {
				_set.Add(rm = new RoleMoney {
					GuildId  = guildId,
					RoleId   = roleId,
					Money    = 0,
					Priority = 0,
				});
			}

			return rm;
		}

		public void SetMoney(ulong guildId, ulong roleId, long money) {
			GetOrCreate(guildId, roleId).Money = money;
		}

		public bool MoneyForRoleIsDefined(ulong guildId, ulong roleId)
			=> _set.Any(rm => rm.GuildId == guildId && rm.RoleId == roleId);

		public void SetPriority(ulong guildId, ulong roleId, int priority) {
			GetOrCreate(guildId, roleId).Priority = priority;
		}

		public bool Remove(ulong guildId, ulong roleId) {
			var rm = _set.FirstOrDefault(m => m.GuildId == guildId && m.RoleId == roleId);

			if(rm != null) {
				_set.Remove(rm);

				return true;
			} else {
				return false;
			}
		}
	}
}
