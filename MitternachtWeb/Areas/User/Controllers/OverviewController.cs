using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MitternachtWeb.Areas.User.Controllers {
	[Authorize]
	[Area("User")]
	public class OverviewController : UserBaseController {
		public IActionResult Index() {
			return View();
		}
	}
}
