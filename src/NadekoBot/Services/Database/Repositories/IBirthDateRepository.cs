using System;
using System.Collections.Generic;
using Mitternacht.Modules.Birthday.Models;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories
{
    public interface IBirthDateRepository : IRepository<BirthDateModel>
    {
        IEnumerable<BirthDateModel> GetBirthdays(DateTime date);
        IEnumerable<BirthDateModel> GetBirthdays(IBirthDate bd, bool checkYear = false);
        BirthDateModel GetUserBirthDate(ulong userid);
        bool HasBirthDate(ulong userid);
        void SetBirthDate(ulong userid, IBirthDate bd);
        bool DeleteBirthDate(ulong userid);
        bool? BirthdayMessageEnabled(ulong userId);
    }
}