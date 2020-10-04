using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories.Impl {
	public class LevelModelRepository : Repository<LevelModel>, ILevelModelRepository {
		private readonly IUnitOfWork _uow;

		public LevelModelRepository(DbContext context, IUnitOfWork uow) : base(context) {
			_uow = uow;
		}

		public static event Func<LevelChangedArgs, Task> LevelChanged = delegate { return Task.CompletedTask; };

		public LevelModel GetOrCreate(ulong guildId, ulong userId) {
			var lm = Get(guildId, userId);

			if(lm == null) {
				_set.Add(lm = new LevelModel {
					GuildId = guildId,
					UserId = userId,
					TotalXP = 0,
					LastMessageXp = DateTime.MinValue,
				});
			}

			return lm;
		}

		public LevelModel Get(ulong guildId, ulong userId)
			=> _set.FirstOrDefault(c => c.GuildId == guildId && c.UserId == userId);

		public void AddXP(ulong guildId, ulong userId, int xp, ulong? channelId = null) {
			var lm = GetOrCreate(guildId, userId);
			
			var oldLevel = lm.Level;
			
			lm.TotalXP = lm.TotalXP + xp >= 0 ? lm.TotalXP + xp : 0;

			if(oldLevel != lm.Level) {
				LevelChanged?.Invoke(new LevelChangedArgs(guildId, userId, oldLevel, lm.Level, channelId));
			}
		}

		public void SetXP(ulong guildId, ulong userId, int xp, ulong? channelId = null) {
			var lm = GetOrCreate(guildId, userId);
			
			var oldLevel = lm.Level;
			lm.TotalXP = xp;

			if(oldLevel != lm.Level) {
				LevelChanged?.Invoke(new LevelChangedArgs(guildId, userId, oldLevel, lm.Level, channelId));
			}
		}

		public void SetLevel(ulong guildId, ulong userId, int level, ulong? channelId = null)
			=> SetXP(guildId, userId, LevelModel.GetXpForLevel(level), channelId);

		public bool CanGetMessageXP(ulong guildId, ulong userId, DateTime time) {
			var lm = Get(guildId, userId);

			return lm == null || (time - lm.LastMessageXp).TotalSeconds >= _uow.GuildConfigs.For(guildId).MessageXpTimeDifference;
		}

		public void ReplaceTimestampOfLastMessageXP(ulong guildId, ulong userId, DateTime timestamp) {
			var lm = Get(guildId, userId);

			if(lm != null) {
				lm.LastMessageXp = timestamp;
			}
		}

		public IOrderedQueryable<LevelModel> ForGuildOrderedByTotalXP(ulong guildId, ulong[] guildUserIds)
			=> _set.AsQueryable().Where(lm => lm.TotalXP != 0 && lm.GuildId == guildId && guildUserIds.Contains(lm.UserId)).OrderByDescending(p => p.TotalXP);
	}
}
