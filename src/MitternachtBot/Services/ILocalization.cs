using System.Globalization;
using Discord;

namespace Mitternacht.Services {
	public interface ILocalization : IMService {
		CultureInfo DefaultCultureInfo { get; }

		CultureInfo GetCultureInfo(IGuild guild);
		CultureInfo GetCultureInfo(ulong? guildId);
		void RemoveGuildCulture(IGuild guild);
		void ResetDefaultCulture();
		void SetDefaultCulture(CultureInfo ci);
		void SetGuildCulture(IGuild guild, CultureInfo ci);
	}
}