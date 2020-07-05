using System.Collections.Concurrent;
using Discord;
using Mitternacht.Common.Collections;

namespace Mitternacht.Modules.Administration.Common {
	public enum ProtectionType {
		Raiding,
		Spamming,
	}

	public class AntiRaidStats {
		public int UsersCount { get; set; }
		public ConcurrentHashSet<IGuildUser> RaidUsers { get; set; } = new ConcurrentHashSet<IGuildUser>();
	}

	public class AntiSpamStats {
		public ConcurrentDictionary<ulong, UserSpamStats> UserStats { get; set; } = new ConcurrentDictionary<ulong, UserSpamStats>();
	}
}
