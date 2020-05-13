using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Discord;
using Mitternacht.Services.Database.Models;
using NLog;

namespace Mitternacht.Services.Impl
{
    public class Localization : ILocalization
    {
        private readonly DbService _db;

        public ConcurrentDictionary<ulong, CultureInfo> GuildCultureInfos { get; }
        public CultureInfo DefaultCultureInfo { get; private set; }
        private static string FallbackCulture => "de-DE";

        public Localization(IBotConfigProvider bcp, IEnumerable<GuildConfig> gcs, DbService db)
        {
            var log = LogManager.GetCurrentClassLogger();

            var cultureInfoNames = gcs.ToDictionary(x => x.GuildId, x => x.Locale);
            var defaultCulture = bcp.BotConfig.Locale;

            _db = db;

            if (string.IsNullOrWhiteSpace(defaultCulture))
                DefaultCultureInfo = new CultureInfo(FallbackCulture);
            else
            {
                try
                {
                    DefaultCultureInfo = new CultureInfo(defaultCulture);
                }
                catch
                {
                    log.Warn($"Unable to load default bot's locale/language. Using {FallbackCulture}.");
                    DefaultCultureInfo = new CultureInfo(FallbackCulture);
                }
            }
            GuildCultureInfos = new ConcurrentDictionary<ulong, CultureInfo>(cultureInfoNames.ToDictionary(x => x.Key, x =>
            {
                CultureInfo cultureInfo = null;
                try
                {
                    if (x.Value == null)
                        return null;
                    cultureInfo = new CultureInfo(x.Value);
                }
                catch { }
                return cultureInfo;
            }).Where(x => x.Value != null));
        }

        public void SetGuildCulture(IGuild guild, CultureInfo ci) =>
            SetGuildCulture(guild.Id, ci);

        public void SetGuildCulture(ulong guildId, CultureInfo ci)
        {
            if (Equals(ci, DefaultCultureInfo))
            {
                RemoveGuildCulture(guildId);
                return;
            }

            using (var uow = _db.UnitOfWork)
            {
                var gc = uow.GuildConfigs.For(guildId, set => set);
                gc.Locale = ci.Name;
                uow.Complete();
            }

            GuildCultureInfos.AddOrUpdate(guildId, ci, (id, old) => ci);
        }

        public void RemoveGuildCulture(IGuild guild) =>
            RemoveGuildCulture(guild.Id);

        public void RemoveGuildCulture(ulong guildId)
        {
            if (!GuildCultureInfos.TryRemove(guildId, out var _)) return;
            using (var uow = _db.UnitOfWork)
            {
                var gc = uow.GuildConfigs.For(guildId, set => set);
                gc.Locale = null;
                uow.Complete();
            }
        }

        public void SetDefaultCulture(CultureInfo ci)
        {
            using (var uow = _db.UnitOfWork)
            {
                var bc = uow.BotConfig.GetOrCreate();
                bc.Locale = ci.Name;
                uow.Complete();
            }
            DefaultCultureInfo = ci;
        }

        public void ResetDefaultCulture() =>
            SetDefaultCulture(CultureInfo.CurrentCulture);

        public CultureInfo GetCultureInfo(IGuild guild) =>
            GetCultureInfo(guild?.Id);

        public CultureInfo GetCultureInfo(ulong? guildId)
        {
            if (guildId == null) return DefaultCultureInfo;
            GuildCultureInfos.TryGetValue(guildId.Value, out var info);
            return info ?? DefaultCultureInfo;
        }
    }
}