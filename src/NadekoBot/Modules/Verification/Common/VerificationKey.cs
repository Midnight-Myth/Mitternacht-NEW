using System;

namespace Mitternacht.Modules.Verification.Common {
	public class VerificationKey {
		public string Key { get; }
		public VerificationKeyScope KeyScope { get; }
		public long ForumUserId { get; }
		public ulong DiscordUserId { get; }
		public ulong GuildId { get; }
		public DateTime CreatedAt { get; }

		public VerificationKey(string key, VerificationKeyScope keyscope, long forumUserId, ulong userId, ulong guildId) {
			Key = key;
			KeyScope = keyscope;
			ForumUserId = forumUserId;
			DiscordUserId = userId;
			GuildId = guildId;
			CreatedAt = DateTime.UtcNow;
		}

		public override bool Equals(object obj)
			=> obj is VerificationKey vk && Equals(vk);

		protected bool Equals(VerificationKey other)
			=> Equals(other, false);

		public bool Equals(VerificationKey other, bool ignoreKey)
			=> (ignoreKey || string.Equals(Key, other.Key))
				&& KeyScope == other.KeyScope
				&& ForumUserId == other.ForumUserId
				&& DiscordUserId == other.DiscordUserId
				&& GuildId == other.GuildId;

		public bool IsKeyFor(ulong guildId, ulong userId, long forumUserId, VerificationKeyScope keyScope)
			=> GuildId == guildId
			&& DiscordUserId == userId
			&& ForumUserId == forumUserId
			&& KeyScope == keyScope;

		public override int GetHashCode()
			=> HashCode.Combine(Key, KeyScope, ForumUserId, DiscordUserId, GuildId);
	}
}
