using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories.Impl {
	public class RoleMoneyRepository : Repository<RoleMoney>, IRoleMoneyRepository {
		public RoleMoneyRepository(DbContext context) : base(context) { }

		public RoleMoney GetOrCreate(ulong roleId) {
			var rm = _set.FirstOrDefault(m => m.RoleId == roleId);

			if(rm == null) {
				_set.Add(rm = new RoleMoney {
					RoleId   = roleId,
					Money    = 0,
					Priority = 0,
				});
			}

			return rm;
		}

		public void SetMoney(ulong roleId, long money) {
			GetOrCreate(roleId).Money = money;
		}

		public bool MoneyForRoleIsDefined(ulong roleId)
			=> _set.Any(rm => rm.RoleId == roleId);

		public void SetPriority(ulong roleId, int priority) {
			GetOrCreate(roleId).Priority = priority;
		}

		public bool Remove(ulong roleId) {
			var rm = _set.FirstOrDefault(m => m.RoleId == roleId);

			if(rm != null) {
				_set.Remove(rm);

				return true;
			} else {
				return false;
			}
		}
	}
}
