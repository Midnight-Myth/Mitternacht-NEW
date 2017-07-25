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

            if(lm == null)
            {
                _set.Add(lm = new LevelModel()
                {
                    UserId = userId,
                    Level = 0,
                    TotalXP = 0,
                    CurrentXP = 0,
                    timestamp = DateTime.MinValue
                });
                _context.SaveChanges();
            }
            return lm;
        }

        public bool TryAddXP(ulong userId, int xp, bool calculateLevel = true)
        {
            var lm = GetOrCreate(userId);
            if (lm.TotalXP + xp < 0) return false;

            lm.TotalXP += xp;
            _set.Update(lm);
            if(calculateLevel) CalculateLevel(userId);
            
            return true;
        }

        public static int GetXPToLevel(int level)
        {
            return (int)(5 * (Math.Pow(level, 2)) + 50 * level + 100);
        }

        public bool TryAddLevel(ulong userId, int level, bool calculateLevel = true)
        {
            var lm = GetOrCreate(userId);
            if (lm.Level + level < 0) return false;

            return TryAddXP(userId, GetXPToLevel(lm.Level + level - 1) - GetXPToLevel(lm.Level - 1), calculateLevel);
        }

        public CalculatedLevel CalculateLevel(ulong userId)
        {
            var lm = GetOrCreate(userId);

            var copyOfTotalXP = lm.TotalXP;
            var calculatedLevel = 0;
            var oldLevel = lm.Level;

            while (copyOfTotalXP > 0)
            {
                var xpNeededForNextLevel = GetXPToLevel(calculatedLevel);

                if (copyOfTotalXP > xpNeededForNextLevel)
                {
                    calculatedLevel++;
                    copyOfTotalXP -= xpNeededForNextLevel;
                }
                else
                {
                    lm.CurrentXP = copyOfTotalXP;
                    copyOfTotalXP = 0;
                }
            }
            lm.Level = calculatedLevel;
            _set.Update(lm);

            return new CalculatedLevel(oldLevel, calculatedLevel);
        }

        public bool CanGetMessageXP(ulong userId, DateTime time)
        {
            var lm = GetOrCreate(userId);
            if ((time - lm.timestamp).TotalSeconds > 60) return true;
            return false;
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
    }
}
