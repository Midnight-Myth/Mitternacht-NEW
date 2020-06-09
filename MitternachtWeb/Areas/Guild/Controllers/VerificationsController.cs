using GommeHDnetForumAPI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mitternacht.Services;
using MitternachtWeb.Areas.Guild.Models;
using System.Linq;

namespace MitternachtWeb.Areas.Guild.Controllers {
	[Authorize]
	[Area("Guild")]
	public class VerificationsController : GuildBaseController {
		private readonly DbService _db;

		public VerificationsController(DbService db) {
			_db = db;
		}
		
		public IActionResult Index() {
			if(PermissionReadVerifications) {
				using var uow = _db.UnitOfWork;
				var verifications = uow.VerifiedUsers.GetVerifiedUsers(GuildId).Select(v => {
					var user = Guild.GetUser(v.UserId);

					return new Verification {
						Username = user?.ToString() ?? uow.UsernameHistory.GetUsernamesDescending(v.UserId).FirstOrDefault()?.ToString() ?? "-",
						AvatarUrl = user?.GetAvatarUrl() ?? user?.GetDefaultAvatarUrl(),
						ForumProfileUrl = $"{ForumPaths.MembersUrl}{v.ForumUserId}",
					};
				}).ToList();

				return View(verifications);
			} else {
				return Unauthorized();
			}
		}
	}
}
