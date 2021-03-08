using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Mitternacht.Common.Collections;

namespace Mitternacht.Modules.Verification.Common {
	public class VerificationKeyManager {
		private static readonly RandomNumberGenerator _random = RandomNumberGenerator.Create();
		private static readonly ConcurrentHashSet<VerificationKey> _verificationKeys = new ConcurrentHashSet<VerificationKey>();

		public static List<VerificationKey> VerificationKeys => _verificationKeys.ToList();

		private static string GenerateKey() {
			var bytes = new byte[8];
			_random.GetBytes(bytes);
			return Convert.ToBase64String(bytes, Base64FormattingOptions.None);
		}

		public static VerificationKey GenerateVerificationKey(ulong guildid, ulong userid, long forumuserid, VerificationKeyScope keyscope) {
			var vkey = new VerificationKey(GenerateKey(), keyscope, forumuserid, userid, guildid);

			_verificationKeys.Add(vkey);
			return vkey;
		}

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
