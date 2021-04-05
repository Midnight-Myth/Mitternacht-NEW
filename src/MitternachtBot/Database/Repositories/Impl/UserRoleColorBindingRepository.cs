using Discord.WebSocket;
using Mitternacht.Database.Models;
using System.Linq;

namespace Mitternacht.Database.Repositories.Impl {
	public class UserRoleColorBindingRepository : Repository<UserRoleColorBinding>, IUserRoleColorBindingRepository {
		public UserRoleColorBindingRepository(MitternachtContext context) : base(context) { }

		public void CreateBinding(ulong userId, SocketRole role) {
			if(!HasBinding(userId, role)) {
				_set.Add(new UserRoleColorBinding {
					UserId  = userId,
					GuildId = role.Guild.Id,
					RoleId  = role.Id,
				});
			}
		}

		public void DeleteBinding(ulong userId, SocketRole role) {
			_set.RemoveRange(UserBindingsOnGuild(userId, role.Guild.Id).Where(m => m.RoleId == role.Id).ToList());
		}

		public bool HasBinding(ulong userId, SocketRole role)
			=> _set.Any(m => m.UserId == userId && m.GuildId == role.Guild.Id && m.RoleId == role.Id);

		public IQueryable<UserRoleColorBinding> UserBindingsOnGuild(ulong userId, ulong guildId)
			=> _set.AsQueryable().Where(m => m.UserId == userId && m.GuildId == guildId);
	}
}
