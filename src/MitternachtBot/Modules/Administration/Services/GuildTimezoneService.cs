using System;
using System.Collections.Concurrent;
using Discord.WebSocket;
using Mitternacht.Services;
using Mitternacht.Services.Impl;

namespace Mitternacht.Modules.Administration.Services {
	public class GuildTimezoneService : IMService {
		private readonly DbService _db;

		public static readonly ConcurrentDictionary<ulong, GuildTimezoneService> AllGuildTimezoneServices = new ConcurrentDictionary<ulong, GuildTimezoneService>();

		public GuildTimezoneService(DiscordSocketClient client, DbService db) {
			_db = db;

			AllGuildTimezoneServices.TryAdd(client.CurrentUser.Id, this);
		}

		public TimeZoneInfo GetTimeZoneOrUtc(ulong guildId) {
			using var uow = _db.UnitOfWork;
			var timeZoneId = uow.GuildConfigs.For(guildId).TimeZoneId;

			try {
				return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
			} catch {
				return TimeZoneInfo.Utc;
			}
		}

		public void SetTimeZone(ulong guildId, TimeZoneInfo tz) {
			using var uow = _db.UnitOfWork;
			var gc = uow.GuildConfigs.For(guildId);
			gc.TimeZoneId = tz?.Id;
			uow.SaveChanges();
		}
	}
}
