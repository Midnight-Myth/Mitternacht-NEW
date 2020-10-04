using Discord.WebSocket;
using Mitternacht.Database.Models;
using System.Linq;

namespace Mitternacht.Database.Repositories {
	public interface IUserRoleColorBindingRepository {
		bool HasBinding(ulong userId, SocketRole role);
		void CreateBinding(ulong userId, SocketRole role);
		void DeleteBinding(ulong userId, SocketRole role);
		IQueryable<UserRoleColorBinding> UserBindingsOnGuild(ulong userId, ulong guildId);
	}
}
