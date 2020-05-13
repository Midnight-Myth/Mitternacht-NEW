using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories.Impl
{
    public class RoleLevelBindingRepository : Repository<RoleLevelBinding>, IRoleLevelBindingRepository
    {
        public RoleLevelBindingRepository(DbContext context) : base(context)
        {
        }

        /// <exception cref="ArgumentException"></exception>
        public int GetMinimumLevel(ulong roleid)
        {
            var rl = _set.FirstOrDefault(r => r.RoleId == roleid);
            if (rl == null) throw new ArgumentException($"Role with Id {roleid} is not bound to a level!");
            return rl.MinimumLevel;
        }

        public bool Remove(ulong roleid)
        {
            var rl = _set.FirstOrDefault(r => r.RoleId == roleid);
            if (rl == null) return false;
            _set.Remove(rl);
            _context.SaveChanges();
            return true;
        }

        public void SetBinding(ulong roleid, int level)
        {
            var rl = _set.FirstOrDefault(r => r.RoleId == roleid);
            if(rl == null)
            {
                _set.Add(rl = new RoleLevelBinding()
                {
                    RoleId = roleid,
                    MinimumLevel = level
                });
                
            }
            else
            {
                rl.MinimumLevel = level;
                _set.Update(rl);
            }
            _context.SaveChanges();
        }
    }
}
