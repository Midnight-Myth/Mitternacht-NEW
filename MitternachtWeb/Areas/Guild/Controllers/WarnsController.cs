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
			if(PermissionReadWarns) {
				using var uow = _db.UnitOfWork;
				var warns = uow.Warnings.GetForGuild(GuildId).ToList().Select((Func<Warning, Warn>)(w => {
					var user = Guild.GetUser(w.UserId);

					return new Warn {
						Id         = w.Id,
						UserId     = w.UserId,
						Username   = user?.ToString() ?? uow.UsernameHistory.GetUsernamesDescending(w.UserId).FirstOrDefault()?.ToString() ?? "-",
						AvatarUrl  = user?.GetAvatarUrl() ?? user?.GetDefaultAvatarUrl(),
						Forgiven   = w.Forgiven,
						ForgivenBy = w.ForgivenBy,
						WarnedBy   = w.Moderator,
						WarnedAt   = w.DateAdded,
						Reason     = w.Reason,
					};
				})).ToList();

				return View(warns);
			} else {
				return Unauthorized();
			}
		}

		public IActionResult ToggleForgive(int id) {
			if(PermissionForgiveWarns) {
				using var uow = _db.UnitOfWork;
				var warning = uow.Warnings.Get(id);
				if(uow.Warnings.ToggleForgiven(GuildId, id, DiscordUser.User.ToString())) {
					uow.Complete();

					return RedirectToAction("Index");
				} else {
					return NotFound();
				}
			} else {
				return Unauthorized();
			}
		}
	}
}
