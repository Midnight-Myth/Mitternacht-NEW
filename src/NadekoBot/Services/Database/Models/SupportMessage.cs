using System;

namespace NadekoBot.Services.Database.Models
{
    public class SupportMessage : DbEntity
    {
        public ulong UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Message { get; set; }
        public bool Editor { get; set; }
    }
}