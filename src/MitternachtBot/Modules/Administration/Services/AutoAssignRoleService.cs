using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Mitternacht.Services;
using Mitternacht.Services.Impl;
using NLog;

namespace Mitternacht.Modules.Administration.Services {
	public class AutoAssignRoleService : IMService {
		private readonly DbService _db;
		private readonly Logger _log;

		public AutoAssignRoleService(DiscordSocketClient client, DbService db) {
			_db = db;
			_log = LogManager.GetCurrentClassLogger();

			client.UserJoined += (user) => {
				var _ = Task.Run(async () => {
					try {
						using var uow = _db.UnitOfWork;
						var roleId = uow.GuildConfigs.For(user.Guild.Id).AutoAssignRoleId;
						var role = user.Guild.Roles.FirstOrDefault(r => r.Id == roleId);

						if(role != null && !role.IsEveryone)
							await user.AddRoleAsync(role).ConfigureAwait(false);
					} catch (Exception ex) {
						_log.Warn(ex);
					}
				});
				return Task.CompletedTask;
			};
		}
	}
}
