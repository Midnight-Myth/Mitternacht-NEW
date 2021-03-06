﻿using System.Threading.Tasks;
using Mitternacht.Services;
using Mitternacht.Database.Models;
using Mitternacht.Services.Impl;

namespace Mitternacht.Modules.Permissions.Services {
	public class ResetPermissionsService : IMService {
		private readonly PermissionService _perms;
		private readonly GlobalPermissionService _globalPerms;
		private readonly DbService _db;

		public ResetPermissionsService(PermissionService perms, GlobalPermissionService globalPerms, DbService db) {
			_perms = perms;
			_globalPerms = globalPerms;
			_db = db;
		}

		public async Task ResetPermissions(ulong guildId) {
			using(var uow = _db.UnitOfWork) {
				var config = uow.GuildConfigs.GcWithPermissionsv2For(guildId);
				config.Permissions = Permission.GetDefaultPermlist;
				await uow.SaveChangesAsync().ConfigureAwait(false);
				_perms.UpdateCache(config);
			}
		}

		public async Task ResetGlobalPermissions() {
			using(var uow = _db.UnitOfWork) {
				var gc = uow.BotConfig.GetOrCreate();
				gc.BlockedCommands.Clear();
				gc.BlockedModules.Clear();

				_globalPerms.BlockedCommands.Clear();
				_globalPerms.BlockedModules.Clear();
				await uow.SaveChangesAsync().ConfigureAwait(false);
			}
		}
	}
}
