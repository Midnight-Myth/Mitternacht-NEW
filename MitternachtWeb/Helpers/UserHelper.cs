using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace MitternachtWeb.Helpers {
	public static class UserHelper {
		private static readonly Dictionary<ulong, (DateTime RequestTime, IUser User)> discordUsers = new Dictionary<ulong, (DateTime, IUser)>();

		public static IUser GetDiscordUser(ClaimsPrincipal user) {
			var userIdString = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
			if(ulong.TryParse(userIdString, out var userId)) {
				var success = discordUsers.TryGetValue(userId, out var t);

				if(success && (t.RequestTime-DateTime.UtcNow).TotalSeconds<15.0) {
					return t.User;
				} else {
					var discordUser = Program.MitternachtBot.Client.GetUser(userId);
					discordUsers.Add(userId, (DateTime.UtcNow, discordUser));
					return discordUser;
				}
			} else {
				return null;
			}
		}
	}
}
