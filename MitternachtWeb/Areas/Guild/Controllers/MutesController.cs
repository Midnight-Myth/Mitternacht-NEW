using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Modules.Administration.Services;
using Mitternacht.Services.Impl;
using MitternachtWeb.Areas.Guild.Models;
using MitternachtWeb.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MitternachtWeb.Areas.Guild.Controllers {
	[Authorize]
	[Area("Guild")]
	public class MutesController : GuildBaseController {
		private readonly DbService _db;
		private readonly MuteService _muteService;

		public MutesController(DbService db, MuteService ms) {
			_db = db;
			_muteService = ms;
		}

		public IActionResult Index() {
			if(PermissionReadMutes) {
				using var uow = _db.UnitOfWork;
				var gc = uow.GuildConfigs.For(GuildId, set => set.Include(g => g.MutedUsers).Include(g => g.UnmuteTimers));
				var mutedUsers = gc.MutedUsers;
				var unmuteTimers = gc.UnmuteTimers;
				var mutes = mutedUsers.Select(mu => mu.UserId).Concat(unmuteTimers.Select(ut => ut.UserId)).Distinct().Select(userId => {
					var user = Guild.GetUser(userId);
					var unmuteTimer = unmuteTimers.Where(ut => ut.UserId == userId).OrderBy(ut => ut.UnmuteAt).FirstOrDefault();
					var mutedUser = mutedUsers.Where(mu => mu.UserId == userId).OrderBy(mu => mu.DateAdded).FirstOrDefault();
					var mutedSince = mutedUser != null ? (unmuteTimer != null ? (DateTime?)new []{ mutedUser.DateAdded, unmuteTimer.DateAdded }.Min() : (DateTime?)mutedUser.DateAdded) : (unmuteTimer != null ? (DateTime?)unmuteTimer.DateAdded : null);

					return new Mute {
						DiscordUser = new ModeledDiscordUser {
							UserId    = userId,
							Username  = user?.ToString() ?? uow.UsernameHistory.GetUsernamesDescending(userId).FirstOrDefault()?.ToString() ?? "-",
							AvatarUrl = user?.GetAvatarUrl() ?? user?.GetDefaultAvatarUrl(),
						},
						Muted      = mutedUser != null,
						MutedSince = mutedSince,
						UnmuteAt   = unmuteTimer?.UnmuteAt,
					};
				}).ToList();

				return View(mutes);
			} else {
				return Unauthorized();
			}
		}

		public async Task<IActionResult> Delete(ulong id) {
			if(PermissionWriteMutes) {
				var user = Guild.GetUser(id);

				if(user != null) {
					await _muteService.UnmuteUser(user);
				} else {
					using var uow = _db.UnitOfWork;
					var gc = uow.GuildConfigs.For(GuildId, set => set.Include(g => g.MutedUsers).Include(g => g.UnmuteTimers));
					gc.MutedUsers.RemoveWhere(mu => mu.UserId == id);
					gc.UnmuteTimers.RemoveWhere(ut => ut.UserId == id);
					await uow.SaveChangesAsync();

					_muteService.StopUnmuteTimer(GuildId, id);
				}

				return RedirectToAction("Index");
			} else {
				return Unauthorized();
			}
		}
	}
}
