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

        private VoiceChannelStats GetOrCreate(ulong userId, ulong guildId)
        {
            var vcs = _set.FirstOrDefault(v => v.UserId == userId && v.GuildId == guildId);
            if(vcs == null)
            {
                _set.Add(vcs = new VoiceChannelStats
                {
                    UserId = userId,
                    GuildId = guildId,
                    TimeInVoiceChannel = 0
                });
                _context.SaveChanges();
            }
            return vcs;
        }

        public void AddTime(ulong userId, ulong guildId, double time)
        {
            var vcs = GetOrCreate(userId, guildId);
            vcs.TimeInVoiceChannel += time;
            _set.Update(vcs);
            _context.SaveChanges();
        }

        public bool RemoveTime(ulong userId, ulong guildId, double time)
        {
            if (!IsSaved(userId, guildId)) return false;
            var vcs = GetOrCreate(userId, guildId);
            if (vcs.TimeInVoiceChannel < time) return false;
            vcs.TimeInVoiceChannel -= time;
            _set.Update(vcs);
            _context.SaveChanges();
            return true;
        }

        public void Reset(ulong userId, ulong guildId)
        {
            if (!IsSaved(userId, guildId)) return;
            var vcs = GetOrCreate(userId, guildId);
            vcs.TimeInVoiceChannel = 0;
            _set.Update(vcs);
            _context.SaveChanges();
        }

        public bool TryGetTime(ulong userId, ulong guildId, out double time)
        {
            time = 0;
            if (!IsSaved(userId, guildId)) return false;
            time = GetOrCreate(userId, guildId).TimeInVoiceChannel;
            return true;
        }

        public bool IsSaved(ulong userId, ulong guildId)
            => _set.FirstOrDefault(v => v.UserId == userId && v.GuildId == guildId) != null;
    }
}
