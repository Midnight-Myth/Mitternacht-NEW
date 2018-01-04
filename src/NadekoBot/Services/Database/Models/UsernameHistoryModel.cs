using System;

namespace Mitternacht.Services.Database.Models
{
    public class UsernameHistoryModel : DbEntity
    {
        public ulong UserId { get; set; }
        public ulong GuildId { get; set; }
        public string Name { get; set; }
        public bool IsNickname { get; set; }
        public DateTime DateSet { get; set; }
        public DateTime? DateReplaced { get; set; }
    }
}
