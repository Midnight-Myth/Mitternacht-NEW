using System;
using System.Linq;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories
{
    public interface ILevelModelRepository : IRepository<LevelModel>
    {
        LevelModel GetOrCreate(ulong guildId, ulong userId);

        /// <summary>
        /// Gets the LevelModel for a given user.
        /// </summary>
        /// <param name="guildId">ID of the user's guild</param>
        /// <param name="userId">The user ID of the LevelModel.</param>
        /// <returns>The LevelModel of the given user or null.</returns>
        LevelModel Get(ulong guildId, ulong userId);

        /// <summary>
        /// Add XP to a given user.
        /// </summary>
        /// <param name="guildId"></param>
        /// <param name="userId">The user ID of the user receiving the XP.</param>
        /// <param name="xp">A specified amount of XP to add. Can be negative.</param>
        /// <param name="channelId"></param>
        void AddXp(ulong guildId, ulong userId, int xp, ulong? channelId = null);

        /// <summary>
        /// Set the XP of a specified user.
        /// </summary>
        /// <param name="guildId"></param>
        /// <param name="userId">The id of the user.</param>
        /// <param name="xp">The new amount of xp.</param>
        /// <param name="channelId"></param>
        void SetXp(ulong guildId, ulong userId, int xp, ulong? channelId = null);

        /// <summary>
        /// Set the XP of a specified user.
        /// </summary>
        /// <param name="guildId"></param>
        /// <param name="userId">The id of the user.</param>
        /// <param name="level"></param>
        /// <param name="channelId"></param>
        void SetLevel(ulong guildId, ulong userId, int level, ulong? channelId = null);

        /// <summary>
        /// Decides wether a user can get Message XP or not.
        /// </summary>
        /// <param name="guildId"></param>
        /// <param name="userId">The users ID.</param>
        /// <param name="time">The time to compare with the saved timestamp.</param>
        /// <returns>True, if the user can get Message XP.</returns>
        bool CanGetMessageXp(ulong guildId, ulong userId, DateTime time);

        /// <summary>
        /// Replaces the current timestamp with a new one.
        /// </summary>
        /// <param name="guildId"></param>
        /// <param name="userId">The users ID.</param>
        /// <param name="timestamp">The new Timestamp</param>
        /// <returns>True, if the timestamp was successfully replaced.</returns>
        void ReplaceTimestamp(ulong guildId, ulong userId, DateTime timestamp);
		
		IOrderedQueryable<LevelModel> GetAllSortedForRanks(ulong guildId, ulong[] guildUserIds);
    }

    public class LevelChangedArgs
    {
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

        public enum ChangeTypes
        {
            Up, Down
        }
    }
}
