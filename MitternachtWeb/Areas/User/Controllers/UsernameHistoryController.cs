﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mitternacht.Services.Impl;
using System.Linq;

namespace MitternachtWeb.Areas.User.Controllers {
	[Authorize]
	[Area("User")]
	public class UsernameHistoryController : UserBaseController {
		private readonly DbService _db;

		public UsernameHistoryController(DbService db) {
			_db = db;
		}

		public IActionResult Index()
			=> Usernames();

		public IActionResult Usernames() {
			using var uow = _db.UnitOfWork;

			var usernames = uow.UsernameHistory.GetUsernamesDescending(RequestedUserId).ToList();

			return View("Usernames", usernames);
		}

		public IActionResult Nicknames() {
			using var uow = _db.UnitOfWork;

			var nicknames = uow.NicknameHistory.GetUserNames(RequestedUserId).OrderByDescending(n => n.DateSet).ToList();

			return View("Nicknames", nicknames);
		}
	}
}
