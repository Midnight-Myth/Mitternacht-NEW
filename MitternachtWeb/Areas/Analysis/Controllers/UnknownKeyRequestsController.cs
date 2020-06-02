using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MitternachtWeb.Areas.Analysis.Services;
using MitternachtWeb.Controllers;
using System.Linq;

namespace MitternachtWeb.Areas.Analysis.Controllers {
	public class UnknownKeyRequestsController : DiscordUserController {
		private readonly UnknownKeyRequestsService _unknownKeyRequestsService;

		public UnknownKeyRequestsController(UnknownKeyRequestsService ukrs) {
			_unknownKeyRequestsService = ukrs;
		}

		[Authorize]
		[Area("Analysis")]
		public IActionResult Index() {
			return View(_unknownKeyRequestsService.UnknownKeyRequests.Select(kv => (kv.Key.moduleName, kv.Key.key, kv.Key.cultureName, kv.Value)));
		}
	}
}
