using Discord;
using GommeHDnetForumAPI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mitternacht.Services.Impl;
using MitternachtWeb.Areas.Guild.Models;
using MitternachtWeb.Models;
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
				var verifications = uow.VerifiedUsers.GetVerifiedUsers(GuildId).ToList().Select(v => {
					var user = Guild.GetUser(v.UserId);

					return new Verification {
						DiscordUser     = new ModeledDiscordUser {
							UserId         = v.UserId,
							Username       = user?.ToString() ?? uow.UsernameHistory.GetUsernamesDescending(v.UserId).FirstOrDefault()?.ToString() ?? "-",
							AvatarUrl      = user?.GetAvatarUrl() ?? user?.GetDefaultAvatarUrl(),
							UserController = "Verifications",
						},
						ForumProfileUrl = $"{ForumPaths.MembersUrl}{v.ForumUserId}",
						Roles           = user?.Roles.Where(r => !r.IsEveryone).OrderByDescending(r => r.Position).ToArray() ?? new IRole[0],
					};
				}).OrderByDescending(m => m.Roles.FirstOrDefault(r => r.IsHoisted)?.Position ?? 0).ThenBy(m => m.DiscordUser.Username).ToList();

				return View(verifications);
			} else {
				return Unauthorized();
			}
		}

		public IActionResult UnverifiedUsers() {
			if(PermissionReadVerifications) {
				using var uow = _db.UnitOfWork;
				var verificationRoleId = uow.GuildConfigs.For(GuildId).VerifiedRoleId;
				var verificationRole = verificationRoleId.HasValue ? Guild.GetRole(verificationRoleId.Value) : Guild.EveryoneRole;
				var verifiedUsers = uow.VerifiedUsers.GetVerifiedUsers(GuildId).Select(vu => vu.UserId).ToList();

				var unverifiedUsers = verificationRole.Members.Where(gu => !verifiedUsers.Contains(gu.Id)).Select(gu => {
					return new Verification {
						DiscordUser = new ModeledDiscordUser {
							UserId    = gu.Id,
							Username  = gu.ToString(),
							AvatarUrl = gu.GetAvatarUrl() ?? gu.GetDefaultAvatarUrl(),
						},
						Roles       = gu.Roles.Where(r => !r.IsEveryone).OrderByDescending(r => r.Position).ToArray(),
					};
				}).OrderByDescending(m => m.Roles.FirstOrDefault(r => r.IsHoisted)?.Position ?? 0).ThenBy(m => m.DiscordUser.Username).ToList();

				return View(unverifiedUsers);
			} else {
				return Unauthorized();
			}
		}
	}
}
