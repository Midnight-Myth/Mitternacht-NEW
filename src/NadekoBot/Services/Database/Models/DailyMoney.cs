using System;

namespace NadekoBot.Services.Database.Models
{
    public class DailyMoney : DbEntity
    {
        public ulong UserId { get; set; }
        public DateTime LastTimeGotten { get; set; }
    }
}
