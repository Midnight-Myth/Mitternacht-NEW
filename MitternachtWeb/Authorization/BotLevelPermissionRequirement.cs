using Microsoft.AspNetCore.Authorization;
using MitternachtWeb.Models;

namespace MitternachtWeb.Authorization {
	public class BotLevelPermissionRequirement : IAuthorizationRequirement {
		public BotLevelPermission BotLevelPermissions { get; set; }

		public BotLevelPermissionRequirement(BotLevelPermission perms) {
			BotLevelPermissions = perms;
		}
	}
}
