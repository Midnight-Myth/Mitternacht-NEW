using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database;

namespace Mitternacht.Services
{
    public class DbService
    {
        private readonly DbContextOptions _options;

        public DbService(IBotCredentials creds)
        {
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlite(creds.DbConnectionString);
            _options = optionsBuilder.Options;
            //switch (_creds.Db.Type.ToUpperInvariant())
            //{
            //    case "SQLITE":
            //        dbType = typeof(NadekoSqliteContext);
            //        break;
            //    //case "SQLSERVER":
            //    //    dbType = typeof(NadekoSqlServerContext);
            //    //    break;
            //    default:
            //        break;

            //}
        }

        public NadekoContext GetDbContext()
        {
            var context = new NadekoContext(_options);
            context.Database.SetCommandTimeout(60);
            context.Database.Migrate();
            context.EnsureSeedData();

            //set important sqlite stuffs
            var conn = context.Database.GetDbConnection();
            conn.Open();

            context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL");
            using (var com = conn.CreateCommand())
            {
                com.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=OFF";
                com.ExecuteNonQuery();
            }

            return context;
        }

        public IUnitOfWork UnitOfWork =>
            new UnitOfWork(GetDbContext());
    }
}