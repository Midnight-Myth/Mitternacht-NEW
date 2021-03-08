using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories {
	public interface IGuildConfigRepository : IRepository<GuildConfig> {
		GuildConfig For(ulong guildId, Func<DbSet<GuildConfig>, IQueryable<GuildConfig>> includes);
		GuildConfig For(ulong guildId, bool preloaded = false);
		IEnumerable<GuildConfig> GetAllGuildConfigs(List<ulong> availableGuilds, Func<IQueryable<GuildConfig>, IQueryable<GuildConfig>> includes = null);
		IEnumerable<GuildConfig> Permissionsv2ForAll(List<ulong> include);
		GuildConfig GcWithPermissionsv2For(ulong guildId);
	}
}
