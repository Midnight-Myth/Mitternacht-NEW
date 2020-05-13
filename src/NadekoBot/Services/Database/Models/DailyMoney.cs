using System;

namespace Mitternacht.Services.Database.Models
{
    public class DailyMoney : DbEntity
    {
        public ulong UserId { get; set; }
        public DateTime LastTimeGotten { get; set; }
    }
}
