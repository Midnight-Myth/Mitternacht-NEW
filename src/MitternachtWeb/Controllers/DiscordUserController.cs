using Microsoft.AspNetCore.Mvc;
using MitternachtWeb.Helpers;
using MitternachtWeb.Models;
using System.Linq;

namespace MitternachtWeb.Controllers {
	public abstract class DiscordUserController : Controller {
		[ViewData]
		public DiscordUser DiscordUser => UserHelper.GetDiscordUserAsync(User, HttpContext).GetAwaiter().GetResult();

		protected ulong[] ReadableGuilds => (DiscordUser.BotPagePermissions & BotLevelPermission.ReadAllGuilds) != BotLevelPermission.None
			? DiscordUser.GuildPagePermissions.Select(kv => kv.Key).ToArray()
			: DiscordUser.GuildPagePermissions.Where(kv => (kv.Value & GuildLevelPermission.ReadAll) != GuildLevelPermission.None).Select(kv => kv.Key).ToArray();
	}
}