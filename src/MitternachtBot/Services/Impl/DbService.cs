using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database;

namespace Mitternacht.Services {
	public class DbService {
		private readonly DbContextOptions _optionsPostgres;

		public DbService(IBotCredentials creds) {
			var optionsBuilderPostgres = new DbContextOptionsBuilder();
			optionsBuilderPostgres.UseNpgsql(creds.DbConnection);
			_optionsPostgres = optionsBuilderPostgres.Options;
		}

		public MitternachtContext GetDbContext() {
			var context = new MitternachtContext(_optionsPostgres);
			context.Database.SetCommandTimeout(60);
			context.Database.Migrate();
			context.EnsureSeedData();

			return context;
		}

		public IUnitOfWork UnitOfWork
			=> new UnitOfWork(GetDbContext());
	}
}