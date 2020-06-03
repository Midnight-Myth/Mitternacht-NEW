using System;

namespace Mitternacht.Services.Database.Models
{
    public class UsernameHistoryModel : DbEntity
    {
        public ulong UserId { get; set; }
        public string Name { get; set; }
        public ushort DiscordDiscriminator { get; set; }
        public DateTime DateSet { get; set; }
        public DateTime? DateReplaced { get; set; }

		public override string ToString()
			=> $"{Name}#{DiscordDiscriminator}";
	}

    public class NicknameHistoryModel : UsernameHistoryModel
    {
        public ulong GuildId { get; set; }
    }
}
