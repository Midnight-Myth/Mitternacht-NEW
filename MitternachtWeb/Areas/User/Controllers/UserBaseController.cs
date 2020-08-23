﻿using Discord.WebSocket;
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
				if(ulong.TryParse(userIdString.ToString(), out var userId)) {
					RequestedUserId     = userId;
					RequestedSocketUser = Program.MitternachtBot.Client.GetUser(RequestedUserId);

					if(RequestedSocketUser is null) {
						throw new UserNotFoundException(userId);
					}
				} else {
					throw new ArgumentException("Failed to parse the UserID.", nameof(userId));
				}
			} else {
				throw new ArgumentNullException("No UserID given.", nameof(userIdString));
			}

			base.OnActionExecuting(context);
		}
	}
}
