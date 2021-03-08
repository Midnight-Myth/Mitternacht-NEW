using Discord;
using System.Collections.Generic;

namespace MitternachtWeb.Models {
	public class DiscordUser {
		public IUser                                   User                 { get; set; }
		public BotLevelPermission                      BotPagePermissions   { get; set; }
		public Dictionary<ulong, GuildLevelPermission> GuildPagePermissions { get; set; }
	}
}
