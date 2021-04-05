using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mitternacht.Services.Impl;
using MitternachtWeb.Areas.Guild.Models;
using MitternachtWeb.Models;
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
							orderby wfa.DateAdded descending
							join vu in uow.VerifiedUsers.GetVerifiedUsers(GuildId) on wfa.ForumUserId equals vu.ForumUserId into vus
							from subvu in vus.DefaultIfEmpty()
							select new {
								WatchedForumAccount = wfa,
								VerifiedUser = subvu,
							}).AsEnumerable().Select(o => {
								var user = o.VerifiedUser is not null ? Guild.GetUser(o.VerifiedUser.UserId) : null;

								return new WatchedForumAccount {
									DiscordUser = o.VerifiedUser is null ? null : new ModeledDiscordUser{
										UserId         = o.VerifiedUser.UserId,
										Username       = user?.ToString() ?? uow.UsernameHistory.GetUsernamesDescending(o.VerifiedUser.UserId).FirstOrDefault()?.ToString() ?? "-",
										AvatarUrl      = user?.GetAvatarUrl() ?? user?.GetDefaultAvatarUrl(),
										UserController = "Verifications",
									},
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

		public IActionResult Edit(long id) {
			if(PermissionWriteWatchedForumAccounts) {
				using var uow = _db.UnitOfWork;

				var wfa = uow.WatchedForumAccounts.GetForGuild(GuildId).FirstOrDefault(wfa => wfa.ForumUserId == id);

				if(wfa is not null) {
					return View(new CreateWatchedForumAccount {
						ForumUserId = wfa.ForumUserId,
						WatchAction = wfa.WatchAction,
					});
				} else {
					return NotFound();
				}
			} else {
				return Unauthorized();
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Edit(long id, CreateWatchedForumAccount watchedForumAccount) {
			if(PermissionWriteWatchedForumAccounts) {
				if(ModelState.IsValid) {
					using var uow = _db.UnitOfWork;

					if(uow.WatchedForumAccounts.ChangeWatchAction(GuildId, watchedForumAccount.ForumUserId, watchedForumAccount.WatchAction)) {
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
