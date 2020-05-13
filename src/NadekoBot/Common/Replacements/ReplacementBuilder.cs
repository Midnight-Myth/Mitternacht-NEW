using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Extensions;
using Mitternacht.Modules.Administration.Services;

namespace Mitternacht.Common.Replacements
{
    public class ReplacementBuilder
    {
        private static readonly Regex RngRegex = new Regex("%rng(?:(?<from>(?:-)?\\d+)-(?<to>(?:-)?\\d+))?%", RegexOptions.Compiled);
        private readonly ConcurrentDictionary<string, Func<string>> _reps = new ConcurrentDictionary<string, Func<string>>();
        private readonly ConcurrentDictionary<Regex, Func<Match, string>> _regex = new ConcurrentDictionary<Regex, Func<Match, string>>();

        public ReplacementBuilder()
        {
            WithRngRegex();
        }

        public ReplacementBuilder WithDefault(IUser usr, IMessageChannel ch, IGuild g, DiscordSocketClient client)
        {
            return this.WithUser(usr)
                .WithChannel(ch)
                .WithServer(client, g)
                .WithClient(client);
        }

        public ReplacementBuilder WithDefault(ICommandContext ctx) =>
            WithDefault(ctx.User, ctx.Channel, ctx.Guild, (DiscordSocketClient)ctx.Client);

        public ReplacementBuilder WithClient(DiscordSocketClient client)
        {
            _reps.TryAdd("%mention%", () => $"<@{client.CurrentUser.Id}>");
            _reps.TryAdd("%shardid%", () => client.ShardId.ToString());
            _reps.TryAdd("%time%", () => DateTime.Now.ToString("HH:mm " + TimeZoneInfo.Local.StandardName.GetInitials()));
            return this;
        }

        public ReplacementBuilder WithServer(DiscordSocketClient client, IGuild g)
        {

            _reps.TryAdd("%sid%", () => g == null ? "DM" : g.Id.ToString());
            _reps.TryAdd("%server%", () => g == null ? "DM" : g.Name);
            _reps.TryAdd("%server_time%", () =>
            {
                var to = TimeZoneInfo.Local;
                if (g == null)
                    return TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.Utc, to).ToString("HH:mm ") + to.StandardName.GetInitials();
                if (GuildTimezoneService.AllServices.TryGetValue(client.CurrentUser.Id, out var tz))
                    to = tz.GetTimeZoneOrDefault(g.Id) ?? TimeZoneInfo.Local;

                return TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.Utc, to).ToString("HH:mm ") + to.StandardName.GetInitials();
            });
            return this;
        }

        public ReplacementBuilder WithChannel(IMessageChannel ch)
        {
            _reps.TryAdd("%channel%", () => (ch as ITextChannel)?.Mention ?? "#" + ch.Name);
            _reps.TryAdd("%chname%", () => ch.Name);
            _reps.TryAdd("%cid%", () => ch?.Id.ToString());
            return this;
        }

        public ReplacementBuilder WithUser(IUser user)
        {
            _reps.TryAdd("%user%", () => user.Mention);
            _reps.TryAdd("%userfull%", () => Format.Sanitize(user.ToString()));
            _reps.TryAdd("%username%", () => Format.Sanitize(user.Username));
            _reps.TryAdd("%userdiscrim%", () => user.Discriminator);
            _reps.TryAdd("%id%", () => user.Id.ToString());
            _reps.TryAdd("%uid%", () => user.Id.ToString());
            return this;
        }

        public ReplacementBuilder WithStats(DiscordSocketClient c)
        {
            _reps.TryAdd("%servers%", () => c.Guilds.Count.ToString());
            _reps.TryAdd("%users%", () => c.Guilds.Sum(s => s.Users.Count).ToString());
            return this;
        }

        public ReplacementBuilder WithRngRegex()
        {
            var rng = new NadekoRandom();
            _regex.TryAdd(RngRegex, match =>
            {
                int.TryParse(match.Groups["from"].ToString(), out var from);
                int.TryParse(match.Groups["to"].ToString(), out var to);

                if (from == 0 && to == 0)
                    return rng.Next(0, 11).ToString();

                return from >= to ? string.Empty : rng.Next(from, to + 1).ToString();
            });
            return this;
        }

        public ReplacementBuilder WithOverride(string key, Func<string> output)
        {
            _reps.AddOrUpdate(key, output, delegate { return output; });
            return this;
        }

        public Replacer Build()
        {
            return new Replacer(_reps.Select(x => (x.Key, x.Value)).ToArray(), _regex.Select(x => (x.Key, x.Value)).ToArray());
        }
    }
}
