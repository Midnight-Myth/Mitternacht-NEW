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

        public static int GetXpToLevel(int level)
        {
            return (int)(5 * Math.Pow(level, 2) + 50 * level + 100);
        }

        public bool TryAddLevel(ulong userId, int level, bool calculateLevel = true)
        {
            var lm = GetOrCreate(userId);
            return lm.Level + level >= 0 && TryAddXp(userId,
                       GetXpToLevel(lm.Level + level - 1) - GetXpToLevel(lm.Level - 1), calculateLevel);
        }

        public CalculatedLevel CalculateLevel(ulong userId)
        {
            var lm = GetOrCreate(userId);

            var copyOfTotalXp = lm.TotalXP;
            var calculatedLevel = 0;
            var oldLevel = lm.Level;

            while (copyOfTotalXp > 0)
            {
                var xpNeededForNextLevel = GetXpToLevel(calculatedLevel);

                if (copyOfTotalXp > xpNeededForNextLevel)
                {
                    calculatedLevel++;
                    copyOfTotalXp -= xpNeededForNextLevel;
                }
                else
                {
                    lm.CurrentXP = copyOfTotalXp;
                    copyOfTotalXp = 0;
                }
            }
            lm.Level = calculatedLevel;
            _set.Update(lm);

            return new CalculatedLevel(oldLevel, calculatedLevel);
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
