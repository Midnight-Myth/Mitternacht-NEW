using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;
using System;
using System.Linq.Expressions;

namespace Mitternacht.Services.Database.Repositories.Impl {
	public class WarningsRepository : Repository<Warning>, IWarningsRepository {
		public WarningsRepository(DbContext context) : base(context) { }

		public IQueryable<Warning> For(ulong guildId, ulong userId)
			=> _set.Where((Expression<Func<Warning, bool>>)(x => x.GuildId == guildId && x.UserId == userId)).OrderByDescending(x => x.DateAdded);

		public async Task ForgiveAll(ulong guildId, ulong userId, string mod) {
			await _set.Where((Expression<Func<Warning, bool>>)(x => x.GuildId == guildId && x.UserId == userId))
				.ForEachAsync(x => {
					if(x.Forgiven)
						return;
					x.Forgiven = true;
					x.ForgivenBy = mod;
				})
				.ConfigureAwait(false);
		}

		public IQueryable<Warning> GetForGuild(ulong id)
			=> _set.Where((Expression<Func<Warning, bool>>)(x => x.GuildId == id)).OrderByDescending(x => x.DateAdded);
	}
}
