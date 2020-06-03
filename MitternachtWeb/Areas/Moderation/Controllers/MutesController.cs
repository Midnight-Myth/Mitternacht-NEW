using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Modules.Administration.Services;
using Mitternacht.Services;
using MitternachtWeb.Areas.Moderation.Models;
using MitternachtWeb.Exceptions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MitternachtWeb.Areas.Moderation.Controllers {
	[Authorize]
	[Area("Moderation")]
	public class MutesController : GuildModerationController {
		private readonly DbService _db;
		private readonly MuteService _muteService;

		public MutesController(DbService db, MuteService ms) {
			_db = db;
			_muteService = ms;
		}
		
		public IActionResult Index() {
			using var uow = _db.UnitOfWork;
			var gc = uow.GuildConfigs.For(GuildId, set => set.Include(g => g.MutedUsers).Include(g => g.UnmuteTimers));
			var mutedUsers = gc.MutedUsers;
			var unmuteTimers = gc.UnmuteTimers;
			var mutes = mutedUsers.Select(mu => mu.UserId).Concat(unmuteTimers.Select(ut => ut.UserId)).Select(userId => new Mute{UserId = userId, Muted = mutedUsers.Any(mu => mu.UserId == userId), UnmuteAt = unmuteTimers.Any() ? (DateTime?)unmuteTimers.Where(ut => ut.UserId == userId).Min(ut => ut.UnmuteAt) : null}).ToList();

			return View(mutes);
		}

		public async Task<IActionResult> Delete(ulong id) {
			if(!PermissionWriteMutes)
				throw new NoPermissionsException();

			var user = Guild.GetUser(id);
			
			if(user != null) {
				await _muteService.UnmuteUser(user);
			} else {
				using var uow = _db.UnitOfWork;
				var gc = uow.GuildConfigs.For(GuildId, set => set.Include(g => g.MutedUsers).Include(g => g.UnmuteTimers));
				gc.MutedUsers.RemoveWhere(mu => mu.UserId == id);
				gc.UnmuteTimers.RemoveWhere(ut => ut.UserId == id);
				await uow.CompleteAsync();

				_muteService.StopUnmuteTimer(GuildId, id);
			}

			return RedirectToAction("Index");
		}
	}
}
