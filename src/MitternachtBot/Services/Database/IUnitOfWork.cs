using System;
using System.Threading.Tasks;
using Mitternacht.Services.Database.Repositories;

namespace Mitternacht.Services.Database
{
    public interface IUnitOfWork : IDisposable
    {
        NadekoContext Context { get; }

        IQuoteRepository Quotes { get; }
        IGuildConfigRepository GuildConfigs { get; }
        IDonatorsRepository Donators { get; }
        IReminderRepository Reminders { get; }
        ISelfAssignedRolesRepository SelfAssignedRoles { get; }
        IBotConfigRepository BotConfig { get; }
        ICustomReactionRepository CustomReactions { get; }
        ICurrencyRepository Currency { get; }
        ICurrencyTransactionsRepository CurrencyTransactions { get; }
        IWarningsRepository Warnings { get; }
        ILevelModelRepository LevelModel { get; }
        IDailyMoneyRepository DailyMoney { get; }
        IRoleMoneyRepository RoleMoney { get; }
        IRoleLevelBindingRepository RoleLevelBinding { get; }
        IMessageXpBlacklist MessageXpBlacklist { get; }
        IVerifiedUserRepository VerifiedUsers { get; }
        IUsernameHistoryRepository UsernameHistory { get; }
        INicknameHistoryRepository NicknameHistory { get; }
        IBirthDateRepository BirthDates { get; }
        IDailyMoneyStatsRepository DailyMoneyStats { get; }
        IVoiceChannelStatsRepository VoiceChannelStats { get; }
        ITeamUpdateRankRepository TeamUpdateRank { get; }

        int Complete();
        Task<int> CompleteAsync();
    }
}
