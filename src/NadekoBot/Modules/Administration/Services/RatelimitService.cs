using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Mitternacht.Common.ModuleBehaviors;
using Mitternacht.Extensions;
using Mitternacht.Modules.Administration.Common;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;
using NLog;

namespace Mitternacht.Modules.Administration.Services
{
    public class SlowmodeService : IEarlyBlocker, IMService
    {
        public ConcurrentDictionary<ulong, Ratelimiter> RatelimitingChannels = new ConcurrentDictionary<ulong, Ratelimiter>();
        public ConcurrentDictionary<ulong, HashSet<ulong>> IgnoredRoles;
        public ConcurrentDictionary<ulong, HashSet<ulong>> IgnoredUsers;

        private readonly Logger _log;
        private readonly DiscordSocketClient _client;

        public SlowmodeService(DiscordSocketClient client, IEnumerable<GuildConfig> igcs)
        {
            _log = LogManager.GetCurrentClassLogger();
            _client = client;
            var gcs = igcs.ToList();

            IgnoredRoles = new ConcurrentDictionary<ulong, HashSet<ulong>>(
                gcs.ToDictionary(x => x.GuildId, x => new HashSet<ulong>(x.SlowmodeIgnoredRoles.Select(y => y.RoleId))));

            IgnoredUsers = new ConcurrentDictionary<ulong, HashSet<ulong>>(
                gcs.ToDictionary(x => x.GuildId, x => new HashSet<ulong>(x.SlowmodeIgnoredUsers.Select(y => y.UserId))));
        }

        public async Task<bool> TryBlockEarly(IGuild guild, IUserMessage usrMsg, bool realExecution = true)
        {
            if (guild == null || !realExecution) return false;

            try
            {
                if (!(usrMsg?.Channel is SocketTextChannel channel) || usrMsg.IsAuthor(_client))
                    return false;
                if (!RatelimitingChannels.TryGetValue(channel.Id, out var limiter))
                    return false;

                if (limiter.CheckUserRatelimit(usrMsg.Author.Id, channel.Guild.Id, usrMsg.Author as SocketGuildUser))
                {
                    await usrMsg.DeleteAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
                
            }
            return false;
        }
    }
}
