using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using MitternachtWeb.Helpers;
using System.Threading.Tasks;

namespace MitternachtWeb.Authorization {
	public class BotLevelPermissionHandler : AuthorizationHandler<BotLevelPermissionRequirement> {
		private readonly IHttpContextAccessor _httpContextAccessor;
		
		public BotLevelPermissionHandler(IHttpContextAccessor httpContextAccessor) {
			_httpContextAccessor = httpContextAccessor;
		}

		protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, BotLevelPermissionRequirement requirement) {
			var user = await UserHelper.GetDiscordUserAsync(context.User, _httpContextAccessor.HttpContext);

			if(user.BotPagePermissions.HasFlag(requirement.BotLevelPermissions)) {
				context.Succeed(requirement);
			}
		}
	}
}
