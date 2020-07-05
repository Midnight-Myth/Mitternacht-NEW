using System;
using Mitternacht.Modules.Birthday.Models;

namespace Mitternacht.Services.Database.Models {
	public class BirthDateModel : DbEntity, IBirthDate {
		public ulong UserId                 { get; set; }
		public int   Day                    { get; set; }
		public int   Month                  { get; set; }
		public int?  Year                   { get; set; }
		public bool  BirthdayMessageEnabled { get; set; } = true;

		public BirthDateModel() { }

		public BirthDateModel(ulong userid, int day, int month, int? year = null, bool birthdayMessageEnabled = true) {
			new DateTime(year ?? 2000, month, day);
			UserId = userid;
			Day = day;
			Month = month;
			Year = year;
			BirthdayMessageEnabled = birthdayMessageEnabled;
		}

		public BirthDateModel(ulong userid, IBirthDate bd) {
			UserId = userid;
			Update(bd);
		}

		public void Update(IBirthDate bd) {
			Day = bd.Day;
			Month = bd.Month;
			Year = bd.Year;
		}

		public bool IsBirthday(DateTime date)
			=> date.Day == Day && date.Month == Month;

		public bool IsBirthday(IBirthDate bd)
			=> bd.Day == Day && bd.Month == Month;

		public override string ToString()
			=> $"{Day:D2}.{Month:D2}.{(Year.HasValue ? $"{Year.Value:D4}" : string.Empty)}";
	}
}