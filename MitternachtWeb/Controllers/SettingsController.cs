using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MitternachtWeb.Controllers {
	[Authorize]
	public class SettingsController : Controller {
		public IActionResult Index() {
			return View();
		}
	}
}