using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;
using System.Linq;

namespace Mitternacht.Services.Database.Repositories.Impl {
	public class VoiceChannelStatsRepository : Repository<VoiceChannelStats>, IVoiceChannelStatsRepository {
		public VoiceChannelStatsRepository(DbContext context) : base(context) { }

		private VoiceChannelStats GetOrCreate(ulong userId, ulong guildId) {
			var vcs = _set.FirstOrDefault(v => v.UserId == userId && v.GuildId == guildId);

			if(vcs == null) {
				_set.Add(vcs = new VoiceChannelStats {
					UserId             = userId,
					GuildId            = guildId,
					TimeInVoiceChannel = 0,
				});
			}

			return vcs;
		}

		public void AddTime(ulong guildId, ulong userId, double time) {
			GetOrCreate(userId, guildId).TimeInVoiceChannel += time;
		}

		public void Reset(ulong guildId, ulong userId) {
			if(HasTrackedTime(guildId, userId)) {
				var vcs = GetOrCreate(userId, guildId);
				vcs.TimeInVoiceChannel = 0;
			}
		}

		public bool TryGetTime(ulong guildId, ulong userId, out double time) {
			time = 0;
			if(HasTrackedTime(guildId, userId)) {
				time = GetOrCreate(userId, guildId).TimeInVoiceChannel;
				return true;
			} else {
				return false;
			}
		}

		public bool HasTrackedTime(ulong guildId, ulong userId)
			=> _set.Any(v => v.UserId == userId && v.GuildId == guildId);
	}
}
