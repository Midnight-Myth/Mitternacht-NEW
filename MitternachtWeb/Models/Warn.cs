﻿using Discord.WebSocket;
using Mitternacht.Common;
using System;

namespace MitternachtWeb.Models {
	public class Warn {
		public int                Id            { get; set; }
		public ulong              GuildId       { get; set; }
		public SocketGuild        Guild         { get; set; }
		public ModeledDiscordUser DiscordUser   { get; set; }
		public DateTime           WarnedAt      { get; set; }
		public string             Reason        { get; set; }
		public bool               Forgiven      { get; set; }
		public string             ForgivenBy    { get; set; }
		public string             WarnedBy      { get; set; }
		public bool               CanBeForgiven { get; set; }
		public ModerationPoints   Points        { get; set; }
		public bool               Hidden        { get; set; }
	}
}
