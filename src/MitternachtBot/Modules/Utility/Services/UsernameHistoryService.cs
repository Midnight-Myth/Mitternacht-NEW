using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Mitternacht.Services;
using Mitternacht.Services.Impl;
using NLog;

namespace Mitternacht.Modules.Utility.Services {
	public class UsernameHistoryService : IMService {
		private readonly DbService _db;
		private readonly DiscordSocketClient _client;
		private readonly Logger _log;

		public UsernameHistoryService(DiscordSocketClient client, DbService db) {
			_db = db;
			_client = client;
			_log = LogManager.GetCurrentClassLogger();

			_client.UserJoined += UserJoined;
			_client.GuildMemberUpdated += UserUpdated;

			var _ = Task.Run(() => UpdateUsernames());
		}

		public (int Nicks, int Usernames, int Users, TimeSpan Time) UpdateUsernames() {
			var time1 = DateTime.UtcNow;
			var users = _client.Guilds.Select(g => g.Users).Aggregate((u, g) => u.Concat(g).ToArray());
			var usernicks = users.GroupBy(u => u.Id, u => new {
				GuildId = u.Guild.Id,
				u.Nickname
			}).ToList();
			var usernames = users.GroupBy(u => u.Id).Select(u => new {
				u.First().Id,
				u.First().Username,
				u.First().DiscriminatorValue
			}).ToList();

			using var uow = _db.UnitOfWork;
			var usernameupdates = usernames.Count(u => uow.UsernameHistory.AddUsername(u.Id, u.Username, u.DiscriminatorValue));
			var nickupdates     = usernicks.Sum(nicks => nicks.Count(a => uow.NicknameHistory.AddUsername(a.GuildId, nicks.Key, a.Nickname, usernames.First(an => an.Id == nicks.Key).DiscriminatorValue)));
			uow.SaveChanges();

			var ts = DateTime.UtcNow - time1;
			_log.Info($"Updated {nickupdates} nicknames and {usernameupdates} usernames for {usernicks.Count} users in {ts.TotalSeconds:F2}s");
			return (nickupdates, usernameupdates, usernicks.Count, ts);
		}

		private Task UserJoined(SocketGuildUser user) {
			var _ = Task.Run(async () => {
				using var uow = _db.UnitOfWork;

				if(IsGuildLoggingUsernames(user.Guild.Id)) {
					uow.NicknameHistory.AddUsername(user.Guild.Id, user.Id, user.Nickname, user.DiscriminatorValue);
				}
					

				if(IsGuildLoggingUsernames()) {
					uow.UsernameHistory.AddUsername(user.Id, user.Username, user.DiscriminatorValue);
				}

				await uow.SaveChangesAsync().ConfigureAwait(false);
			});
			return Task.CompletedTask;
		}

		private Task UserUpdated(SocketGuildUser b, SocketGuildUser a) {
			var _ = Task.Run(async () => {
				if(b.Id != a.Id)
					return;

				using var uow = _db.UnitOfWork;

				if(IsGuildLoggingUsernames(a.Guild.Id)) {
					uow.NicknameHistory.AddUsername(a.Guild.Id, a.Id, a.Nickname, a.DiscriminatorValue);

				}

				if(IsGuildLoggingUsernames()) {
					uow.UsernameHistory.AddUsername(a.Id, a.Username, a.DiscriminatorValue);
				}

				await uow.SaveChangesAsync().ConfigureAwait(false);
			});
			return Task.CompletedTask;
		}

		private bool IsGuildLoggingUsernames(ulong guildId = 0) {
			using var uow = _db.UnitOfWork;

			var globalLogging = uow.BotConfig.GetOrCreate().LogUsernames;
			var guildLogging = guildId != 0 ? (uow.GuildConfigs.For(guildId)?.LogUsernameHistory) : null;

			return guildLogging ?? globalLogging;
		}
	}
}
