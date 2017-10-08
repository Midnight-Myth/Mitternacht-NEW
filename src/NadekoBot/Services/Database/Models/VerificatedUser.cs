namespace NadekoBot.Services.Database.Models
{
    public class VerificatedUser : DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public long ForumUserId { get; set; }
    }
}