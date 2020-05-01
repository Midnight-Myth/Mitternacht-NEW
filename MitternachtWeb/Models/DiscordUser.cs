using Discord;
using System.Collections.Generic;

namespace MitternachtWeb.Models {
	public class DiscordUser {
		public IUser                                   User                 { get; set; }
		public BotPagePermission                      BotPagePermissions   { get; set; }
		public Dictionary<ulong, GuildPagePermission> GuildPagePermissions { get; set; }
	}
}
