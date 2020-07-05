using System;

namespace Mitternacht.Services.Database.Models {
	public class LevelModel : DbEntity {
		public ulong    GuildId       { get; set; }
		public ulong    UserId        { get; set; }
		public int      TotalXP       { get; set; }
		public DateTime LastMessageXp { get; set; }

		public int CurrentLevel {
			get {
				var lvl = 1;

				while(TotalXP >= GetXpForLevel(lvl)) {
					lvl++;
				}
				return lvl - 1;
			}
		}

		public int XP => TotalXP - GetXpForLevel(CurrentLevel);

		public static int GetXpToNextLevel(int previous)
			=> (int)(5 * Math.Pow(previous, 2) + 50 * previous + 100);

		public static int GetXpForLevel(int level)
			=> level <= 0 ? 0 : (int)(5 / 3d * Math.Pow(level, 3) + 45 / 2d * Math.Pow(level, 2) + 455 / 6d * level);
	}
}
