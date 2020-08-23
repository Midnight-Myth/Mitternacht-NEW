using Discord.WebSocket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace MitternachtWeb.Areas.User.Controllers {
	[Authorize]
	[Area("User")]
	public class OverviewController : UserBaseController {
		public IActionResult Index() {
			var loggedInUserGuilds = DiscordUser.GuildPagePermissions.Keys.ToArray();
			var guilds = RequestedSocketUser != null ? RequestedSocketUser.MutualGuilds.Where(g => loggedInUserGuilds.Contains(g.Id)).ToArray() : new SocketGuild[0];

			return View(guilds);
		}
	}
}
