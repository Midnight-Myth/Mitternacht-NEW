using Mitternacht.Common;

namespace Mitternacht.Database.Models {
	public class AntiRaidSetting : DbEntity {
		public int              GuildConfigId { get; set; }
		public GuildConfig      GuildConfig   { get; set; }
		public int              UserThreshold { get; set; }
		public int              Seconds       { get; set; }
		public PunishmentAction Action        { get; set; }
	}
}