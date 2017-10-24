using System;

namespace Mitternacht.Services.Database.Models
{
    public class LevelModel : DbEntity
    {
        public ulong UserId { get; set; }
        public int Level { get; set; }
        public int TotalXP { get; set; }
        public int CurrentXP { get; set; }
        public DateTime timestamp { get; set; }
    }
}
