using System;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Mitternacht.Modules.Administration.Services;

namespace Mitternacht.Common.TypeReaders {
	public class GuildDateTimeTypeReader : TypeReader {
		public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services) {
			var gts = services.GetService<GuildTimezoneService>();

			if(DateTime.TryParse(input, out var dt)) {
				var tz = gts.GetTimeZoneOrUtc(context.Guild.Id);

				return Task.FromResult(TypeReaderResult.FromSuccess(new GuildDateTime(tz, dt)));
			} else {
				return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Input string is in an incorrect format."));
			}
		}
	}

	public class GuildDateTime {
		public TimeZoneInfo Timezone         { get; }
		public DateTime     CurrentGuildTime { get; }
		public DateTime     InputTime        { get; }
		public DateTime     InputTimeUtc     { get; }

		public GuildDateTime(TimeZoneInfo guildTimezone, DateTime inputTime) {
			var now = DateTime.UtcNow;
			
			Timezone         = guildTimezone;
			CurrentGuildTime = TimeZoneInfo.ConvertTime(now, TimeZoneInfo.Utc, Timezone);
			InputTime        = inputTime;
			InputTimeUtc     = TimeZoneInfo.ConvertTime(inputTime, Timezone, TimeZoneInfo.Utc);
		}
	}
}
