using System.Globalization;
using Discord;

namespace Mitternacht.Services.Impl {
	public class Localization : ILocalization {
		private readonly DbService _db;
		private readonly IBotConfigProvider _bcp;

		public CultureInfo DefaultCultureInfo {
			get {
				try {
					return new CultureInfo(_bcp.BotConfig.Locale);
				} catch {
					return new CultureInfo(FallbackCulture);
				}
			}
		}

		private static string FallbackCulture => "de-DE";

		public Localization(IBotConfigProvider bcp, DbService db) {
			_db = db;
			_bcp = bcp;
		}

		public void SetGuildCulture(IGuild guild, CultureInfo ci)
			=> SetGuildCulture(guild.Id, ci);

		public void SetGuildCulture(ulong guildId, CultureInfo ci) {
			using var uow = _db.UnitOfWork;
			var gc = uow.GuildConfigs.For(guildId);
			gc.Locale = ci.Name;
			uow.SaveChanges();
		}

		public void RemoveGuildCulture(IGuild guild)
			=> RemoveGuildCulture(guild.Id);

		public void RemoveGuildCulture(ulong guildId) {
			using var uow = _db.UnitOfWork;
			var gc = uow.GuildConfigs.For(guildId);
			gc.Locale = null;
			uow.SaveChanges();
		}

		public void SetDefaultCulture(CultureInfo ci) {
			using var uow = _db.UnitOfWork;
			var bc = uow.BotConfig.GetOrCreate();
			bc.Locale = ci.Name;
			uow.SaveChanges();
			_bcp.Reload();
		}

		public void ResetDefaultCulture()
			=> SetDefaultCulture(CultureInfo.CurrentCulture);

		public CultureInfo GetCultureInfo(IGuild guild)
			=> GetCultureInfo(guild?.Id);

		public CultureInfo GetCultureInfo(ulong? guildId) {
			if(guildId.HasValue) {
				using var uow = _db.UnitOfWork;
				var gc = uow.GuildConfigs.For(guildId.Value);
				try {
					return new CultureInfo(gc.Locale);
				} catch {
					return DefaultCultureInfo;
				}
			} else {
				return DefaultCultureInfo;
			}
		}
	}
}