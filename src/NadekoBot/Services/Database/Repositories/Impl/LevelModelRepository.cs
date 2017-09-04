using NadekoBot.Services.Database.Models;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace NadekoBot.Services.Database.Repositories.Impl
{
    public class LevelModelRepository : Repository<LevelModel>, ILevelModelRepository
    {
        public LevelModelRepository(DbContext context) : base(context)
        {
        }

        public LevelModel GetOrCreate(ulong userId)
        {
            var lm = _set.FirstOrDefault(c => c.UserId == userId);

            if (lm != null) return lm;
            _set.Add(lm = new LevelModel
            {
                UserId = userId,
                Level = 0,
                TotalXP = 0,
                CurrentXP = 0,
                timestamp = DateTime.MinValue
            });
            _context.SaveChanges();
            return lm;
        }

        public bool TryAddXp(ulong userId, int xp, bool calculateLevel = true)
        {
            var lm = GetOrCreate(userId);
            if (lm.TotalXP + xp < 0) xp = -lm.TotalXP;
            lm.TotalXP += xp;
            if(calculateLevel) CalculateLevel(userId);
            return true;
        }

        public static int GetXpToNextLevel(int previous) 
            => (int)(5 * Math.Pow(previous, 2) + 50 * previous + 100);

        public static int GetXpForLevel(int level)
            => level <= 0 ? 0 : (level == 1 ? 100 : (int) (10 / 6d * Math.Pow(level - 1, 3) + 165 / 6d * Math.Pow(level - 1, 2) + 755 / 6d * (level - 1)));

        public bool TryAddLevel(ulong userId, int level, bool calculateLevel = true)
        {
            var lm = GetOrCreate(userId);
            return lm.Level + level >= 0 && TryAddXp(userId,
                       GetXpToNextLevel(lm.Level + level - 1) - GetXpToNextLevel(lm.Level - 1), calculateLevel);
        }

        public CalculatedLevel CalculateLevel(ulong userId)
        {
            var lm = GetOrCreate(userId);

            var oldLevel = lm.Level;
            var lvl = 1;

            while (lm.TotalXP >= GetXpForLevel(lvl)) {
                lvl++;
            }
            lm.Level = lvl - 1;
            lm.CurrentXP = lm.TotalXP - GetXpForLevel(lm.Level);
            _set.Update(lm);

            return new CalculatedLevel(oldLevel, lm.Level);
        }

        public bool CanGetMessageXp(ulong userId, DateTime time)
        {
            var lm = GetOrCreate(userId);
            return (time - lm.timestamp).TotalSeconds > 60;
        }

        public void ReplaceTimestamp(ulong userId, DateTime time)
        {
            var lm = GetOrCreate(userId);
            lm.timestamp = time;
            _set.Update(lm);
        }

        public int GetLevel(ulong userId)
        {
            return GetOrCreate(userId).Level;
        }

        public int GetXp(ulong userId)
        {
            return GetOrCreate(userId).TotalXP;
        }

        public void SetXp(ulong userId, int xp, bool calculateLevel = true)
        {
            GetOrCreate(userId).TotalXP = xp;
            if (calculateLevel) CalculateLevel(userId);
        }
    }
}
