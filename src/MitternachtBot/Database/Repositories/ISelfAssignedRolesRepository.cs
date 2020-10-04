using System.Linq;
using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories {
	public interface ISelfAssignedRolesRepository : IRepository<SelfAssignedRole> {
		bool DeleteByGuildAndRoleId(ulong guildId, ulong roleId);
		IQueryable<SelfAssignedRole> GetFromGuild(ulong guildId);
	}
}
