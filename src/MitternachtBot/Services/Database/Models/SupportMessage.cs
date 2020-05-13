using System;

namespace Mitternacht.Services.Database.Models
{
    public class SupportMessage : DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public ulong EditorId { get; set; }
        public ulong ChannelId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Message { get; set; }
    }
}