using System;
using System.Linq;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories {
	public interface ILevelModelRepository : IRepository<LevelModel> {
		LevelModel GetOrCreate(ulong guildId, ulong userId);
		LevelModel Get(ulong guildId, ulong userId);
		void AddXP(ulong guildId, ulong userId, int xp, ulong? channelId = null);
		void SetXP(ulong guildId, ulong userId, int xp, ulong? channelId = null);
		void SetLevel(ulong guildId, ulong userId, int level, ulong? channelId = null);
		bool CanGetMessageXP(ulong guildId, ulong userId, DateTime time);
		void ReplaceTimestampOfLastMessageXP(ulong guildId, ulong userId, DateTime timestamp);

		IOrderedQueryable<LevelModel> ForGuildOrderedByTotalXP(ulong guildId, ulong[] guildUserIds);
	}

	public class LevelChangedArgs {
		public ulong GuildId { get; }
		public ulong UserId { get; }
		public int OldLevel { get; }
		public int NewLevel { get; }
		public ulong? ChannelId { get; }
		public ChangeTypes ChangeType => OldLevel < NewLevel ? ChangeTypes.Up : ChangeTypes.Down;

		public LevelChangedArgs(ulong guildId, ulong userId, int oldLevel, int newLevel, ulong? channelId = null) {
			GuildId = guildId;
			UserId = userId;
			OldLevel = oldLevel;
			NewLevel = newLevel;
			ChannelId = channelId;
		}

		public enum ChangeTypes {
			Up, Down
		}
	}
}
