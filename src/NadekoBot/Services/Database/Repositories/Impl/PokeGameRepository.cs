using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories.Impl
{
    public class PokeGameRepository : Repository<UserPokeTypes>, IPokeGameRepository
    {
        public PokeGameRepository(DbContext context) : base(context)
        {

        }

        //List<UserPokeTypes> GetAllPokeTypes()
        //{
        //    var toReturn = _set.Include(pt => pt.UserId).ToList();
        //    toReturn.ForEach(pt => pt.).ToList();
        //    return toReturn;
        //}
    }
}
