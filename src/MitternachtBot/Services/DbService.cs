using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database;
using Mitternacht.Services.Database.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mitternacht.Services {
	public class DbService {
		private readonly DbContextOptions _optionsSqlite;
		private readonly DbContextOptions _optionsPostgres;

		public DbService(IBotCredentials creds) {
			var optionsBuilderSqlite = new DbContextOptionsBuilder();
			optionsBuilderSqlite.UseSqlite(creds.DbConnectionString);
			_optionsSqlite = optionsBuilderSqlite.Options;

			var optionsBuilderPostgres = new DbContextOptionsBuilder();
			optionsBuilderPostgres.UseNpgsql(creds.DbConnection);
			_optionsPostgres = optionsBuilderPostgres.Options;
		}

		public MitternachtContext GetDbContext() {
			var context = new MitternachtContext(_optionsPostgres);
			context.Database.SetCommandTimeout(60);
			MigrateSqliteToPostgresOrMigrate(context);
			context.EnsureSeedData();

			return context;
		}

		private NadekoContext GetDbContextSqlite() {
			var context = new NadekoContext(_optionsSqlite);
			if(context.Database.CanConnect()) {
				context.Database.SetCommandTimeout(60);
				context.Database.Migrate();
				context.EnsureSeedData();

				//set important sqlite stuffs
				var conn = context.Database.GetDbConnection();
				conn.Open();

				context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL");
				using(var com = conn.CreateCommand()) {
					com.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=OFF";
					com.ExecuteNonQuery();
				}

				return context;
			} else {
				return null;
			}
		}

		private void MigrateSqliteToPostgresOrMigrate(MitternachtContext pgsql) {
			NadekoContext sqlite;
			var logger = LogManager.GetCurrentClassLogger();

			if(pgsql.Database.GetAppliedMigrations().Count() == 0 && (sqlite = GetDbContextSqlite()) != null) {
				logger.Info("Performing first MitternachtContext migration, copying data from SQLite database to PostgreSQL...");

				pgsql.Database.Migrate();

				static void CopyData<T>(IEnumerable<T> source, DbSet<T> destination) where T : DbEntity {
					destination.AddRange(source.OrderBy((Func<T, DateTime>)(t => t.DateAdded ?? DateTime.MinValue)).ToList());
				}

				var dbEntities = 0;
				var nonDbEntities = 0;

				void stateChanged(object obj, Microsoft.EntityFrameworkCore.ChangeTracking.EntityStateChangedEventArgs e) {
					if(e.OldState == EntityState.Added || e.NewState == EntityState.Added) {
						if(e.Entry.Entity is DbEntity dbEntity) {
							dbEntities++;
							dbEntity.Id = 0;
						} else {
							nonDbEntities++;
						}
					}
				}
				pgsql.ChangeTracker.StateChanged += stateChanged;

				CopyData(sqlite.BirthDates, pgsql.BirthDates);
				CopyData(sqlite.BotConfig
					.Include(t => t.Blacklist)
					.Include(t => t.RotatingStatusMessages)
					.Include(t => t.EightBallResponses)
					.Include(t => t.RaceAnimals)
					.Include(t => t.StartupCommands)
					.Include(t => t.BlockedCommands)
					.Include(t => t.BlockedModules), pgsql.BotConfig);
				CopyData(sqlite.Currency, pgsql.Currency);
				CopyData(sqlite.CurrencyTransactions, pgsql.CurrencyTransactions);
				CopyData(sqlite.CustomReactions, pgsql.CustomReactions);
				CopyData(sqlite.DailyMoney, pgsql.DailyMoney);
				CopyData(sqlite.DailyMoneyStats, pgsql.DailyMoneyStats);
				CopyData(sqlite.Donators, pgsql.Donators);
				CopyData(sqlite.GuildConfigs.ToList().Select(gc => {
					sqlite.Entry(gc).Reference(gc => gc.RootPermission).Load();
					sqlite.Entry(gc).Collection(gc => gc.Permissions).Load();
					sqlite.Entry(gc).Reference(gc => gc.LogSetting).Load();

					sqlite.Entry(gc).Collection(gc => gc.FilterInvitesChannelIds).Load();
					sqlite.Entry(gc).Collection(gc => gc.FilteredWords).Load();
					sqlite.Entry(gc).Collection(gc => gc.FilterWordsChannelIds).Load();
					sqlite.Entry(gc).Collection(gc => gc.FilterZalgoChannelIds).Load();

					if(gc.LogSetting != null)
						sqlite.Entry(gc.LogSetting).Collection(ls => ls.IgnoredChannels).Load();
					sqlite.Entry(gc).Reference(gc => gc.AntiSpamSetting).Load();
					if(gc.AntiSpamSetting != null)
						sqlite.Entry(gc.AntiSpamSetting).Collection(x => x.IgnoredChannels).Load();
					sqlite.Entry(gc).Reference(gc => gc.AntiRaidSetting).Load();
					sqlite.Entry(gc).Reference(gc => gc.StreamRole).Load();
					
					sqlite.Entry(gc).Collection(gc => gc.MutedUsers).Load();
					sqlite.Entry(gc).Collection(gc => gc.GuildRepeaters).Load();
					sqlite.Entry(gc).Collection(gc => gc.FollowedStreams).Load();
					sqlite.Entry(gc).Collection(gc => gc.GenerateCurrencyChannelIds).Load();
					sqlite.Entry(gc).Collection(gc => gc.CommandCooldowns).Load();
					sqlite.Entry(gc).Collection(gc => gc.UnmuteTimers).Load();
					sqlite.Entry(gc).Collection(gc => gc.VcRoleInfos).Load();
					sqlite.Entry(gc).Collection(gc => gc.CommandAliases).Load();
					sqlite.Entry(gc).Collection(gc => gc.WarnPunishments).Load();

					sqlite.Entry(gc).Collection(gc => gc.SlowmodeIgnoredRoles).Load();
					sqlite.Entry(gc).Collection(gc => gc.SlowmodeIgnoredUsers).Load();
					sqlite.Entry(gc).Collection(gc => gc.NsfwBlacklistedTags).Load();
					sqlite.Entry(gc).Collection(gc => gc.ShopEntries).Load();

					return gc;
				}), pgsql.GuildConfigs);
				CopyData(sqlite.LevelModel, pgsql.LevelModel);
				CopyData(sqlite.MessageXpRestrictions, pgsql.MessageXpRestrictions);
				CopyData(sqlite.NicknameHistory, pgsql.NicknameHistory);
				CopyData(sqlite.Quotes, pgsql.Quotes);
				CopyData(sqlite.Reminders, pgsql.Reminders);
				CopyData(sqlite.RoleLevelBinding, pgsql.RoleLevelBinding);
				CopyData(sqlite.RoleMoney, pgsql.RoleMoney);
				CopyData(sqlite.SelfAssignableRoles, pgsql.SelfAssignableRoles);
				CopyData(sqlite.TeamUpdateRank, pgsql.TeamUpdateRank);
				CopyData(sqlite.UsernameHistory, pgsql.UsernameHistory);
				CopyData(sqlite.VerifiedUsers, pgsql.VerifiedUsers);
				CopyData(sqlite.VoiceChannelStats, pgsql.VoiceChannelStats);
				CopyData(sqlite.Warnings, pgsql.Warnings);

				pgsql.SaveChanges();

				LogManager.GetCurrentClassLogger().Info($"Copied {dbEntities} entities, found {nonDbEntities} non-DbEntities during the process.");
				pgsql.ChangeTracker.StateChanged -= stateChanged;
			} else {
				pgsql.Database.Migrate();
			}
		}

		public IUnitOfWork UnitOfWork =>
			new UnitOfWork(GetDbContext());


	}
}