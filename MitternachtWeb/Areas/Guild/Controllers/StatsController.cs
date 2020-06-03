using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MitternachtWeb.Areas.Guild.Controllers {
	[Authorize]
	[Area("Guild")]
	public class StatsController : GuildBaseController {
		public IActionResult Index() {
			return View();
		}
	}
}
