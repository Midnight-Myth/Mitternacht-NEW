using Discord.WebSocket;
using Mitternacht.Modules.Utility.Common;
using Mitternacht.Services;
using System.Linq;
using System.Threading.Tasks;

namespace Mitternacht.Modules.Utility.Services
{
    public class VoiceStatsService : IMService
    {
        private readonly DbService _db;
        private readonly DiscordSocketClient _client;
        private Task _writeStats;
        private readonly VoiceStateTimeHelper _timeHelper;

        public VoiceStatsService(DiscordSocketClient client, DbService db)
        {
            _client = client;
            _db = db;

            _timeHelper = new VoiceStateTimeHelper();
            _timeHelper.Reset();

            var guildusers = client.Guilds.SelectMany(g => g.VoiceChannels.SelectMany(svc => svc.Users).Select(sgu => (UserId: sgu.Id, GuildId: g.Id))).ToList();
            foreach ((ulong UserId, ulong GuildId) in guildusers)
            {
                _timeHelper.StartTracking(UserId, GuildId);
            }

            _client.UserVoiceStateUpdated += UserVoiceStateUpdated;
            _client.JoinedGuild += ClientJoinedGuild;
            _client.LeftGuild += ClientLeftGuild;

            _writeStats = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        var usertimes = _timeHelper.GetUserTimes();
                        using (var uow = _db.UnitOfWork)
                        {
                            foreach (var ut in usertimes)
                            {
                                uow.VoiceChannelStats.AddTime(ut.Key.UserId, ut.Key.GuildId, ut.Value);
                            }
                            await uow.CompleteAsync().ConfigureAwait(false);
                        }
                    }
                    catch { /* ignored */ }
                    
                    await Task.Delay(5000);
                }
            });
        }

        private Task UserVoiceStateUpdated(SocketUser user, SocketVoiceState stateo, SocketVoiceState staten)
        {
            if (stateo.VoiceChannel == null && staten.VoiceChannel != null) _timeHelper.StartTracking(user.Id, staten.VoiceChannel.Guild.Id);
            if (stateo.VoiceChannel != null && staten.VoiceChannel == null && !_timeHelper.StopTracking(user.Id, stateo.VoiceChannel.Guild.Id))
                    _timeHelper.EndUserTrackingAfterInterval.Add((user.Id, stateo.VoiceChannel.Guild.Id));

            return Task.CompletedTask;
        }

        private Task ClientJoinedGuild(SocketGuild guild)
        {
            var gus = guild.VoiceChannels.SelectMany(svc => svc.Users).Select(sgu => (UserId: sgu.Id, GuildId: guild.Id)).ToList();
            foreach ((ulong UserId, ulong GuildId) in gus)
            {
                _timeHelper.StartTracking(UserId, GuildId);
            }
            return Task.CompletedTask;
        }

        private Task ClientLeftGuild(SocketGuild guild)
        {
            _timeHelper.StopGuildTracking(guild.Id);
            return Task.CompletedTask;
        }
    }
}
