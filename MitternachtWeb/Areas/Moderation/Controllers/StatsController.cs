using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MitternachtWeb.Areas.Moderation.Controllers {
	[Authorize]
	[Area("Moderation")]
	public class StatsController : GuildModerationController {
		public IActionResult Index() {
			return View();
		}
	}
}
