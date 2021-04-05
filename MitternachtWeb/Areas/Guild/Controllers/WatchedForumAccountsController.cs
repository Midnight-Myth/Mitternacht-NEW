using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mitternacht.Services.Impl;
using MitternachtWeb.Areas.Guild.Models;
using System.Linq;

namespace MitternachtWeb.Areas.Guild.Controllers {
	[Authorize]
	[Area("Guild")]
	public class WatchedForumAccountsController : GuildBaseController {
		private readonly DbService _db;

		public WatchedForumAccountsController(DbService db) {
			_db = db;
		}

		public IActionResult Index() {
			if(PermissionReadWatchedForumAccounts) {
				using var uow = _db.UnitOfWork;



				var data = (from wfa in uow.WatchedForumAccounts.GetForGuild(GuildId)
							join vu in uow.VerifiedUsers.GetVerifiedUsers(GuildId) on wfa.ForumUserId equals vu.ForumUserId into vus
							from subvu in vus.DefaultIfEmpty()
							select new {
								WatchedForumAccount = wfa,
								VerifiedUser = subvu,
							}).AsEnumerable().Select(o => {
								var user = o.VerifiedUser is not null ? Guild.GetUser(o.VerifiedUser.UserId) : null;

								return new WatchedForumAccount {
									UserId      = o.VerifiedUser?.UserId,
									Username    = user?.ToString() ?? (o.VerifiedUser is not null ? uow.UsernameHistory.GetUsernamesDescending(o.VerifiedUser.UserId).FirstOrDefault()?.ToString() : null) ?? "-",
									AvatarUrl   = user?.GetAvatarUrl() ?? user?.GetDefaultAvatarUrl(),
									ForumUserId = o.WatchedForumAccount.ForumUserId,
									WatchAction = o.WatchedForumAccount.WatchAction,
								};
							}).ToList();

				return View(data);
			} else {
				return Unauthorized();
			}
		}

		public IActionResult Create()
			=> PermissionWriteWatchedForumAccounts ? View() : Unauthorized();

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Create(CreateWatchedForumAccount watchedForumAccount) {
			if(PermissionWriteWatchedForumAccounts) {
				if(ModelState.IsValid) {
					using var uow = _db.UnitOfWork;

					if(uow.WatchedForumAccounts.Create(GuildId, watchedForumAccount.ForumUserId, watchedForumAccount.WatchAction)) {
						uow.SaveChanges();

						return RedirectToAction("Index");
					} else {
						return View(watchedForumAccount);
					}
				} else {
					return View(watchedForumAccount);
				}
			} else {
				return Unauthorized();
			}
		}
	}
}
