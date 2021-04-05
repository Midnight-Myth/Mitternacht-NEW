using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mitternacht.Common;
using Mitternacht.Services.Impl;
using MitternachtWeb.Models;
using System;
using System.Linq;

namespace MitternachtWeb.Areas.User.Controllers {
	[Authorize]
	[Area("User")]
	public class WarnsController : UserBaseController {
		private readonly DbService _db;

		public WarnsController(DbService db) {
			_db = db;
		}

		public IActionResult Index() {
			using var uow = _db.UnitOfWork;

			var allWarnings = uow.Warnings.GetForUser(RequestedUserId).OrderByDescending(w => w.DateAdded).ToList();
			var filteredWarnings = DiscordUser.BotPagePermissions.HasFlag(BotLevelPermission.ReadAllWarns) ? allWarnings : allWarnings.Where(w => DiscordUser.GuildPagePermissions.TryGetValue(w.GuildId, out var perm) && perm.HasFlag(GuildLevelPermission.ReadWarns)).ToList();
			var guildsWhereUserCanForgiveWarns = DiscordUser.GuildPagePermissions.Where(kv => kv.Value.HasFlag(GuildLevelPermission.ForgiveWarns)).Select(kv => kv.Key).ToArray();
			var warns = filteredWarnings.Select(w => {
				var user = Program.MitternachtBot.Client.GetGuild(w.GuildId)?.GetUser(RequestedUserId);

				return new Warn {
					Id            = w.Id,
					GuildId       = w.GuildId,
					Guild         = user?.Guild,
					DiscordUser   = new ModeledDiscordUser {
						UserId    = w.UserId,
						Username  = user?.ToString() ?? uow.UsernameHistory.GetUsernamesDescending(w.UserId).FirstOrDefault()?.ToString() ?? "-",
						AvatarUrl = user?.GetAvatarUrl() ?? user?.GetDefaultAvatarUrl(),
					},
					Forgiven      = w.Forgiven,
					ForgivenBy    = w.ForgivenBy,
					WarnedBy      = w.Moderator,
					WarnedAt      = w.DateAdded,
					Reason        = w.Reason,
					CanBeForgiven = guildsWhereUserCanForgiveWarns.Contains(w.GuildId),
					Points        = (ModerationPoints) w,
					Hidden        = w.Hidden,
				};
			}).ToList();



			return View(warns);
		}

		public IActionResult ToggleForgive(int id) {
			using var uow = _db.UnitOfWork;
			var warning = uow.Warnings.Get(id);

			if(warning != null && (DiscordUser.BotPagePermissions.HasFlag(BotLevelPermission.ForgiveAllWarns) || DiscordUser.GuildPagePermissions.TryGetValue(warning.GuildId, out var perm) && perm.HasFlag(GuildLevelPermission.ForgiveWarns))) {
				if(uow.Warnings.ToggleForgiven(warning.GuildId, id, DiscordUser.User.ToString())) {
					uow.SaveChanges();

					return RedirectToAction("Index");
				} else {
					return NotFound();
				}
			} else {
				return Unauthorized();
			}
		}

		public IActionResult ToggleHidden(int id) {
			using var uow = _db.UnitOfWork;
			var warning = uow.Warnings.Get(id);

			if(warning != null && (DiscordUser.BotPagePermissions.HasFlag(BotLevelPermission.ForgiveAllWarns) || DiscordUser.GuildPagePermissions.TryGetValue(warning.GuildId, out var perm) && perm.HasFlag(GuildLevelPermission.ForgiveWarns))) {
				uow.Warnings.ToggleHidden(warning.GuildId, id);
				uow.SaveChanges();

				return RedirectToAction("Index");
			} else {
				return Unauthorized();
			}
		}
	}
}
