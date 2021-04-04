using System.Linq;
using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories.Impl {
	public class RoleMoneyRepository : Repository<RoleMoney>, IRoleMoneyRepository {
		public RoleMoneyRepository(MitternachtContext context) : base(context) { }

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

		public void SetMoney(ulong guildId, ulong roleId, long? money = null, int? priority = null) {
			var rm = GetOrCreate(guildId, roleId);

			if(money.HasValue) {
				rm.Money = money.Value;
			}

			if(priority.HasValue) {
				rm.Priority = priority.Value;
			}
		}

		public bool MoneyForRoleIsDefined(ulong guildId, ulong roleId)
			=> _set.Any(rm => rm.GuildId == guildId && rm.RoleId == roleId);

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
