using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Services;
using NLog;

namespace Mitternacht.Modules.Administration.Services {
	public class AdministrationService : IMService {
		private readonly DbService _db;
		private readonly Logger _log;

		public AdministrationService(DbService db, CommandHandler cmdHandler) {
			_db = db;
			_log = LogManager.GetCurrentClassLogger();

			cmdHandler.CommandExecuted += DeleteMessageAfterCommandExecution;
		}

		private Task DeleteMessageAfterCommandExecution(IUserMessage msg, CommandInfo cmd) {
			var _ = Task.Run(async () => {
				try {
					if(msg.Channel is SocketTextChannel channel) {
						using var uow = _db.UnitOfWork;
						if(uow.GuildConfigs.For(channel.Guild.Id).DeleteMessageOnCommand)
							await msg.DeleteAsync().ConfigureAwait(false);
					}
				} catch (Exception ex) {
					_log.Warn("Failed to delete message after command.");
					_log.Warn(ex);
				}
			});
			return Task.CompletedTask;
		}
	}
}
