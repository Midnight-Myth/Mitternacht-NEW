using System;
using System.Linq;
using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories.Impl {
	public class NicknameHistoryRepository : Repository<NicknameHistoryModel>, INicknameHistoryRepository {
		public NicknameHistoryRepository(MitternachtContext context) : base(context) { }

		public IQueryable<NicknameHistoryModel> GetGuildUserNames(ulong guildId, ulong userId)
			=> _set.AsQueryable().Where(n => n.GuildId == guildId && n.UserId == userId);

		public IQueryable<NicknameHistoryModel> GetUserNames(ulong userId)
			=> _set.AsQueryable().Where(n => n.UserId == userId);

		public bool AddUsername(ulong guildId, ulong userId, string nickname, ushort discriminator) {
			nickname = nickname?.Trim() ?? "";

			var current = GetGuildUserNames(guildId, userId).OrderByDescending(u => u.DateSet).FirstOrDefault();
			if(current != null || !string.IsNullOrWhiteSpace(nickname)) {
				var now = DateTime.UtcNow;

				if(current != null) {
					if(string.IsNullOrWhiteSpace(nickname)) {
						if(!current.DateReplaced.HasValue) {
							current.DateReplaced = now;
							return true;
						} else {
							return false;
						}
					} else {
						if(!string.Equals(current.Name, nickname, StringComparison.Ordinal) || current.DiscordDiscriminator != discriminator || current.DateReplaced.HasValue) {
							if(!current.DateReplaced.HasValue) {
								current.DateReplaced = now;
							}
						} else {
							return false;
						}
					}
				}

				_set.Add(new NicknameHistoryModel {
					UserId               = userId,
					GuildId              = guildId,
					Name                 = nickname,
					DiscordDiscriminator = discriminator,
					DateSet              = now,
				});
				
				return true;
			} else {
				return false;
			}
		}

		public bool CloseNickname(ulong guildId, ulong userId) {
			var current = GetUserNames(userId).OrderByDescending(u => u.DateSet).FirstOrDefault();

			if(current != null && !current.DateReplaced.HasValue) {
				current.DateReplaced = DateTime.UtcNow;
				return true;
			} else {
				return false;
			}
		}
	}
}