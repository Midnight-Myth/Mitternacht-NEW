using NadekoBot.Services.Database.Models;
using System.Collections.Generic;

namespace NadekoBot.Services.Database.Repositories
{
    public interface IRoleLevelBindingRepository : IRepository<RoleLevelBinding>
    {
        IEnumerable<RoleLevelBinding> RoleLevelBindings { get; }
        void SetBinding(ulong roleid, int level);
        bool Remove(ulong roleid);
        int GetMinimumLevel(ulong roleid);
    }
}
