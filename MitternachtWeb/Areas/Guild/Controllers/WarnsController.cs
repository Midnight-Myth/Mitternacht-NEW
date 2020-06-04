using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;
using MitternachtWeb.Areas.Guild.Models;
using System;
using System.Linq;

namespace MitternachtWeb.Areas.Guild.Controllers {
	[Authorize]
	[Area("Guild")]
	public class WarnsController : GuildBaseController {
		private readonly DbService _db;

		public WarnsController(DbService db) {
			_db = db;
		}

		public IActionResult Index() {
			using var uow = _db.UnitOfWork;
			var warns = uow.Warnings.GetForGuild(GuildId).Select((Func<Warning, Warn>)(w => {
				var user = Guild.GetUser(w.UserId);

				return new Warn {
					UserId     = w.UserId,
					Username   = user?.ToString() ?? uow.UsernameHistory.GetUsernamesDescending(w.UserId).FirstOrDefault()?.ToString() ?? "-",
					AvatarUrl  = user?.GetAvatarUrl(),
					Forgiven   = w.Forgiven,
					ForgivenBy = w.ForgivenBy,
					WarnedBy   = w.Moderator,
					WarnedAt   = w.DateAdded,
					Reason     = w.Reason,
				};
			})).ToList();

			return View(warns);
		}
	}
}
