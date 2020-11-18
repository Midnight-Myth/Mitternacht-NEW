using Mitternacht.Common;
using System.Collections.Generic;

namespace Mitternacht.Database.Models {
	public class AntiSpamSetting : DbEntity {
		public int                     GuildConfigId    { get; set; }
		public GuildConfig             GuildConfig      { get; set; }
		public int                     MessageThreshold { get; set; } = 3;
		public int                     MuteTime         { get; set; } = 0;
		public PunishmentAction        Action           { get; set; }
		public HashSet<AntiSpamIgnore> IgnoredChannels  { get; set; } = new HashSet<AntiSpamIgnore>();
	}
}