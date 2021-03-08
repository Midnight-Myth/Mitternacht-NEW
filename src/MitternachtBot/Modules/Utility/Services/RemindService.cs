using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Mitternacht.Common.Replacements;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Database;
using Mitternacht.Database.Models;
using Mitternacht.Services.Impl;
using NLog;

namespace Mitternacht.Modules.Utility.Services {
	public class RemindService : IMService {
		private readonly Logger              _log;
		private readonly CancellationToken   _cancelAllToken;
		private readonly DiscordSocketClient _client;
		private readonly DbService           _db;
		private readonly IBotConfigProvider  _bcp;

		public string RemindMessageFormat => _bcp.BotConfig.RemindMessageFormat;

		public RemindService(DiscordSocketClient client, IBotConfigProvider bcp, DbService db) {
			_log    = LogManager.GetCurrentClassLogger();
			_client = client;
			_db     = db;
			_bcp    = bcp;

			var cancelSource = new CancellationTokenSource();
			_cancelAllToken  = cancelSource.Token;

			using var uow = _db.UnitOfWork;
			var reminders = uow.Reminders.GetRemindersForGuilds(client.Guilds.Select(g => g.Id).ToArray()).ToList();

			foreach(var r in reminders) {
				Task.Run(() => StartReminder(r));
			}
		}

		public async Task StartReminder(Reminder r) {
			var t   = _cancelAllToken;
			var now = DateTime.UtcNow;

			var time = r.When - now;

			if(time.TotalMilliseconds > int.MaxValue)
				return;

			await Task.Delay(time, t).ConfigureAwait(false);
			try {
				IMessageChannel ch;
				if(r.IsPrivate) {
					var user = _client.GetGuild(r.ServerId).GetUser(r.ChannelId);
					if(user == null)
						return;
					ch = await user.GetOrCreateDMChannelAsync().ConfigureAwait(false);
				} else {
					ch = _client.GetGuild(r.ServerId)?.GetTextChannel(r.ChannelId);
				}
				if(ch == null)
					return;

				var rep = new ReplacementBuilder()
					.WithOverride("%user%", () => $"<@!{r.UserId}>")
					.WithOverride("%message%", () => r.Message)
					.WithOverride("%target%", () => r.IsPrivate ? "Direct Message" : $"<#{r.ChannelId}>")
					.Build();

				await ch.SendMessageAsync(rep.Replace(RemindMessageFormat).SanitizeMentions()).ConfigureAwait(false);
			} catch(Exception ex) { _log.Warn(ex); } finally {
				using var uow = _db.UnitOfWork;
				uow.Reminders.Remove(r);
				await uow.SaveChangesAsync();
			}
		}
	}
}