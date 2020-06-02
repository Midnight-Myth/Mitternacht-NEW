using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MitternachtWeb.Models;

namespace MitternachtWeb.Controllers {
	public class HomeController : DiscordUserController {
		private readonly ILogger<HomeController> _logger;

		public HomeController(ILogger<HomeController> logger) {
			_logger = logger;
		}

		public IActionResult Index() {
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error() {
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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
