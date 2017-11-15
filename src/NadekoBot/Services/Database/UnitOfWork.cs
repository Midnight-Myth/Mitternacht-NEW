using System;
using System.Threading.Tasks;
using Mitternacht.Services.Database.Repositories;
using Mitternacht.Services.Database.Repositories.Impl;

namespace Mitternacht.Services.Database
{
    public class UnitOfWork : IUnitOfWork
    {
        public NadekoContext Context { get; }

        private IQuoteRepository _quotes;
        public IQuoteRepository Quotes => _quotes ?? (_quotes = new QuoteRepository(Context));

        private IGuildConfigRepository _guildConfigs;
        public IGuildConfigRepository GuildConfigs => _guildConfigs ?? (_guildConfigs = new GuildConfigRepository(Context));

        private IDonatorsRepository _donators;
        public IDonatorsRepository Donators => _donators ?? (_donators = new DonatorsRepository(Context));

        private IClashOfClansRepository _clashOfClans;
        public IClashOfClansRepository ClashOfClans => _clashOfClans ?? (_clashOfClans = new ClashOfClansRepository(Context));

        private IReminderRepository _reminders;
        public IReminderRepository Reminders => _reminders ?? (_reminders = new ReminderRepository(Context));

        private ISelfAssignedRolesRepository _selfAssignedRoles;
        public ISelfAssignedRolesRepository SelfAssignedRoles => _selfAssignedRoles ?? (_selfAssignedRoles = new SelfAssignedRolesRepository(Context));

        private IBotConfigRepository _botConfig;
        public IBotConfigRepository BotConfig => _botConfig ?? (_botConfig = new BotConfigRepository(Context));

        private ICurrencyRepository _currency;
        public ICurrencyRepository Currency => _currency ?? (_currency = new CurrencyRepository(Context));

        private ICurrencyTransactionsRepository _currencyTransactions;
        public ICurrencyTransactionsRepository CurrencyTransactions => _currencyTransactions ?? (_currencyTransactions = new CurrencyTransactionsRepository(Context));

        private IUnitConverterRepository _conUnits;
        public IUnitConverterRepository ConverterUnits => _conUnits ?? (_conUnits = new UnitConverterRepository(Context));

        private IMusicPlaylistRepository _musicPlaylists;
        public IMusicPlaylistRepository MusicPlaylists => _musicPlaylists ?? (_musicPlaylists = new MusicPlaylistRepository(Context));

        private ICustomReactionRepository _customReactions;
        public ICustomReactionRepository CustomReactions => _customReactions ?? (_customReactions = new CustomReactionsRepository(Context));

        private IPokeGameRepository _pokegame;
        public IPokeGameRepository PokeGame => _pokegame ?? (_pokegame = new PokeGameRepository(Context));

        private IWaifuRepository _waifus;
        public IWaifuRepository Waifus => _waifus ?? (_waifus = new WaifuRepository(Context));

        private IDiscordUserRepository _discordUsers;
        public IDiscordUserRepository DiscordUsers => _discordUsers ?? (_discordUsers = new DiscordUserRepository(Context));

        private IWarningsRepository _warnings;
        public IWarningsRepository Warnings => _warnings ?? (_warnings = new WarningsRepository(Context));

        private ILevelModelRepository _levelmodel;
        public ILevelModelRepository LevelModel => _levelmodel ?? (_levelmodel = new LevelModelRepository(Context));

        private IDailyMoneyRepository _dailymoney;
        public IDailyMoneyRepository DailyMoney => _dailymoney ?? (_dailymoney = new DailyMoneyRepository(Context));

        private IRoleMoneyRepository _rolemoney;
        public IRoleMoneyRepository RoleMoney => _rolemoney ?? (_rolemoney = new RoleMoneyRepository(Context));

        private IRoleLevelBindingRepository _rolelevelbinding;
        public IRoleLevelBindingRepository RoleLevelBinding => _rolelevelbinding ?? (_rolelevelbinding = new RoleLevelBindingRepository(Context));

        private IMessageXpBlacklist _messagexpblacklist;
        public IMessageXpBlacklist MessageXpBlacklist => _messagexpblacklist ?? (_messagexpblacklist = new MessageXpBlacklist(Context));

        private IVerifiedUserRepository _verifiedusers;
        public IVerifiedUserRepository VerifiedUsers => _verifiedusers ?? (_verifiedusers = new VerifiedUserRepository(Context));

        public UnitOfWork(NadekoContext context)
        {
            Context = context;
        }

        public int Complete() =>
            Context.SaveChanges();

        public Task<int> CompleteAsync() => 
            Context.SaveChangesAsync();

        private bool _disposed;

        protected void Dispose(bool disposing)
        {
            if (!_disposed)
                if (disposing)
                    Context.Dispose();
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
