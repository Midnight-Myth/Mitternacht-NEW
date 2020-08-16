using System;
using System.Threading.Tasks;
using Mitternacht.Services.Database.Repositories;
using Mitternacht.Services.Database.Repositories.Impl;

namespace Mitternacht.Services.Database {
	public class UnitOfWork : IUnitOfWork {
		public MitternachtContext Context { get; }

		private IQuoteRepository                _quotes;
		public  IQuoteRepository                Quotes => _quotes ??= new QuoteRepository(Context);
		private IGuildConfigRepository          _guildConfigs;
		public  IGuildConfigRepository          GuildConfigs => _guildConfigs ??= new GuildConfigRepository(Context);
		private IDonatorsRepository             _donators;
		public  IDonatorsRepository             Donators => _donators ??= new DonatorsRepository(Context);
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
		private ICustomReactionRepository       _customReactions;
		public  ICustomReactionRepository       CustomReactions => _customReactions ??= new CustomReactionsRepository(Context);
		private IWarningsRepository             _warnings;
		public  IWarningsRepository             Warnings => _warnings ??= new WarningsRepository(Context);
		private ILevelModelRepository           _levelModel;
		public  ILevelModelRepository           LevelModel => _levelModel ??= new LevelModelRepository(Context, this);
		private IDailyMoneyRepository           _dailyMoney;
		public  IDailyMoneyRepository           DailyMoney => _dailyMoney ??= new DailyMoneyRepository(Context);
		private IRoleMoneyRepository            _roleMoney;
		public  IRoleMoneyRepository            RoleMoney => _roleMoney ??= new RoleMoneyRepository(Context);
		private IRoleLevelBindingRepository     _roleLevelBindings;
		public  IRoleLevelBindingRepository     RoleLevelBindings => _roleLevelBindings ??= new RoleLevelBindingRepository(Context);
		private IMessageXpRestrictionRepository _messageXpRestrictions;
		public  IMessageXpRestrictionRepository MessageXpRestrictions => _messageXpRestrictions ??= new MessageXpRestrictionRepository(Context);
		private IVerifiedUserRepository         _verifiedUsers;
		public  IVerifiedUserRepository         VerifiedUsers => _verifiedUsers ??= new VerifiedUserRepository(Context);
		private IUsernameHistoryRepository      _usernameHistory;
		public  IUsernameHistoryRepository      UsernameHistory => _usernameHistory ??= new UsernameHistoryRepository(Context);
		private INicknameHistoryRepository      _nicknameHistory;
		public  INicknameHistoryRepository      NicknameHistory => _nicknameHistory ??= new NicknameHistoryRepository(Context);
		private IBirthDateRepository            _birthDates;
		public  IBirthDateRepository            BirthDates => _birthDates ??= new BirthDateRepository(Context);
		private IDailyMoneyStatsRepository      _dailyMoneyStats;
		public  IDailyMoneyStatsRepository      DailyMoneyStats => _dailyMoneyStats ??= new DailyMoneyStatsRepository(Context);
		private IVoiceChannelStatsRepository    _voiceChannelStats;
		public  IVoiceChannelStatsRepository    VoiceChannelStats => _voiceChannelStats ??= new VoiceChannelStatsRepository(Context);
		private ITeamUpdateRankRepository       _teamUpdateRanks;
		public  ITeamUpdateRankRepository       TeamUpdateRanks => _teamUpdateRanks ??= new TeamUpdateRankRepository(Context);
		private IUserRoleColorBindingRepository _userRoleColorBindings;
		public  IUserRoleColorBindingRepository UserRoleColorBindings => _userRoleColorBindings ??= new UserRoleColorBindingRepository(Context);

		public UnitOfWork(MitternachtContext context) {
			Context = context;
		}

		public int SaveChanges(bool acceptAllChanges = true)
			=> Context.SaveChanges(acceptAllChanges);

		public Task<int> SaveChangesAsync(bool acceptAllChanges = true)
			=> Context.SaveChangesAsync(acceptAllChanges);

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
