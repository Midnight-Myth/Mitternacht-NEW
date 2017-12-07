using Mitternacht.Common;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services
{
    public interface IBotConfigProvider
    {
        BotConfig BotConfig { get; }
        void Reload();
        bool Edit(BotConfigEditType type, string newValue);
    }
}