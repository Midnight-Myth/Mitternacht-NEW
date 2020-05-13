namespace Mitternacht.Services.Database.Models
{
    public class RoleMoney : DbEntity
    {
        public ulong RoleId { get; set; }
        public long Money { get; set; }
        public int Priority { get; set; }
    }
}
