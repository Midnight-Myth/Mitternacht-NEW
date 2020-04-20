using System;

namespace MitternachtWeb.Models {
	[Flags]
	public enum BotPagePermissions {
		None           = 0b00,
		ReadBotConfig  = 0b01,
		WriteBotConfig = 0b11,
	}

	[Flags]
	public enum GuildPagePermissions {
		None             = 0b_0000_0000,
		ReadGuildConfig  = 0b_0000_0001,
		WriteGuildConfig = 0b_0000_0011,
	}
}
