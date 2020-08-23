using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MitternachtWeb.Models;

namespace MitternachtWeb.Controllers {
	public class ErrorController : Controller {
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Index() {
			var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
			var error = new ErrorViewModel {
				ErrorException = exceptionHandlerPathFeature?.Error,
			};

			return View(error);
		}
	}
}
