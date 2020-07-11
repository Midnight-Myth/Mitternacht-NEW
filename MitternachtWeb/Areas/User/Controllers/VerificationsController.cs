using GommeHDnetForumAPI.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mitternacht.Modules.Forum.Services;
using Mitternacht.Services;
using System;
using System.Linq;

namespace MitternachtWeb.Areas.User.Controllers {
	[Authorize]
	[Area("User")]
	public class VerificationsController : UserBaseController {
		private readonly DbService _db;
		private readonly ForumService _forum;

		public VerificationsController(DbService db, ForumService forum) {
			_db = db;
			_forum = forum;
		}

		public IActionResult Index() {
			if(_forum.HasForumInstance) {
				using var uow = _db.UnitOfWork;

				var guilds = Program.MitternachtBot.Client.Guilds;
				var guildIds = guilds.Select(g => g.Id).ToArray();

				var verifications = uow.VerifiedUsers.GetVerificationsOfUser(RequestedUserId).Where(v => guildIds.Contains(v.GuildId)).ToList().Select(v => {
					var userInfo = new UserInfo(_forum.Forum, v.ForumUserId);
					var guild = guilds.First(g => g.Id == v.GuildId);
					
					try {
						userInfo.DownloadDataAsync().GetAwaiter().GetResult();
					} catch { }

					return (v, userInfo, guild);
				}).ToList();

				return View(verifications);
			} else {
				return StatusCode(503);
			}
		}
	}
}
