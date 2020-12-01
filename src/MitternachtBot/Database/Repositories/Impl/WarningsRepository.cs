using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories.Impl {
	public class WarningsRepository : Repository<Warning>, IWarningsRepository {
		public WarningsRepository(DbContext context) : base(context) { }

		public IQueryable<Warning> For(ulong guildId, ulong userId)
			=> _set.AsQueryable().Where(x => x.GuildId == guildId && x.UserId == userId);

		public bool ToggleForgiven(ulong guildId, int warnId, string modName) {
			var warn = _set.FirstOrDefault(w => w.GuildId == guildId && w.Id == warnId);

			if(warn != null) {
				warn.Forgiven = !warn.Forgiven;
				warn.ForgivenBy = modName;

				return true;
			} else {
				return false;
			}
		}

		public async Task ForgiveAll(ulong guildId, ulong userId, string mod) {
			await _set.AsQueryable().Where(x => x.GuildId == guildId && x.UserId == userId)
				.ForEachAsync(x => {
					if(x.Forgiven)
						return;
					x.Forgiven = true;
					x.ForgivenBy = mod;
				})
				.ConfigureAwait(false);
		}

		public IQueryable<Warning> GetForGuild(ulong id)
			=> _set.AsQueryable().Where(x => x.GuildId == id);

		public IQueryable<Warning> GetForUser(ulong userId)
			=> _set.AsQueryable().Where(w => w.UserId == userId);

		public bool ToggleHidden(ulong guildId, int warnId) {
			var warn = _set.FirstOrDefault(w => w.GuildId == guildId && w.Id == warnId);

			return warn.Hidden = !warn.Hidden;
		}
	}
}
