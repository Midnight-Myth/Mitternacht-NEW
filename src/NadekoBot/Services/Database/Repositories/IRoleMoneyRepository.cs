using NadekoBot.Services.Database.Models;

namespace NadekoBot.Services.Database.Repositories
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
