using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;
using System.Linq;

namespace Mitternacht.Services.Database.Repositories.Impl
{
    public class VoiceChannelStatsRepository : Repository<VoiceChannelStats>, IVoiceChannelStatsRepository
    {
        public VoiceChannelStatsRepository(DbContext context) : base(context)
        {
        }

        private VoiceChannelStats GetOrCreate(ulong userId)
        {
            var vcs = _set.FirstOrDefault(v => v.UserId == userId);
            if(vcs == null)
            {
                _set.Add(vcs = new VoiceChannelStats
                {
                    UserId = userId,
                    TimeInVoiceChannel = 0
                });
                _context.SaveChanges();
            }
            return vcs;
        }

        public void AddTime(ulong userId, double time)
        {
            var vcs = GetOrCreate(userId);
            vcs.TimeInVoiceChannel += time;
            _set.Update(vcs);
            _context.SaveChanges();
        }

        public bool RemoveTime(ulong userId, double time)
        {
            if (!IsSaved(userId)) return false;
            var vcs = GetOrCreate(userId);
            if (vcs.TimeInVoiceChannel < time) return false;
            vcs.TimeInVoiceChannel -= time;
            _set.Update(vcs);
            _context.SaveChanges();
            return true;
        }

        public void Reset(ulong userId)
        {
            if (IsSaved(userId))
            {
                var vcs = GetOrCreate(userId);
                vcs.TimeInVoiceChannel = 0;
                _set.Update(vcs);
                _context.SaveChanges();
            }
        }

        public bool TryGetTime(ulong userId, out double time)
        {
            time = 0;
            if (!IsSaved(userId)) return false;
            time = GetOrCreate(userId).TimeInVoiceChannel;
            return true;
        }

        public bool IsSaved(ulong userId)
            => _set.FirstOrDefault(v => v.UserId == userId) != null;
    }
}
