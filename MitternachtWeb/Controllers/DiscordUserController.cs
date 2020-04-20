using Discord;
using Microsoft.AspNetCore.Mvc;
using MitternachtWeb.Helpers;

namespace MitternachtWeb.Controllers {
	public abstract class DiscordUserController : Controller {
		public IUser DiscordUser => UserHelper.GetDiscordUser(User);
	}
}