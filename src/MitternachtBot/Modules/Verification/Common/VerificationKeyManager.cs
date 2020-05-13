using System;
using System.Collections.Generic;
using System.Linq;
using Mitternacht.Common.Collections;

namespace Mitternacht.Modules.Verification.Common {
	public class VerificationKeyManager {
		private static readonly Random _random = new Random();
		private static readonly ConcurrentHashSet<VerificationKey> _verificationKeys = new ConcurrentHashSet<VerificationKey>();

		public static List<VerificationKey> VerificationKeys => _verificationKeys.ToList();

		private static string GenerateKey() {
			var bytes = new byte[8];
			_random.NextBytes(bytes);
			return Convert.ToBase64String(bytes, Base64FormattingOptions.None);
		}

		public static VerificationKey GenerateVerificationKey(ulong guildid, ulong userid, long forumuserid, VerificationKeyScope keyscope) {
			string key;
			while(HasKey(key = GenerateKey())) { }
			var vkey = new VerificationKey(key, keyscope, forumuserid, userid, guildid);

			_verificationKeys.Add(vkey);
			return vkey;
		}

		public static bool HasKey(string key)
			=> _verificationKeys.Any(vkey => vkey.Key.Equals(key, StringComparison.Ordinal));

		public static bool HasVerificationKey(VerificationKey key, bool ignoreKey = false)
			=> _verificationKeys.Any(vkey => vkey.Equals(key, ignoreKey));

		public static VerificationKey GetKey(ulong guildId, ulong userId, long forumUserId, VerificationKeyScope keyScope)
			=> _verificationKeys.FirstOrDefault(vk => vk.IsKeyFor(guildId, userId, forumUserId, keyScope));

		public static string GetKeyString(ulong guildId, ulong userId, long forumUserId, VerificationKeyScope keyScope)
			=> GetKey(guildId, userId, forumUserId, keyScope)?.Key;

		public static bool RemoveKey(ulong guildId, ulong userId, long forumUserId, VerificationKeyScope keyScope) {
			var key = GetKey(guildId, userId, forumUserId, keyScope);
			return key is null ? false : _verificationKeys.TryRemove(key);
		}
	}
}
