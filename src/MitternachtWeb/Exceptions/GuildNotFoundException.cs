using System;

namespace MitternachtWeb.Exceptions {
	public class GuildNotFoundException : Exception {
		public GuildNotFoundException(ulong guildId) : base(guildId.ToString()) { }
	}
}
