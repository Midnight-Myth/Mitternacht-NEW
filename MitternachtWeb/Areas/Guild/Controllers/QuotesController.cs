using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mitternacht.Services.Impl;
using MitternachtWeb.Areas.Guild.Models;
using MitternachtWeb.Models;
using System.Linq;

namespace MitternachtWeb.Areas.Guild.Controllers {
	[Authorize]
	[Area("Guild")]
	public class QuotesController : GuildBaseController {
		private readonly DbService _db;

		public QuotesController(DbService db) {
			_db = db;
		}

		public IActionResult Index() {
			if(PermissionReadQuotes) {
				using var uow = _db.UnitOfWork;
				var quotes = uow.Quotes.GetAllForGuild(GuildId).OrderByDescending(q => q.DateAdded).ToList().Select(q => {
					var user = Guild.GetUser(q.AuthorId);

					return new Quote {
						Id      = q.Id,
						Author  = new ModeledDiscordUser {
							UserId    = q.AuthorId,
							Username  = user?.ToString() ?? uow.UsernameHistory.GetUsernamesDescending(q.AuthorId).FirstOrDefault()?.ToString() ?? q.AuthorName ?? "-",
							AvatarUrl = user?.GetAvatarUrl() ?? user?.GetDefaultAvatarUrl(),
						},
						Keyword = q.Keyword,
						Text    = q.Text,
						AddedAt = q.DateAdded,
					};
				}).ToList();

				return View(quotes);
			} else {
				return Unauthorized();
			}
		}

		public IActionResult Edit(int id) {
			if(PermissionWriteQuotes) {
				using var uow = _db.UnitOfWork;
				var quote = uow.Quotes.GetAllForGuild(GuildId).FirstOrDefault(q => q.Id == id);

				if(quote is not null) {
					return View(new EditQuote {
						Keyword = quote.Keyword,
						Text = quote.Text,
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
		public IActionResult Edit(int id, EditQuote quote) {
			if(PermissionWriteQuotes) {
				if(ModelState.IsValid) {
					using var uow = _db.UnitOfWork;

					if(uow.Quotes.UpdateQuote(GuildId, id, quote.Keyword, quote.Text)) {
						uow.SaveChanges();

						return RedirectToAction("Index");
					} else {
						return View(quote);
					}
				} else {
					return View(quote);
				}
			} else {
				return Unauthorized();
			}
		}

		public IActionResult Delete(int id) {
			if(PermissionWriteQuotes) {
				using var uow = _db.UnitOfWork;
				var quote = uow.Quotes.Get(id);

				if(quote != null && quote.GuildId == GuildId) {
					uow.Quotes.Remove(quote);
					uow.SaveChanges();

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
