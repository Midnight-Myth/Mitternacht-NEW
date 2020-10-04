using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories {
	public interface IBotConfigRepository : IRepository<BotConfig> {
		BotConfig GetOrCreate();
	}
}
