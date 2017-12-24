using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Mitternacht.Common.Collections;
using Mitternacht.Common.ModuleBehaviors;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;
using NLog;

namespace Mitternacht.Modules.Permissions.Services
{
    public class FilterService : IEarlyBlocker, INService
    {
        private readonly Logger _log;

        public ConcurrentHashSet<ulong> InviteFilteringChannels { get; }
        public ConcurrentHashSet<ulong> InviteFilteringServers { get; }

        //serverid, filteredwords
        public ConcurrentDictionary<ulong, ConcurrentHashSet<string>> ServerFilteredWords { get; }

        public ConcurrentHashSet<ulong> WordFilteringChannels { get; }
        public ConcurrentHashSet<ulong> WordFilteringServers { get; }

        public ConcurrentHashSet<string> FilteredWordsForChannel(ulong channelId, ulong guildId)
        {
            var words = new ConcurrentHashSet<string>();
            if (WordFilteringChannels.Contains(channelId))
                ServerFilteredWords.TryGetValue(guildId, out words);
            return words;
        }

        public ConcurrentHashSet<string> FilteredWordsForServer(ulong guildId)
        {
            var words = new ConcurrentHashSet<string>();
            if (WordFilteringServers.Contains(guildId))
                ServerFilteredWords.TryGetValue(guildId, out words);
            return words;
        }

        public FilterService(DiscordSocketClient client, IEnumerable<GuildConfig> igcs)
        {
            _log = LogManager.GetCurrentClassLogger();
            var gcs = igcs.ToList();
            
            InviteFilteringServers = new ConcurrentHashSet<ulong>(gcs.Where(gc => gc.FilterInvites).Select(gc => gc.GuildId));
            InviteFilteringChannels = new ConcurrentHashSet<ulong>(gcs.SelectMany(gc => gc.FilterInvitesChannelIds.Select(fci => fci.ChannelId)));

            var dict = gcs.ToDictionary(gc => gc.GuildId, gc => new ConcurrentHashSet<string>(gc.FilteredWords.Select(fw => fw.Word)));

            ServerFilteredWords = new ConcurrentDictionary<ulong, ConcurrentHashSet<string>>(dict);

            var serverFiltering = gcs.Where(gc => gc.FilterWords);
            WordFilteringServers = new ConcurrentHashSet<ulong>(serverFiltering.Select(gc => gc.GuildId));

            WordFilteringChannels = new ConcurrentHashSet<ulong>(gcs.SelectMany(gc => gc.FilterWordsChannelIds.Select(fwci => fwci.ChannelId)));

            client.MessageUpdated += (oldData, newMsg, channel) =>
            {
                var _ = Task.Run(() =>
                {
                    var guild = (channel as ITextChannel)?.Guild;

                    if (guild == null || !(newMsg is IUserMessage usrMsg))
                        return Task.CompletedTask;

                    return TryBlockEarly(guild, usrMsg);
                });
                return Task.CompletedTask;
            };
        }

        public async Task<bool> TryBlockEarly(IGuild guild, IUserMessage msg, bool realExecution = true)
            => msg.Author is IGuildUser gu && !gu.GuildPermissions.Administrator && (await FilterInvites(guild, msg, realExecution) || await FilterWords(guild, msg, realExecution));

        public async Task<bool> FilterWords(IGuild guild, IUserMessage usrMsg, bool realExecution = true)
        {
            if (guild is null || usrMsg is null) return false;

            var filteredChannelWords = FilteredWordsForChannel(usrMsg.Channel.Id, guild.Id) ?? new ConcurrentHashSet<string>();
            var filteredServerWords = FilteredWordsForServer(guild.Id) ?? new ConcurrentHashSet<string>();
            var wordsInMessage = usrMsg.Content.ToLowerInvariant().Split(' ');
            if (filteredChannelWords.Count == 0 && filteredServerWords.Count == 0) return false;
            if (!wordsInMessage.Any(word => filteredChannelWords.Contains(word) || filteredServerWords.Contains(word))) return false;
            if (!realExecution) return true;
            try
            {
                await usrMsg.DeleteAsync().ConfigureAwait(false);
            }
            catch (HttpException ex)
            {
                _log.Warn("I do not have permission to filter words in channel with id " + usrMsg.Channel.Id, ex);
            }
            return true;
        }

        public async Task<bool> FilterInvites(IGuild guild, IUserMessage usrMsg, bool realExecution = true)
        {
            if (guild is null || usrMsg is null) return false;

            if (!InviteFilteringChannels.Contains(usrMsg.Channel.Id) && !InviteFilteringServers.Contains(guild.Id) || !usrMsg.Content.IsDiscordInvite()) return false;
            if (!realExecution) return true;
            try
            {
                await usrMsg.DeleteAsync().ConfigureAwait(false);
                return true;
            }
            catch (HttpException ex)
            {
                _log.Warn("I do not have permission to filter invites in channel with id " + usrMsg.Channel.Id, ex);
                return true;
            }
        }
    }
}