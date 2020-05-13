using System;
using System.Threading.Tasks;
using Mitternacht.Services.Database.Repositories;
using Mitternacht.Services.Database.Repositories.Impl;

namespace Mitternacht.Services.Database {
	public class UnitOfWork : IUnitOfWork {
		public NadekoContext Context { get; }

		private IQuoteRepository                _quotes;
		public  IQuoteRepository                Quotes => _quotes ??= new QuoteRepository(Context);
		private IGuildConfigRepository          _guildConfigs;
		public  IGuildConfigRepository          GuildConfigs => _guildConfigs ??= new GuildConfigRepository(Context);
		private IDonatorsRepository             _donators;
		public  IDonatorsRepository             Donators => _donators ??= new DonatorsRepository(Context);
		private IClashOfClansRepository         _clashOfClans;
		public  IClashOfClansRepository         ClashOfClans => _clashOfClans ??= new ClashOfClansRepository(Context);
		private IReminderRepository             _reminders;
		public  IReminderRepository             Reminders => _reminders ??= new ReminderRepository(Context);
		private ISelfAssignedRolesRepository    _selfAssignedRoles;
		public  ISelfAssignedRolesRepository    SelfAssignedRoles => _selfAssignedRoles ??= new SelfAssignedRolesRepository(Context);
		private IBotConfigRepository            _botConfig;
		public  IBotConfigRepository            BotConfig => _botConfig ??= new BotConfigRepository(Context);
		private ICurrencyRepository             _currency;
		public  ICurrencyRepository             Currency => _currency ??= new CurrencyRepository(Context);
		private ICurrencyTransactionsRepository _currencyTransactions;
		public  ICurrencyTransactionsRepository CurrencyTransactions => _currencyTransactions ??= new CurrencyTransactionsRepository(Context);
		private IUnitConverterRepository        _conUnits;
		public  IUnitConverterRepository        ConverterUnits => _conUnits ??= new UnitConverterRepository(Context);
		private ICustomReactionRepository       _customReactions;
		public  ICustomReactionRepository       CustomReactions => _customReactions ??= new CustomReactionsRepository(Context);
		private IWaifuRepository                _waifus;
		public  IWaifuRepository                Waifus => _waifus ??= new WaifuRepository(Context);
		private IDiscordUserRepository          _discordUsers;
		public  IDiscordUserRepository          DiscordUsers => _discordUsers ??= new DiscordUserRepository(Context);
		private IWarningsRepository             _warnings;
		public  IWarningsRepository             Warnings => _warnings ??= new WarningsRepository(Context);
		private ILevelModelRepository           _levelmodel;
		public  ILevelModelRepository           LevelModel => _levelmodel ??= new LevelModelRepository(Context, this);
		private IDailyMoneyRepository           _dailymoney;
		public  IDailyMoneyRepository           DailyMoney => _dailymoney ??= new DailyMoneyRepository(Context);
		private IRoleMoneyRepository            _rolemoney;
		public  IRoleMoneyRepository            RoleMoney => _rolemoney ??= new RoleMoneyRepository(Context);
		private IRoleLevelBindingRepository     _rolelevelbinding;
		public  IRoleLevelBindingRepository     RoleLevelBinding => _rolelevelbinding ??= new RoleLevelBindingRepository(Context);
		private IMessageXpBlacklist             _messagexpblacklist;
		public  IMessageXpBlacklist             MessageXpBlacklist => _messagexpblacklist ??= new MessageXpBlacklist(Context);
		private IVerifiedUserRepository         _verifiedusers;
		public  IVerifiedUserRepository         VerifiedUsers => _verifiedusers ??= new VerifiedUserRepository(Context);
		private IUsernameHistoryRepository      _usernamehistory;
		public  IUsernameHistoryRepository      UsernameHistory => _usernamehistory ??= new UsernameHistoryRepository(Context);
		private INicknameHistoryRepository      _nicknamehistory;
		public  INicknameHistoryRepository      NicknameHistory => _nicknamehistory ??= new NicknameHistoryRepository(Context);
		private IBirthDateRepository            _birthdates;
		public  IBirthDateRepository            BirthDates => _birthdates ??= new BirthDateRepository(Context);
		private IDailyMoneyStatsRepository      _dailyMoneyStats;
		public  IDailyMoneyStatsRepository      DailyMoneyStats => _dailyMoneyStats ??= new DailyMoneyStatsRepository(Context);
		private IVoiceChannelStatsRepository    _voiceChannelStats;
		public  IVoiceChannelStatsRepository    VoiceChannelStats => _voiceChannelStats ??= new VoiceChannelStatsRepository(Context);
		private ITeamUpdateRankRepository       _teamUpdateRank;
		public  ITeamUpdateRankRepository       TeamUpdateRank => _teamUpdateRank ??= new TeamUpdateRankRepository(Context);

		public UnitOfWork(NadekoContext context) {
			Context = context;
		}

		public int Complete()
			=> Context.SaveChanges();

		public Task<int> CompleteAsync()
			=> Context.SaveChangesAsync();

		private bool _disposed;

		protected void Dispose(bool disposing) {
			if(!_disposed && disposing) Context.Dispose();
			_disposed = true;
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
