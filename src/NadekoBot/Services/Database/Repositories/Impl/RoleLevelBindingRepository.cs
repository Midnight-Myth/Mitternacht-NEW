using NadekoBot.Services.Database.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using System.Collections.Generic;

namespace NadekoBot.Services.Database.Repositories.Impl
{
    public class RoleLevelBindingRepository : Repository<RoleLevelBinding>, IRoleLevelBindingRepository
    {
        public RoleLevelBindingRepository(DbContext context) : base(context)
        {
            UpdateRoleLevelBindingsCopy();
        }

        private IEnumerable<RoleLevelBinding> rolelevelbindings;
        public IEnumerable<RoleLevelBinding> RoleLevelBindings => rolelevelbindings;

        /// <exception cref="ArgumentException"></exception>
        public int GetMinimumLevel(ulong roleid)
        {
            var rl = RoleLevelBindings.FirstOrDefault(r => r.RoleId == roleid);
            if (rl == null) throw new ArgumentException($"Role with Id {roleid} is not bound to a level!");
            return rl.MinimumLevel;
        }

        public bool Remove(ulong roleid)
        {
            var rl = _set.FirstOrDefault(r => r.RoleId == roleid);
            if (rl == null) return false;
            _set.Remove(rl);
            _context.SaveChanges();
            UpdateRoleLevelBindingsCopy();
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
            UpdateRoleLevelBindingsCopy();
        }

        private void UpdateRoleLevelBindingsCopy() => rolelevelbindings = _set.AsEnumerable();
    }
}
