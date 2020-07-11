using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories {
	public interface IVoiceChannelStatsRepository : IRepository<VoiceChannelStats> {
		void AddTime(ulong userId, ulong guildId, double time);
		bool RemoveTime(ulong userId, ulong guildId, double time);
		bool TryGetTime(ulong userId, ulong guildId, out double time);
		void Reset(ulong userId, ulong guildId);
		bool IsSaved(ulong userId, ulong guildId);
	}
}
