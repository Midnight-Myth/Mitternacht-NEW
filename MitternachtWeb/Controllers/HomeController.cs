using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MitternachtWeb.Controllers {
	public class HomeController : DiscordUserController {
		private readonly ILogger<HomeController> _logger;

		public HomeController(ILogger<HomeController> logger) {
			_logger = logger;
		}

		public IActionResult Index() {
			return View();
		}

		[Authorize]
		public IActionResult Login() {
			return RedirectToAction("Index", "Home");
		}

		public async Task<IActionResult> Logout() {
			await HttpContext.SignOutAsync("Cookies");
			return RedirectToAction("Index", "Home");
		}
	}
}
