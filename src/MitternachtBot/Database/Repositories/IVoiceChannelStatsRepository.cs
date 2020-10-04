using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories {
	public interface IVoiceChannelStatsRepository : IRepository<VoiceChannelStats> {
		void AddTime(ulong guildId, ulong userId, double time);
		bool TryGetTime(ulong guildId, ulong userId, out double time);
		void Reset(ulong guildId, ulong userId);
		bool HasTrackedTime(ulong guildId, ulong userId);
	}
}
