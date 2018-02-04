using Mitternacht.Services;

namespace Mitternacht.Modules.Birthday.Services
{
    public class BirthdayService : INService
    {
        private readonly DbService _db;

        public BirthdayService(DbService db) {
            _db = db;
        }
    }
}