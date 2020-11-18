using Discord.WebSocket;
using System;

namespace MitternachtWeb.Models {
	public class Warn {
		public int         Id            { get; set; }
		public ulong       GuildId       { get; set; }
		public SocketGuild Guild         { get; set; }
		public ulong       UserId        { get; set; }
		public string      Username      { get; set; }
		public DateTime?   WarnedAt      { get; set; }
		public string      AvatarUrl     { get; set; }
		public string      Reason        { get; set; }
		public bool        Forgiven      { get; set; }
		public string      ForgivenBy    { get; set; }
		public string      WarnedBy      { get; set; }
		public bool        CanBeForgiven { get; set; }
		public long        Points        { get; set; }

		public string WarnedAtString => WarnedAt.HasValue ? WarnedAt.Value.ToString("yyyy-MM-ddTHH:mm:ssZ") : "-";
	}
}
