using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MitternachtWeb.Models;
using System.Linq;

namespace MitternachtWeb.Controllers {
	[Authorize]
	public class ModeratableGuildsController : DiscordUserController {
		private ulong[] ReadableGuilds => DiscordUser.BotPagePermissions.HasFlag(BotLevelPermission.ReadAllModerations) ? DiscordUser.GuildPagePermissions.Select(kv => kv.Key).ToArray() : DiscordUser.GuildPagePermissions.Where(kv => kv.Value.HasFlag(GuildLevelPermission.ReadModeration)).Select(kv => kv.Key).ToArray();
		
		public IActionResult Index() {
			var readableGuilds = ReadableGuilds;
			var guilds = Program.MitternachtBot.Client.Guilds.Where(g => readableGuilds.Contains(g.Id)).ToList();
			return View(guilds);
		}
	}
}
