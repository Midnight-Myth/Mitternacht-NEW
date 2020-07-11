using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mitternacht.Services;
using MitternachtWeb.Models;
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
			var warns = filteredWarnings.Select(w => {
				var user = Program.MitternachtBot.Client.GetGuild(w.GuildId)?.GetUser(RequestedUserId);

				return new Warn {
					Id         = w.Id,
					GuildId    = w.GuildId,
					Guild      = user?.Guild,
					UserId     = w.UserId,
					Username   = user?.ToString() ?? uow.UsernameHistory.GetUsernamesDescending(w.UserId).FirstOrDefault()?.ToString() ?? "-",
					AvatarUrl  = user?.GetAvatarUrl() ?? user?.GetDefaultAvatarUrl(),
					Forgiven   = w.Forgiven,
					ForgivenBy = w.ForgivenBy,
					WarnedBy   = w.Moderator,
					WarnedAt   = w.DateAdded,
					Reason     = w.Reason,
				};
			}).ToList();

			return View(warns);
		}
	}
}
