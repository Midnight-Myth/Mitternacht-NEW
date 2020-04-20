using MitternachtWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace MitternachtWeb.Helpers {
	public static class UserHelper {
		private static readonly Dictionary<ulong, (DateTime RequestTime, DiscordUser User)> discordUsers = new Dictionary<ulong, (DateTime, DiscordUser)>();

		public static DiscordUser GetDiscordUser(ClaimsPrincipal user) {
			var userIdString = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
			if(ulong.TryParse(userIdString, out var userId)) {
				var success = discordUsers.TryGetValue(userId, out var t);

				if(success && (t.RequestTime-DateTime.UtcNow).TotalSeconds<15.0) {
					return t.User;
				} else {
					var dUser = Program.MitternachtBot.Client.GetUser(userId);
					var discordUser = new DiscordUser {
						User = dUser
					};
					discordUsers.Add(userId, (DateTime.UtcNow, discordUser));
					return discordUser;
				}
			} else {
				return null;
			}
		}
	}
}
