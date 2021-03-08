using System;

namespace Mitternacht.Modules.Birthday.Models {
	public interface IBirthDate {
		int  Day   { get; set; }
		int  Month { get; set; }
		int? Year  { get; set; }

		bool IsBirthday(DateTime date);
		bool IsBirthday(IBirthDate bd);
	}
}