using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Mitternacht.Extensions;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database {
	public class MitternachtContextFactory : IDesignTimeDbContextFactory<MitternachtContext> {
		public MitternachtContext CreateDbContext(string[] args) {
			var optionsBuilder = new DbContextOptionsBuilder();
			optionsBuilder.UseNpgsql("Host=127.0.0.1;Port=5432;Database=mitternachtbot_development;Username=mitternachtbottest;Password=mitternachtbottestpassword;");
			var ctx = new MitternachtContext(optionsBuilder.Options);
			ctx.Database.SetCommandTimeout(60);
			return ctx;
		}
	}

	public class MitternachtContext : DbContext {
		public DbSet<BirthDateModel> BirthDates { get; set; }
		public DbSet<BotConfig> BotConfig { get; set; }
		public DbSet<Currency> Currency { get; set; }
		public DbSet<CurrencyTransaction> CurrencyTransactions { get; set; }
		public DbSet<CustomReaction> CustomReactions { get; set; }
		public DbSet<DailyMoney> DailyMoney { get; set; }
		public DbSet<DailyMoneyStats> DailyMoneyStats { get; set; }
		public DbSet<Donator> Donators { get; set; }
		public DbSet<GuildConfig> GuildConfigs { get; set; }
		public DbSet<LevelModel> LevelModel { get; set; }
		public DbSet<MessageXpRestriction> MessageXpRestrictions { get; set; }
		public DbSet<NicknameHistoryModel> NicknameHistory { get; set; }
		public DbSet<Quote> Quotes { get; set; }
		public DbSet<Reminder> Reminders { get; set; }
		public DbSet<RoleLevelBinding> RoleLevelBinding { get; set; }
		public DbSet<RoleMoney> RoleMoney { get; set; }
		public DbSet<SelfAssignedRole> SelfAssignableRoles { get; set; }
		public DbSet<TeamUpdateRank> TeamUpdateRank { get; set; }
		public DbSet<UsernameHistoryModel> UsernameHistory { get; set; }
		public DbSet<VerifiedUser> VerifiedUsers { get; set; }
		public DbSet<VoiceChannelStats> VoiceChannelStats { get; set; }
		public DbSet<Warning> Warnings { get; set; }
		public DbSet<UserRoleColorBinding> UserRoleColorBindings { get; set; }

		public MitternachtContext() {

		}

		public MitternachtContext(DbContextOptions options) : base(options) {
		}

		public void EnsureSeedData() {
			if(BotConfig.Any())
				return;
			var bc = new BotConfig();

			bc.EightBallResponses.AddRange(new HashSet<EightBallResponse> {
				new EightBallResponse { Text = "Most definitely yes" },
				new EightBallResponse { Text = "For sure" },
				new EightBallResponse { Text = "Totally!" },
				new EightBallResponse { Text = "Of course!" },
				new EightBallResponse { Text = "As I see it, yes" },
				new EightBallResponse { Text = "My sources say yes" },
				new EightBallResponse { Text = "Yes" },
				new EightBallResponse { Text = "Most likely" },
				new EightBallResponse { Text = "Perhaps" },
				new EightBallResponse { Text = "Maybe" },
				new EightBallResponse { Text = "Not sure" },
				new EightBallResponse { Text = "It is uncertain" },
				new EightBallResponse { Text = "Ask me again later" },
				new EightBallResponse { Text = "Don't count on it" },
				new EightBallResponse { Text = "Probably not" },
				new EightBallResponse { Text = "Very doubtful" },
				new EightBallResponse { Text = "Most likely no" },
				new EightBallResponse { Text = "Nope" },
				new EightBallResponse { Text = "No" },
				new EightBallResponse { Text = "My sources say no" },
				new EightBallResponse { Text = "Dont even think about it" },
				new EightBallResponse { Text = "Definitely no" },
				new EightBallResponse { Text = "NO - It may cause disease contraction" }
			});

			BotConfig.Add(bc);

			SaveChanges();
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder) {
			modelBuilder.Entity<Donator>()
				.HasIndex(d => d.UserId)
				.IsUnique();

			modelBuilder.Entity<DailyMoney>()
				.HasIndex(c => new { c.GuildId, c.UserId })
				.IsUnique();
			modelBuilder.Entity<RoleMoney>()
				.HasIndex(c => new { c.GuildId, c.RoleId })
				.IsUnique();
			modelBuilder.Entity<LevelModel>()
				.HasIndex(c => new { c.GuildId, c.UserId })
				.IsUnique();
			modelBuilder.Entity<RoleLevelBinding>()
				.HasIndex(c => new { c.GuildId, c.RoleId })
				.IsUnique();
			modelBuilder.Entity<MessageXpRestriction>()
				.HasIndex(c => new { c.GuildId, c.ChannelId })
				.IsUnique();

			modelBuilder.Entity<GuildConfig>()
				.HasIndex(c => c.GuildId)
				.IsUnique();
			modelBuilder.Entity<AntiSpamSetting>()
				.HasOne(x => x.GuildConfig)
				.WithOne(x => x.AntiSpamSetting);
			modelBuilder.Entity<AntiRaidSetting>()
				.HasOne(x => x.GuildConfig)
				.WithOne(x => x.AntiRaidSetting);

			modelBuilder.Entity<SelfAssignedRole>()
				.HasIndex(s => (new { s.GuildId, s.RoleId }))
				.IsUnique();

			modelBuilder.Entity<Currency>()
				.HasIndex(c => new { c.GuildId, c.UserId })
				.IsUnique();
			
			modelBuilder.Entity<Permission>()
				.HasOne(p => p.Next)
				.WithOne(p => p.Previous)
				.IsRequired(false);

			modelBuilder.Entity<RewardedUser>()
				.HasIndex(x => x.UserId)
				.IsUnique();

			modelBuilder.Entity<VerifiedUser>()
				.HasIndex(vu => new { vu.GuildId, vu.UserId })
				.IsUnique();

			modelBuilder.Entity<BirthDateModel>()
				.HasIndex(bdm => bdm.UserId)
				.IsUnique();

			modelBuilder.Entity<VoiceChannelStats>()
				.HasIndex(vcs => new { vcs.UserId, vcs.GuildId })
				.IsUnique();

			modelBuilder.Entity<TeamUpdateRank>()
				.HasIndex(tur => new { tur.GuildId, tur.Rankname })
				.IsUnique();

			modelBuilder.Entity<UserRoleColorBinding>()
				.HasIndex(m => new { m.UserId, m.GuildId, m.RoleId })
				.IsUnique();
		}
	}
}
