using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Common.TypeReaders;
using Mitternacht.Modules.Permissions.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Permissions {
	public partial class Permissions {
		[Group]
		public class BlacklistCommands : MitternachtSubmodule<BlacklistService> {
			private readonly DbService _db;

			public BlacklistCommands(DbService db) {
				_db = db;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[OwnerOnly]
			public Task BlacklistUser(AddRemove action, ulong id)
				=> Blacklist(action, id, BlacklistType.User);

			[MitternachtCommand, Usage, Description, Aliases]
			[OwnerOnly]
			public Task BlacklistUser(AddRemove action, IUser user)
				=> Blacklist(action, user.Id, BlacklistType.User);

			[MitternachtCommand, Usage, Description, Aliases]
			[OwnerOnly]
			public Task BlacklistChannel(AddRemove action, ulong id)
				=> Blacklist(action, id, BlacklistType.Channel);

			[MitternachtCommand, Usage, Description, Aliases]
			[OwnerOnly]
			public Task BlacklistChannel(AddRemove action, IChannel channel)
				=> Blacklist(action, channel.Id, BlacklistType.Channel);

			[MitternachtCommand, Usage, Description, Aliases]
			[OwnerOnly]
			public Task BlacklistServer(AddRemove action, ulong id)
				=> Blacklist(action, id, BlacklistType.Server);

			[MitternachtCommand, Usage, Description, Aliases]
			[OwnerOnly]
			public Task BlacklistServer(AddRemove action, IGuild guild)
				=> Blacklist(action, guild.Id, BlacklistType.Server);

			private async Task Blacklist(AddRemove action, ulong id, BlacklistType type) {
				using var uow = _db.UnitOfWork;
				if(action == AddRemove.Add) {
					uow.BotConfig.GetOrCreate().Blacklist.Add(new BlacklistItem {
						ItemId = id,
						Type   = type,
					});
				} else {
					uow.BotConfig.GetOrCreate().Blacklist.RemoveWhere(bi => bi.ItemId == id && bi.Type == type);
				}
				await uow.SaveChangesAsync().ConfigureAwait(false);

				await ReplyConfirmLocalized(action == AddRemove.Add ? "blacklisted" : "unblacklisted", Format.Code(type.ToString()), Format.Code(id.ToString())).ConfigureAwait(false);
			}
		}
	}
}
