using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories.Impl
{
    public class LevelModelRepository : Repository<LevelModel>, ILevelModelRepository
    {
        private readonly IUnitOfWork _uow;

        public LevelModelRepository(DbContext context, IUnitOfWork uow) : base(context) {
            _uow = uow;
        }
        
        public static event Func<LevelChangedArgs, Task> LevelChanged = delegate { return Task.CompletedTask; };

        public LevelModel GetOrCreate(ulong guildId, ulong userId) {
            var lm = Get(guildId, userId);

            if (lm != null) return lm;
            _set.Add(lm = new LevelModel {
                GuildId = guildId,
                UserId = userId,
                TotalXP = 0,
                timestamp = DateTime.MinValue
            });
            _context.SaveChanges();
            return lm;
        }

        public LevelModel Get(ulong guildId, ulong userId) 
            => _set.FirstOrDefault(c => c.GuildId == guildId && c.UserId == userId);

        public void AddXp(ulong guildId, ulong userId, int xp, ulong? channelId = null) {
            var lm = GetOrCreate(guildId, userId);
            var oldLevel = GetLevel(guildId, userId);
            if (lm.TotalXP + xp < 0) xp = -lm.TotalXP;
            lm.TotalXP += xp;
            _set.Update(lm);
            _context.SaveChanges();
            var newLevel = GetLevel(guildId, userId);
            if(oldLevel != newLevel) LevelChanged?.Invoke(new LevelChangedArgs(guildId, userId, oldLevel, newLevel, channelId));
        }

        public void SetXp(ulong guildId, ulong userId, int xp, ulong? channelId = null) {
            var lm = GetOrCreate(guildId, userId);
            var oldLevel = GetLevel(guildId, userId);
            lm.TotalXP = xp;
            _set.Update(lm);
            _context.SaveChanges();
            var newLevel = GetLevel(guildId, userId);
            if (oldLevel != newLevel) LevelChanged?.Invoke(new LevelChangedArgs(guildId, userId, oldLevel, newLevel, channelId));
        }

        public void SetLevel(ulong guildId, ulong userId, int level, ulong? channelId = null) 
            => SetXp(guildId, userId, GetXpForLevel(level));

        public bool CanGetMessageXp(ulong guildId, ulong userId, DateTime time) {
            var lm = Get(guildId, userId);
            if (lm == null) return true;
            return (time - lm.timestamp).TotalSeconds >= _uow.GuildConfigs.For(guildId, set => set).MessageXpTimeDifference;
        }

        public void ReplaceTimestamp(ulong guildId, ulong userId, DateTime timestamp) {
            var lm = Get(guildId, userId);
            if (lm == null) return;
            lm.timestamp = timestamp;
            _set.Update(lm);
            _context.SaveChanges();
        }

        public int GetLevel(ulong guildId, ulong userId) {
            var lm = Get(guildId, userId);
            if (lm == null) return 0;
            var lvl = 1;

            while (lm.TotalXP >= GetXpForLevel(lvl))
            {
                lvl++;
            }
            return lvl - 1;
        }

        public int GetTotalXp(ulong guildId, ulong userId) 
            => Get(guildId, userId)?.TotalXP ?? 0;

        public int GetCurrentXp(ulong guildId, ulong userId) 
            => GetTotalXp(guildId, userId) - GetXpForLevel(GetLevel(guildId, userId));

        
        public static int GetXpToNextLevel(int previous) 
            => (int)(5 * Math.Pow(previous, 2) + 50 * previous + 100);

        public static int GetXpForLevel(int level)
            => level <= 0 ? 0 : (int) (5 / 3d * Math.Pow(level, 3) + 45 / 2d * Math.Pow(level, 2) + 455 / 6d * level);
    }
}
