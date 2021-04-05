using System.Linq;
using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories.Impl {
	public class SelfAssignedRolesRepository : Repository<SelfAssignedRole>, ISelfAssignedRolesRepository {
		public SelfAssignedRolesRepository(MitternachtContext context) : base(context) { }

		public bool DeleteByGuildAndRoleId(ulong guildId, ulong roleId) {
			var role = _set.FirstOrDefault(s => s.GuildId == guildId && s.RoleId == roleId);

			if(role != null) {
				_set.Remove(role);
				return true;
			} else {
				return false;
			}
		}

		public IQueryable<SelfAssignedRole> GetFromGuild(ulong guildId)
			=> _set.AsQueryable().Where(s => s.GuildId == guildId);
	}
}
