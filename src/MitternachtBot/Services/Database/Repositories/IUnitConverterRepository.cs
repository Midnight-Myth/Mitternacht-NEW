using System;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories
{
    public interface IUnitConverterRepository : IRepository<ConvertUnit>
    {
        void AddOrUpdate(Func<ConvertUnit, bool> check, ConvertUnit toAdd, Func<ConvertUnit, ConvertUnit> toUpdate);
        bool Empty();
    }
}
