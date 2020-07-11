using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MitternachtWeb.Controllers;
using System;

namespace MitternachtWeb.Areas.User.Controllers {
	public abstract class UserBaseController : DiscordUserController {
		[ViewData]
		public ulong RequestedUserId { get; set; }
		[ViewData]
		public SocketUser RequestedSocketUser { get; set; }

		public override void OnActionExecuting(ActionExecutingContext context) {
			if(RouteData.Values.TryGetValue("userId", out var userIdString)) {
				if(ulong.TryParse(userIdString.ToString(), out var userId)) {
					RequestedUserId     = userId;
					RequestedSocketUser = Program.MitternachtBot.Client.GetUser(RequestedUserId);
				} else {
					throw new ArgumentException("userId");
				}
			} else {
				throw new ArgumentNullException("userId");
			}

			base.OnActionExecuting(context);
		}
	}
}
