using System;
using System.Threading.Tasks;
using Mitternacht.Services.Database.Repositories;

namespace Mitternacht.Services.Database
{
    public interface IUnitOfWork : IDisposable
    {
		MitternachtContext Context { get; }

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
        IMessageXpRestrictionRepository MessageXpRestriction { get; }
        IVerifiedUserRepository VerifiedUsers { get; }
        IUsernameHistoryRepository UsernameHistory { get; }
        INicknameHistoryRepository NicknameHistory { get; }
        IBirthDateRepository BirthDates { get; }
        IDailyMoneyStatsRepository DailyMoneyStats { get; }
        IVoiceChannelStatsRepository VoiceChannelStats { get; }
        ITeamUpdateRankRepository TeamUpdateRank { get; }

        int SaveChanges(bool acceptAllChanges = true);
        Task<int> SaveChangesAsync(bool acceptAllChanges = true);
    }
}
