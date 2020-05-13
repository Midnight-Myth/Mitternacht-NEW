using System.Linq;
using Discord;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories.Impl
{
    public class DiscordUserRepository : Repository<DiscordUser>, IDiscordUserRepository
    {
        public DiscordUserRepository(DbContext context) : base(context)
        {
        }

        public DiscordUser GetOrCreate(IUser original)
        {
            DiscordUser toReturn;

            toReturn = _set.FirstOrDefault(u => u.UserId == original.Id);

            if (toReturn == null)
                _set.Add(toReturn = new DiscordUser()
                {
                    AvatarId = original.AvatarId,
                    Discriminator = original.Discriminator,
                    UserId = original.Id,
                    Username = original.Username,
                });

            return toReturn;
        }
    }
}
