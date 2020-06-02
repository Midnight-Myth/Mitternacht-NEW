using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MitternachtWeb.Controllers;

namespace MitternachtWeb.Areas.Analysis.Controllers {
	public class AnalysisController : DiscordUserController {
		[Authorize]
		[Area("Analysis")]
		public IActionResult Index() {
			return View();
		}
	}
}
