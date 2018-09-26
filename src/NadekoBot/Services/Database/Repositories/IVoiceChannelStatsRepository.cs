using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories
{
    public interface IVoiceChannelStatsRepository : IRepository<VoiceChannelStats>
    {
        void AddTime(ulong userId, double time);
        bool RemoveTime(ulong userId, double time);
        bool TryGetTime(ulong userId, out double time);
        void Reset(ulong userId);
        bool IsSaved(ulong userId);
    }
}
