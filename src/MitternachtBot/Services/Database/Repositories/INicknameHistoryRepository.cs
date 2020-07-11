using System.Linq;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories {
	public interface INicknameHistoryRepository : IRepository<NicknameHistoryModel> {
		IQueryable<NicknameHistoryModel> GetGuildUserNames(ulong guildId, ulong userId);
		IQueryable<NicknameHistoryModel> GetUserNames(ulong userId);
		bool AddUsername(ulong guildId, ulong userId, string nickname, ushort discriminator);
		bool CloseNickname(ulong guildId, ulong userId);
	}
}