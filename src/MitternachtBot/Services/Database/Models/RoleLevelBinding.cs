namespace Mitternacht.Services.Database.Models
{
    public class RoleLevelBinding : DbEntity
    {
        public ulong RoleId { get; set; }
        public int MinimumLevel { get; set; }
    }
}
