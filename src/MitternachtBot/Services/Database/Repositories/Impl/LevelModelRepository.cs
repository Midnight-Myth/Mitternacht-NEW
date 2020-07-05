using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories.Impl {
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
                LastMessageXp = DateTime.MinValue
            });

            return lm;
        }

        public LevelModel Get(ulong guildId, ulong userId) 
            => _set.FirstOrDefault(c => c.GuildId == guildId && c.UserId == userId);

        public void AddXP(ulong guildId, ulong userId, int xp, ulong? channelId = null) {
            var lm = GetOrCreate(guildId, userId);
            var oldLevel = lm.CurrentLevel;
            if (lm.TotalXP + xp < 0) xp = -lm.TotalXP;
            lm.TotalXP += xp;
            _set.Update(lm);

            var newLevel = lm.CurrentLevel;
            if(oldLevel != newLevel) LevelChanged?.Invoke(new LevelChangedArgs(guildId, userId, oldLevel, newLevel, channelId));
        }

        public void SetXP(ulong guildId, ulong userId, int xp, ulong? channelId = null) {
            var lm = GetOrCreate(guildId, userId);
            var oldLevel = lm.CurrentLevel;
            lm.TotalXP = xp;
            _set.Update(lm);

            var newLevel = lm.CurrentLevel;
            if (oldLevel != newLevel) LevelChanged?.Invoke(new LevelChangedArgs(guildId, userId, oldLevel, newLevel, channelId));
        }

        public void SetLevel(ulong guildId, ulong userId, int level, ulong? channelId = null) 
            => SetXP(guildId, userId, LevelModel.GetXpForLevel(level));

        public bool CanGetMessageXP(ulong guildId, ulong userId, DateTime time) {
            var lm = Get(guildId, userId);
			return lm == null ? true : (time - lm.LastMessageXp).TotalSeconds >= _uow.GuildConfigs.For(guildId).MessageXpTimeDifference;
		}

		public void ReplaceTimestampOfLastMessageXP(ulong guildId, ulong userId, DateTime timestamp) {
            var lm = Get(guildId, userId);
            if (lm == null) return;
            lm.LastMessageXp = timestamp;
            _set.Update(lm);
        }

		public IOrderedQueryable<LevelModel> GetAllSortedForRanks(ulong guildId, ulong[] guildUserIds)
			=> _set.Where((Expression<Func<LevelModel, bool>>)(lm => lm.TotalXP != 0 && lm.GuildId == guildId && guildUserIds.Contains(lm.UserId))).OrderByDescending(p => p.TotalXP);
    }
}
