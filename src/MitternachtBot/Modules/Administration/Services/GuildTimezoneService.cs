using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Discord.WebSocket;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Administration.Services {
	public class GuildTimezoneService : IMService {
		// todo 70 this is a hack >.<
		public static ConcurrentDictionary<ulong, GuildTimezoneService> AllServices { get; } = new ConcurrentDictionary<ulong, GuildTimezoneService>();
		private ConcurrentDictionary<ulong, TimeZoneInfo> _timezones;
		private readonly DbService _db;

		public GuildTimezoneService(DiscordSocketClient client, IEnumerable<GuildConfig> gcs, DbService db) {
			_timezones = gcs
				.Select(x => {
					TimeZoneInfo tz;
					try {
						tz = x.TimeZoneId == null ? null : TimeZoneInfo.FindSystemTimeZoneById(x.TimeZoneId);
					} catch {
						tz = null;
					}
					return (x.GuildId, tz);
				})
				.Where(x => x.tz != null)
				.ToDictionary(x => x.GuildId, x => x.tz)
				.ToConcurrent();

			var curUser = client.CurrentUser;
			if(curUser != null)
				AllServices.TryAdd(curUser.Id, this);
			_db = db;
		}

		public TimeZoneInfo GetTimeZoneOrDefault(ulong guildId)
			=> _timezones.TryGetValue(guildId, out var tz) ? tz : null;

		public void SetTimeZone(ulong guildId, TimeZoneInfo tz) {
			using var uow = _db.UnitOfWork;
			var gc = uow.GuildConfigs.For(guildId);

			gc.TimeZoneId = tz?.Id;
			uow.Complete();

			if(tz == null)
				_timezones.TryRemove(guildId, out tz);
			else
				_timezones.AddOrUpdate(guildId, tz, (key, old) => tz);
		}

		public TimeZoneInfo GetTimeZoneOrUtc(ulong guildId)
			=> GetTimeZoneOrDefault(guildId) ?? TimeZoneInfo.Utc;
	}
}
