using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories.Impl {
	public class BotConfigRepository : Repository<BotConfig>, IBotConfigRepository {
		public BotConfigRepository(DbContext context) : base(context) { }

		public BotConfig GetOrCreate() {
			var config = _set.Include(bc => bc.Blacklist)
				.Include(bc => bc.RotatingStatusMessages)
				.Include(bc => bc.EightBallResponses)
				.Include(bc => bc.StartupCommands)
				.Include(bc => bc.BlockedCommands)
				.Include(bc => bc.BlockedModules)
				.FirstOrDefault();

			if(config == null) {
				_set.Add(config = new BotConfig());
				_context.SaveChanges(false);
			}

			return config;
		}
	}
}
