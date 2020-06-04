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
			using var uow = _db.UnitOfWork;
			var quotes = uow.Quotes.GetAllForGuild(GuildId).Select(q => {
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
		}
	}
}
