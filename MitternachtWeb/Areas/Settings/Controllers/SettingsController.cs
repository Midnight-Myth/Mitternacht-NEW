using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MitternachtWeb.Controllers;

namespace MitternachtWeb.Areas.Settings.Controllers {
	[Authorize]
	[Area("Settings")]
	public class SettingsController : DiscordUserController {
		public IActionResult Index() {
			return View();
		}
	}
}