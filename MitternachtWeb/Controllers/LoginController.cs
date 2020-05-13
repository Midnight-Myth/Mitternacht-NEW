using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MitternachtWeb.Controllers {
	public class LoginController : DiscordUserController {
		[Authorize]
		public IActionResult Index() {
			return RedirectToAction("Index", "Home");
		}
	}
}