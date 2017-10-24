using System.Threading.Tasks;
using Mitternacht.Services;

namespace Mitternacht.Modules.Help.Services
{
    public class SupportService : INService
    {
        private readonly DbService _db;

        public SupportService(DbService db)
        {
            _db = db;
        }

        public async Task SetSupportChannel(ulong guildId, ulong? channelId)
        {
            using (var uow = _db.UnitOfWork) {
                uow.GuildConfigs.For(guildId, set => set).SupportChannelId = channelId;
                await uow.CompleteAsync();
            }
        }

        public ulong? GetSupportChannelId(ulong guildId)
        {
            using (var uow = _db.UnitOfWork) {
                return uow.GuildConfigs.For(guildId, set => set).SupportChannelId;
            }
        }
    }
}