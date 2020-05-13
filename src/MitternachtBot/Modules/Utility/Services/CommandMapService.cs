using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Mitternacht.Common.ModuleBehaviors;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;
using NLog;

namespace Mitternacht.Modules.Utility.Services
{
    public class CommandMapService : IInputTransformer, IMService
    {
        private readonly Logger _log;

        public ConcurrentDictionary<ulong, ConcurrentDictionary<string, string>> AliasMaps { get; }
        //commandmap
        public CommandMapService(IEnumerable<GuildConfig> gcs)
        {
            _log = LogManager.GetCurrentClassLogger();
            AliasMaps = new ConcurrentDictionary<ulong, ConcurrentDictionary<string, string>>(
                gcs.ToDictionary(
                    x => x.GuildId,
                        x => new ConcurrentDictionary<string, string>(x.CommandAliases
                            .Distinct(new CommandAliasEqualityComparer())
                            .ToDictionary(ca => ca.Trigger, ca => ca.Mapping))));
        }

        public async Task<string> TransformInput(IGuild guild, IMessageChannel channel, IUser user, string input, bool realExecution = true)
        {
            await Task.Yield();

            if (guild == null || string.IsNullOrWhiteSpace(input))
                return input;
            
            input = input.ToLowerInvariant();
            if (!AliasMaps.TryGetValue(guild.Id, out var maps)) return input;
            var keys = maps.Keys.OrderByDescending(x => x.Length);

            foreach (var k in keys)
            {
                string newInput;
                if (input.StartsWith(k + " ")) newInput = maps[k] + input.Substring(k.Length, input.Length - k.Length);
                else if (input == k) newInput = maps[k];
                else continue;

                if (!realExecution) return newInput;

                _log.Info($"--Mapping Command--\nGuildId: {guild.Id}\nTrigger: {input}\nMapping: {newInput}");
                try { await channel.SendConfirmAsync($"{input} => {newInput}").ConfigureAwait(false); } catch { /*ignore*/ }
                return newInput;
            }

            return input;
        }
    }

    public class CommandAliasEqualityComparer : IEqualityComparer<CommandAlias>
    {
        public bool Equals(CommandAlias x, CommandAlias y) => x.Trigger == y.Trigger;

        public int GetHashCode(CommandAlias obj) => obj.Trigger.GetHashCode();
    }
}
