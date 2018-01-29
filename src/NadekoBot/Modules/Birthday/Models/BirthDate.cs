using System;

namespace Mitternacht.Modules.Birthday.Models
{
    public class BirthDate
    {
        public int Day { get; set; }
        public int Month { get; set; }
        public int? Year { get; set; }

        public BirthDate(int day, int month, int? year = null) {
            new DateTime(year ?? 2000, month, day);
            Day = day;
            Month = month;
            Year = year;
        }

        public override string ToString()
            => $"{Day:D2}.{Month:D2}.{(Year.HasValue ? $"{Year.Value:D4}" : string.Empty)}";
    }
}