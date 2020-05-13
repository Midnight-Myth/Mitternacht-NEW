using Microsoft.AspNetCore.Mvc;
using MitternachtWeb.Helpers;
using MitternachtWeb.Models;

namespace MitternachtWeb.Controllers {
	public abstract class DiscordUserController : Controller {
		[ViewData]
		public DiscordUser DiscordUser => UserHelper.GetDiscordUserAsync(User, HttpContext).GetAwaiter().GetResult();
	}
}