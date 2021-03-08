using System;

namespace Mitternacht.Modules.Birthday.Models {
	public class BirthDate : IBirthDate {
		public int  Day   { get; set; }
		public int  Month { get; set; }
		public int? Year  { get; set; }

		public static BirthDate Today            => new BirthDate(DateTime.Now.Day, DateTime.Now.Month, DateTime.Now.Year);
		public static BirthDate TodayWithoutYear => new BirthDate(DateTime.Now.Day, DateTime.Now.Month);

		public BirthDate(int day, int month, int? year = null) {
			new DateTime(year ?? 2000, month, day);
			Day   = day;
			Month = month;
			Year  = year;
		}

		public bool IsBirthday(DateTime date)
			=> date.Day == Day && date.Month == Month;

		public bool IsBirthday(IBirthDate bd)
			=> bd.Day == Day && bd.Month == Month;

		public override string ToString()
			=> $"{Day:D2}.{Month:D2}.{(Year.HasValue ? $"{Year.Value:D4}" : string.Empty)}";
	}
}