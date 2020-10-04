using Discord.WebSocket;
using Mitternacht.Services.Database.Models;
using System.Linq;

namespace Mitternacht.Services.Database.Repositories {
	public interface IUserRoleColorBindingRepository {
		bool HasBinding(ulong userId, SocketRole role);
		void CreateBinding(ulong userId, SocketRole role);
		void DeleteBinding(ulong userId, SocketRole role);
		IQueryable<UserRoleColorBinding> UserBindingsOnGuild(ulong userId, ulong guildId);
	}
}
