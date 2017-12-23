using System;

namespace Mitternacht.Services.Database.Models
{
    //todo: change names when EF has a workaround for SQLite column renaming
    public class LevelModel : DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public int TotalXP { get; set; }
        public DateTime timestamp { get; set; }

        /// <summary>
        /// Deprecated
        /// </summary>
        public int CurrentXP { get; set; }
        /// <summary>
        /// Deprecated
        /// </summary>
        public int Level { get; set; }
    }
}
