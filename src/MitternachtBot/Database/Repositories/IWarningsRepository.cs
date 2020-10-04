using System.Linq;
using System.Threading.Tasks;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories {
	public interface IWarningsRepository : IRepository<Warning> {
		IQueryable<Warning> For(ulong guildId, ulong userId);
		Task ForgiveAll(ulong guildId, ulong userId, string mod);
		IQueryable<Warning> GetForGuild(ulong id);
		bool ToggleForgiven(ulong guildId, int warnId, string modName);
		IQueryable<Warning> GetForUser(ulong userId);
	}
}
