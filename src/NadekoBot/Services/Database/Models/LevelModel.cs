using System;

namespace NadekoBot.Services.Database.Models
{
    public class LevelModel : DbEntity
    {
        public ulong UserId { get; set; }
        public int Level { get; set; }
        public int TotalXP { get; set; }
        public int CurrentXP { get; set; }
        public DateTime timestamp { get; set; }

        public override bool Equals(object obj)
        {
            return (obj is LevelModel && ((LevelModel)obj).UserId == this.UserId);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
