using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Modules.Birthday.Models;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories.Impl {
	public class BirthDateRepository : Repository<BirthDateModel>, IBirthDateRepository {
		public BirthDateRepository(DbContext context) : base(context) { }

		public IQueryable<BirthDateModel> GetBirthdays(DateTime date)
			=> _set.AsQueryable().Where(b => b.Day == date.Day && b.Month == date.Month);

		public IQueryable<BirthDateModel> GetBirthdays(IBirthDate bd, bool checkYear = false)
			=> _set.AsQueryable().Where(b => b.Day == bd.Day && b.Month == bd.Month && (!checkYear || !bd.Year.HasValue || b.Year.HasValue && bd.Year.HasValue && b.Year == bd.Year));

		public BirthDateModel GetUserBirthDate(ulong userid)
			=> _set.FirstOrDefault(b => b.UserId == userid);

		public bool HasBirthDate(ulong userid)
			=> _set.Any(b => b.UserId == userid);

		public void SetBirthDate(ulong userid, IBirthDate bd) {
			var bdm = GetUserBirthDate(userid);

			if(bdm == null) {
				_set.Add(new BirthDateModel(userid, bd));
			} else {
				bdm.Update(bd);
			}
		}

		public bool DeleteBirthDate(ulong userid) {
			var bdm = GetUserBirthDate(userid);
			
			if(bdm != null) {
				_set.Remove(bdm);
				return true;
			} else {
				return false;
			}
		}

		public bool? BirthdayMessageEnabled(ulong userId)
			=> GetUserBirthDate(userId)?.BirthdayMessageEnabled;
	}
}