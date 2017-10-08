﻿using NadekoBot.Services.Database.Repositories;
using NadekoBot.Services.Database.Repositories.Impl;
using System;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database
{
    public class UnitOfWork : IUnitOfWork
    {
        public NadekoContext _context { get; }

        private IQuoteRepository _quotes;
        public IQuoteRepository Quotes => _quotes ?? (_quotes = new QuoteRepository(_context));

        private IGuildConfigRepository _guildConfigs;
        public IGuildConfigRepository GuildConfigs => _guildConfigs ?? (_guildConfigs = new GuildConfigRepository(_context));

        private IDonatorsRepository _donators;
        public IDonatorsRepository Donators => _donators ?? (_donators = new DonatorsRepository(_context));

        private IClashOfClansRepository _clashOfClans;
        public IClashOfClansRepository ClashOfClans => _clashOfClans ?? (_clashOfClans = new ClashOfClansRepository(_context));

        private IReminderRepository _reminders;
        public IReminderRepository Reminders => _reminders ?? (_reminders = new ReminderRepository(_context));

        private ISelfAssignedRolesRepository _selfAssignedRoles;
        public ISelfAssignedRolesRepository SelfAssignedRoles => _selfAssignedRoles ?? (_selfAssignedRoles = new SelfAssignedRolesRepository(_context));

        private IBotConfigRepository _botConfig;
        public IBotConfigRepository BotConfig => _botConfig ?? (_botConfig = new BotConfigRepository(_context));

        private ICurrencyRepository _currency;
        public ICurrencyRepository Currency => _currency ?? (_currency = new CurrencyRepository(_context));

        private ICurrencyTransactionsRepository _currencyTransactions;
        public ICurrencyTransactionsRepository CurrencyTransactions => _currencyTransactions ?? (_currencyTransactions = new CurrencyTransactionsRepository(_context));

        private IUnitConverterRepository _conUnits;
        public IUnitConverterRepository ConverterUnits => _conUnits ?? (_conUnits = new UnitConverterRepository(_context));

        private IMusicPlaylistRepository _musicPlaylists;
        public IMusicPlaylistRepository MusicPlaylists => _musicPlaylists ?? (_musicPlaylists = new MusicPlaylistRepository(_context));

        private ICustomReactionRepository _customReactions;
        public ICustomReactionRepository CustomReactions => _customReactions ?? (_customReactions = new CustomReactionsRepository(_context));

        private IPokeGameRepository _pokegame;
        public IPokeGameRepository PokeGame => _pokegame ?? (_pokegame = new PokeGameRepository(_context));

        private IWaifuRepository _waifus;
        public IWaifuRepository Waifus => _waifus ?? (_waifus = new WaifuRepository(_context));

        private IDiscordUserRepository _discordUsers;
        public IDiscordUserRepository DiscordUsers => _discordUsers ?? (_discordUsers = new DiscordUserRepository(_context));

        private IWarningsRepository _warnings;
        public IWarningsRepository Warnings => _warnings ?? (_warnings = new WarningsRepository(_context));

        private ILevelModelRepository _levelmodel;
        public ILevelModelRepository LevelModel => _levelmodel ?? (_levelmodel = new LevelModelRepository(_context));

        private IDailyMoneyRepository _dailymoney;
        public IDailyMoneyRepository DailyMoney => _dailymoney ?? (_dailymoney = new DailyMoneyRepository(_context));

        private IRoleMoneyRepository _rolemoney;
        public IRoleMoneyRepository RoleMoney => _rolemoney ?? (_rolemoney = new RoleMoneyRepository(_context));

        private IRoleLevelBindingRepository _rolelevelbinding;
        public IRoleLevelBindingRepository RoleLevelBinding => _rolelevelbinding ?? (_rolelevelbinding = new RoleLevelBindingRepository(_context));

        private IMessageXpBlacklist _messagexpblacklist;
        public IMessageXpBlacklist MessageXpBlacklist => _messagexpblacklist ?? (_messagexpblacklist = new MessageXpBlacklist(_context));

        private IVerificatedUserRepository _verificateduser;
        public IVerificatedUserRepository VerificatedUser => _verificateduser ?? (_verificateduser = new VerificatedUserRepository(_context));

        public UnitOfWork(NadekoContext context)
        {
            _context = context;
        }

        public int Complete() =>
            _context.SaveChanges();

        public Task<int> CompleteAsync() => 
            _context.SaveChangesAsync();

        private bool _disposed;

        protected void Dispose(bool disposing)
        {
            if (!_disposed)
                if (disposing)
                    _context.Dispose();
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
