using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Mitternacht.Services.Impl;
using MitternachtWeb.Areas.Guild.Models;

namespace MitternachtWeb.Areas.Guild.Controllers {
	[Area("Guild")]
	public class GuildConfigController : GuildBaseController {
		private readonly DbService _db;

		public GuildConfigController(DbService db) {
			_db = db;
		}

		public IActionResult Edit() {
			if(PermissionReadGuildConfig) {
				using var uow = _db.UnitOfWork;

				var guildConfig = uow.GuildConfigs.For(GuildId);
				var editGuildConfig = EditGuildConfig.FromGuildConfig(guildConfig);

				return View(editGuildConfig);
			} else {
				return Unauthorized();
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(EditGuildConfig guildConfig) {
			if(PermissionWriteGuildConfig) {
				if(ModelState.IsValid) {
					using var uow = _db.UnitOfWork;
					var gc        = uow.GuildConfigs.For(GuildId);

					if(gc != null) {
						if(guildConfig.ApplyToGuildConfig(gc)) {
							await uow.SaveChangesAsync();

							return RedirectToAction(nameof(Edit));
						} else {
							ModelState.AddModelError("", "GuildId does not match the one in the path.");

							return View(guildConfig);
						}
					} else {
						return NotFound();
					}
				} else {
					return View(guildConfig);
				}
			} else {
				return Unauthorized();
			}
		}
	}
}
