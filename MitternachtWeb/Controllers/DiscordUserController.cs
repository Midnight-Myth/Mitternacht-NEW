using Microsoft.AspNetCore.Mvc;
using MitternachtWeb.Helpers;
using MitternachtWeb.Models;

namespace MitternachtWeb.Controllers {
	public abstract class DiscordUserController : Controller {
		public DiscordUser DiscordUser => UserHelper.GetDiscordUser(User);
	}
}