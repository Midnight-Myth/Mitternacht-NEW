using System.Threading.Tasks;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories {
	public interface IWarningsRepository : IRepository<Warning> {
		Warning[] For(ulong guildId, ulong userId);
		Task ForgiveAll(ulong guildId, ulong userId, string mod);
		Warning[] GetForGuild(ulong id);
	}
}
