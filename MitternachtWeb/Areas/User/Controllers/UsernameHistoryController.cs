using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mitternacht.Services;
using System.Linq;

namespace MitternachtWeb.Areas.User.Controllers {
	[Authorize]
	[Area("User")]
	public class UsernameHistoryController : UserBaseController {
		private readonly DbService _db;

		public UsernameHistoryController(DbService db) {
			_db = db;
		}

		public IActionResult Index()
			=> Usernames();

		public IActionResult Usernames() {
			using var uow = _db.UnitOfWork;

			var usernames = uow.UsernameHistory.GetUsernamesDescending(RequestedUserId).ToList();

			return View("Usernames", usernames);
		}
	}
}
