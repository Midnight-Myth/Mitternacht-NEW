using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mitternacht.Services;
using MitternachtWeb.Areas.Guild.Models;
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
				var quotes = uow.Quotes.GetAllForGuild(GuildId).ToList().Select(q => {
					var user = Guild.GetUser(q.AuthorId);

					return new Quote {
						Id         = q.Id,
						AuthorId   = q.AuthorId,
						Authorname = user?.ToString() ?? uow.UsernameHistory.GetUsernamesDescending(q.AuthorId).FirstOrDefault()?.ToString() ?? q.AuthorName ?? "-",
						AvatarUrl  = user?.GetAvatarUrl() ?? user?.GetDefaultAvatarUrl(),
						Keyword    = q.Keyword,
						Text       = q.Text,
						AddedAt    = q.DateAdded,
					};
				}).ToList();

				return View(quotes);
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
