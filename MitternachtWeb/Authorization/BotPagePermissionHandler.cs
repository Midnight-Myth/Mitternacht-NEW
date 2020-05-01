using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using MitternachtWeb.Helpers;
using System.Threading.Tasks;

namespace MitternachtWeb.Authorization {
	public class BotPagePermissionHandler : AuthorizationHandler<BotPagePermissionRequirement> {
		private readonly IHttpContextAccessor _httpContextAccessor;
		
		public BotPagePermissionHandler(IHttpContextAccessor httpContextAccessor) {
			_httpContextAccessor = httpContextAccessor;
		}

		protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, BotPagePermissionRequirement requirement) {
			var user = await UserHelper.GetDiscordUserAsync(context.User, _httpContextAccessor.HttpContext);

			if(user.BotPagePermissions.HasFlag(requirement.BotPagePermissions)) {
				context.Succeed(requirement);
			}
		}
	}
}
