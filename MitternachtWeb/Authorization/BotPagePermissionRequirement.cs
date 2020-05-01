using Microsoft.AspNetCore.Authorization;
using MitternachtWeb.Models;

namespace MitternachtWeb.Authorization {
	public class BotPagePermissionRequirement : IAuthorizationRequirement {
		public BotPagePermission BotPagePermissions { get; set; }

		public BotPagePermissionRequirement(BotPagePermission perms) {
			BotPagePermissions = perms;
		}
	}
}
