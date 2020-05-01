using Mitternacht.Common;
using Mitternacht.Services.Database.Models;
using System;

namespace Mitternacht.Services {
	public interface IBotConfigProvider {
		BotConfig BotConfig { get; }
		event Action<BotConfig> BotConfigChanged;

		void Reload();
		bool Edit(BotConfigEditType type, string newValue);
	}
}