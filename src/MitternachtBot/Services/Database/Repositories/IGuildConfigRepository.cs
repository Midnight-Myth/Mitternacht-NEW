using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories
{
    public interface IGuildConfigRepository : IRepository<GuildConfig>
    {
        GuildConfig For(ulong guildId, Func<DbSet<GuildConfig>, IQueryable<GuildConfig>> includes);
        GuildConfig For(ulong guildId, bool preloaded = false);
        IEnumerable<GuildConfig> OldPermissionsForAll();
        IEnumerable<GuildConfig> GetAllGuildConfigs(List<ulong> availableGuilds);
        IEnumerable<FollowedStream> GetAllFollowedStreams(List<ulong> included);
        void SetCleverbotEnabled(ulong id, bool cleverbotEnabled);
        IEnumerable<GuildConfig> Permissionsv2ForAll(List<ulong> include);
        GuildConfig GcWithPermissionsv2For(ulong guildId);
    }
}
