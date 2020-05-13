using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories.Impl
{
    public class RoleMoneyRepository : Repository<RoleMoney>, IRoleMoneyRepository
    {
        public RoleMoneyRepository(DbContext context) : base(context)
        {
        }

        public RoleMoney GetOrCreate(ulong roleid)
        {
            var rm = _set.FirstOrDefault(m => m.RoleId == roleid);

            if(rm == null)
            {
                _set.Add(rm = new RoleMoney()
                {
                    RoleId = roleid,
                    Money = 0,
                    Priority = 0
                });
                _context.SaveChanges();
            }
            return rm;
        }

        public void SetMoney(ulong roleid, long money)
        {
            var rm = GetOrCreate(roleid);
            rm.Money = money;
            _set.Update(rm);
            _context.SaveChanges();
        }

        public bool Exists(ulong roleid)
        {
            return _set.FirstOrDefault(rm => rm.RoleId == roleid) != null;
        }

        public void SetPriority(ulong roleid, int priority)
        {
            var rm = GetOrCreate(roleid);
            rm.Priority = priority;
            _set.Update(rm);
            _context.SaveChanges();
        }

        public bool Remove(ulong roleid)
        {
            if (!Exists(roleid)) return false;
            _set.Remove(GetOrCreate(roleid));
            _context.SaveChanges();
            return true;
        }
    }
}
