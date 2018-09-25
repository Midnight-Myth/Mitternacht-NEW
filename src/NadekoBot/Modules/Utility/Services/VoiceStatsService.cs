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

            _client.UserVoiceStateUpdated += userVoiceStateUpdated;

            _writeStats = Task.Run(async () =>
            {
                while (true)
                {
                    var usertimes = _timeHelper.GetUserTimes();
                    using(var uow = _db.UnitOfWork)
                    {

                    }
                    await Task.Delay(5000);
                }
            });
        }

        private async Task userVoiceStateUpdated(SocketUser user, SocketVoiceState stateo, SocketVoiceState staten)
        {
            
        }
    }
}
