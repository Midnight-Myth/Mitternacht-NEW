using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories {
	public interface IBotConfigRepository : IRepository<BotConfig> {
		BotConfig GetOrCreate();
	}
}
