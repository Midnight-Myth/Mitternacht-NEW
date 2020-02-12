﻿using System;

namespace Mitternacht.Services.Database.Models
{
    //todo: change names when EF has a workaround for SQLite column renaming
    public class LevelModel : DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public int TotalXP { get; set; }
        public DateTime timestamp { get; set; }


		public int Level {
			get {
				var lvl = 1;

				while(TotalXP >= GetXpForLevel(lvl)) {
					lvl++;
				}
				return lvl - 1;
			}
		}

		public int CurrentXP => TotalXP - GetXpForLevel(Level);

		public static int GetXpToNextLevel(int previous)
			=> (int)(5 * Math.Pow(previous, 2) + 50 * previous + 100);

		public static int GetXpForLevel(int level)
			=> level <= 0 ? 0 : (int)(5 / 3d * Math.Pow(level, 3) + 45 / 2d * Math.Pow(level, 2) + 455 / 6d * level);
	}
}
