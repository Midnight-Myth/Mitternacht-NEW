using System;
using System.Linq;
using Mitternacht.Modules.Birthday.Models;
using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories {
	public interface IBirthDateRepository : IRepository<BirthDateModel> {
		IQueryable<BirthDateModel> GetBirthdays(DateTime date);
		IQueryable<BirthDateModel> GetBirthdays(IBirthDate bd, bool checkYear = false);
		BirthDateModel GetUserBirthDate(ulong userid);
		bool HasBirthDate(ulong userid);
		void SetBirthDate(ulong userid, IBirthDate bd);
		bool DeleteBirthDate(ulong userid);
		bool? BirthdayMessageEnabled(ulong userId);
	}
}