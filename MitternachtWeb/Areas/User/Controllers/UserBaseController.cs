using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MitternachtWeb.Controllers;
using MitternachtWeb.Exceptions;
using System;

namespace MitternachtWeb.Areas.User.Controllers {
	public abstract class UserBaseController : DiscordUserController {
		[ViewData]
		public ulong RequestedUserId { get; set; }
		[ViewData]
		public SocketUser RequestedSocketUser { get; set; }

		public override void OnActionExecuting(ActionExecutingContext context) {
			if(RouteData.Values.TryGetValue("userId", out var userIdString)) {
				RequestedUserId = ulong.Parse(userIdString.ToString());
				RequestedSocketUser = Program.MitternachtBot.Client.GetUser(RequestedUserId);

				if(RequestedSocketUser == null) {
					throw new UserNotFoundException(RequestedUserId);
				}
			} else {
				throw new ArgumentNullException("userId");
			}

			base.OnActionExecuting(context);
		}
	}
}
