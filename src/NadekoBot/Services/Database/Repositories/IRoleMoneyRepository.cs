using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories
{
    public interface IRoleMoneyRepository : IRepository<RoleMoney>
    {
        RoleMoney GetOrCreate(ulong roleid);
        void SetMoney(ulong roleid, long money);
        bool Exists(ulong roleid);
        void SetPriority(ulong roleid, int priority);
        bool Remove(ulong roleid);
    }
}
