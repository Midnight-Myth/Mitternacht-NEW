using NadekoBot.Services.Database.Models;
using System;

namespace NadekoBot.Services.Database.Repositories
{
    public interface ILevelModelRepository : IRepository<LevelModel>
    {
        /// <summary>
        /// Gets the LevelModel for a given user.
        /// </summary>
        /// <param name="userId">The user ID of the LevelModel.</param>
        /// <returns>The LevelModel of the given user.</returns>
        LevelModel GetOrCreate(ulong userId);

        /// <summary>
        /// Add XP to a given user.
        /// </summary>
        /// <param name="userId">The user ID of the user receiving the XP.</param>
        /// <param name="xp">A specified amount of XP to add. Can be negative.</param>
        /// <param name="calculateLevel"></param>
        /// <returns>True, if adding was successful</returns>
        bool TryAddXp(ulong userId, int xp, bool calculateLevel = true);

        /// <summary>
        /// Add levels to a given user.
        /// </summary>
        /// <param name="userId">The user ID of the user receiving the levels.</param>
        /// <param name="level">The amount of levels to add.</param>
        /// <param name="calculateLevel"></param>
        /// <returns>True, if adding was successful</returns>
        bool TryAddLevel(ulong userId, int level, bool calculateLevel = true);
        /// <summary>
        /// Calculates the current level and overflow XP for the given user.
        /// </summary>
        /// <param name="userId">The user to calculate the level for.</param>
        /// <returns>The new level.</returns>
        CalculatedLevel CalculateLevel(ulong userId);

        /// <summary>
        /// Decides wether a user can get Message XP or not.
        /// </summary>
        /// <param name="userId">The users ID.</param>
        /// <param name="time">The time to compare with the saved timestamp.</param>
        /// <returns>True, if the user can get Message XP.</returns>
        bool CanGetMessageXp(ulong userId, DateTime time);

        /// <summary>
        /// Replaces the current timestamp with a new one.
        /// </summary>
        /// <param name="userId">The users ID.</param>
        /// <param name="timestamp">The new Timestamp</param>
        /// <returns>True, if the timestamp was successfully replaced.</returns>
        void ReplaceTimestamp(ulong userId, DateTime timestamp);

        /// <summary>
        /// Get the level of a specified user.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <returns>The current amount of levels.</returns>
        int GetLevel(ulong userId);

        /// <summary>
        /// Get the XP of a specified user.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <returns>The current amount of xp.</returns>
        int GetXp(ulong userId);

        /// <summary>
        /// Set the XP of a specified user.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <param name="xp">The new amount of xp.</param>
        /// <param name="calculateLevel"></param>
        void SetXp(ulong userId, int xp, bool calculateLevel = true);
    }

    public class CalculatedLevel
    {
        public int OldLevel { get; }
        public int NewLevel { get; }
        public bool IsNewLevelHigher => OldLevel < NewLevel;
        public bool IsNewLevelLower => OldLevel > NewLevel;

        public CalculatedLevel(int oldLevel, int newLevel)
        {
            OldLevel = oldLevel;
            NewLevel = newLevel;
        }
    }
}
