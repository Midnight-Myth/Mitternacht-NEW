using Mitternacht.Common;

namespace Mitternacht.Database.Models {
	public class Warning : DbEntity, IModerationPoints {
		public ulong  GuildId      { get; set; }
		public ulong  UserId       { get; set; }
		public string Reason       { get; set; }
		public bool   Forgiven     { get; set; }
		public string ForgivenBy   { get; set; }
		public string Moderator    { get; set; }
		public long   PointsLight  { get; set; }
		public long   PointsMedium { get; set; }
		public long   PointsHard   { get; set; }
		public bool   Hidden       { get; set; }
	}
}
