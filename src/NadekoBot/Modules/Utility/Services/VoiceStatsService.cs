using Discord.WebSocket;
using Mitternacht.Modules.Utility.Common;
using Mitternacht.Services;
using System.Threading.Tasks;

namespace Mitternacht.Modules.Utility.Services
{
    public class VoiceStatsService : INService
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

            _client.UserVoiceStateUpdated += UserVoiceStateUpdated;

            _writeStats = Task.Run(async () =>
            {
                while (true)
                {
                    var usertimes = _timeHelper.GetUserTimes();
                    using(var uow = _db.UnitOfWork)
                    {
                        foreach (var ut in usertimes)
                        {
                            uow.VoiceChannelStats.AddTime(ut.Key, ut.Value);
                        }
                        await uow.CompleteAsync().ConfigureAwait(false);
                    }
                    await Task.Delay(5000);
                }
            });
        }

        private Task UserVoiceStateUpdated(SocketUser user, SocketVoiceState stateo, SocketVoiceState staten)
        {
            if (stateo.VoiceChannel == null && staten.VoiceChannel != null) _timeHelper.StartTracking(user.Id);
            if (stateo.VoiceChannel != null && staten.VoiceChannel == null && !_timeHelper.StopTracking(user.Id))
                    _timeHelper.EndUserTrackingAfterInterval.Add(user.Id);

            return Task.CompletedTask;
        }
    }
}
