using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace MitternachtWeb.Controllers {
	[Authorize]
	public class GuildListController : DiscordUserController {
		public IActionResult Index() {
			var readableGuilds = ReadableGuilds;
			var guilds = Program.MitternachtBot.Client.Guilds.Where(g => readableGuilds.Contains(g.Id)).ToList();
			return View(guilds);
		}
	}
}
