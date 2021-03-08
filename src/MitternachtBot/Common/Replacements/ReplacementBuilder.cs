using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Extensions;
using Mitternacht.Modules.Administration.Services;

namespace Mitternacht.Common.Replacements {
	public class ReplacementBuilder {
		private static readonly Regex RngRegex = new Regex("%rng(?:(?<from>(?:-)?\\d+)-(?<to>(?:-)?\\d+))?%", RegexOptions.Compiled);

		private readonly ConcurrentDictionary<string, Func<string>>       _reps  = new ConcurrentDictionary<string, Func<string>>();
		private readonly ConcurrentDictionary<Regex, Func<Match, string>> _regex = new ConcurrentDictionary<Regex, Func<Match, string>>();

		public ReplacementBuilder() {
			WithRngRegex();
		}

		public ReplacementBuilder WithDefault(IUser user, IMessageChannel channel, IGuild guild, DiscordSocketClient client)
			=> WithUser(user)
				.WithChannel(channel)
				.WithServer(client, guild)
				.WithClient(client);

		public ReplacementBuilder WithDefault(ICommandContext context)
			=> WithDefault(context.User, context.Channel, context.Guild, (DiscordSocketClient)context.Client);

		public ReplacementBuilder WithClient(DiscordSocketClient client) {
			_reps.TryAdd("%mention%", () => client.CurrentUser.Mention);
			_reps.TryAdd("%shardid%", () => client.ShardId.ToString());
			_reps.TryAdd("%time%", () => DateTime.Now.ToString($"HH:mm {TimeZoneInfo.Local.StandardName.GetInitials()}"));
			return this;
		}

		public ReplacementBuilder WithServer(DiscordSocketClient client, IGuild guild) {
			_reps.TryAdd("%sid%",         () => guild == null ? "DM" : guild.Id.ToString());
			_reps.TryAdd("%server%",      () => guild == null ? "DM" : guild.Name);
			_reps.TryAdd("%server_time%", () => {
				var timeZone = GuildTimezoneService.AllGuildTimezoneServices.TryGetValue(client.CurrentUser.Id, out var tzs) ? tzs.GetTimeZoneOrUtc(guild.Id) : TimeZoneInfo.Utc;

				return $"{TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.Utc, timeZone):HH:mm} {StringExtensions.GetInitials(timeZone.StandardName)}";
			});
			return this;
		}

		public ReplacementBuilder WithChannel(IMessageChannel channel) {
			_reps.TryAdd("%channel%", () => (channel as ITextChannel)?.Mention ?? "#" + channel.Name);
			_reps.TryAdd("%chname%",  () => channel.Name);
			_reps.TryAdd("%cid%",     () => channel.Id.ToString());
			return this;
		}

		public ReplacementBuilder WithUser(IUser user) {
			_reps.TryAdd("%user%",        () => user.Mention);
			_reps.TryAdd("%userfull%",    () => Format.Sanitize(user.ToString()));
			_reps.TryAdd("%username%",    () => Format.Sanitize(user.Username));
			_reps.TryAdd("%userdiscrim%", () => user.Discriminator);
			_reps.TryAdd("%id%",          () => user.Id.ToString());
			_reps.TryAdd("%uid%",         () => user.Id.ToString());
			return this;
		}

		public ReplacementBuilder WithStats(DiscordSocketClient client) {
			_reps.TryAdd("%servers%", () => client.Guilds.Count.ToString());
			_reps.TryAdd("%users%",   () => client.Guilds.Sum(s => s.Users.Count).ToString());
			return this;
		}

		public ReplacementBuilder WithRngRegex() {
			var rng = new NadekoRandom();
			_regex.TryAdd(RngRegex, match => {
				int.TryParse(match.Groups["from"].ToString(), out var from);
				int.TryParse(match.Groups["to"  ].ToString(), out var to);

				return from == 0 && to == 0 ? rng.Next(0, 11).ToString() : from >= to ? string.Empty : rng.Next(from, to + 1).ToString();
			});
			return this;
		}

		public ReplacementBuilder WithOverride(string key, Func<string> output) {
			_reps.AddOrUpdate(key, output, delegate { return output; });
			return this;
		}

		public Replacer Build()
			=> new Replacer(_reps.Select(x => (x.Key, x.Value)).ToArray(), _regex.Select(x => (x.Key, x.Value)).ToArray());
	}
}
