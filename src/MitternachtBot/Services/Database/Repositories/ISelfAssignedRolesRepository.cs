using System.Linq;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories {
	public interface ISelfAssignedRolesRepository : IRepository<SelfAssignedRole> {
		bool DeleteByGuildAndRoleId(ulong guildId, ulong roleId);
		IQueryable<SelfAssignedRole> GetFromGuild(ulong guildId);
	}
}
