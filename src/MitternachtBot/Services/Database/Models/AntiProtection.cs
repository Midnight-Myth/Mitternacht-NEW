using System.Collections.Generic;

namespace Mitternacht.Services.Database.Models {
	public class AntiRaidSetting : DbEntity {
        public int              GuildConfigId { get; set; }
        public GuildConfig      GuildConfig   { get; set; }
		public int              UserThreshold { get; set; }
		public int              Seconds       { get; set; }
		public PunishmentAction Action        { get; set; }
	}

	public class AntiSpamSetting : DbEntity {
        public int                     GuildConfigId    { get; set; }
        public GuildConfig             GuildConfig      { get; set; }
		public int                     MessageThreshold { get; set; } = 3;
		public int                     MuteTime         { get; set; } = 0;
		public PunishmentAction        Action           { get; set; }
		public HashSet<AntiSpamIgnore> IgnoredChannels  { get; set; } = new HashSet<AntiSpamIgnore>();
	}


	public enum PunishmentAction {
		Mute,
		Kick,
		Ban,
		Softban
	}

	public class AntiSpamIgnore : DbEntity {
		public ulong ChannelId { get; set; }

		public override int GetHashCode()
			=> ChannelId.GetHashCode();

		public override bool Equals(object obj)
			=> obj is AntiSpamIgnore inst && inst.ChannelId == ChannelId;
	}
}