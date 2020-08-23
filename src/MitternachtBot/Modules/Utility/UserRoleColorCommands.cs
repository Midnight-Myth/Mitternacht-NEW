using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Services.Database;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mitternacht.Modules.Utility {
	public partial class Utility {
		[Group]
		public class UserRoleColorCommands : MitternachtSubmodule {
			private readonly IUnitOfWork _uow;

			public UserRoleColorCommands(IUnitOfWork uow) {
				_uow = uow;
			}

			[MitternachtCommand, Description, Usage, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task UserRoleColorBindings(IUser user = null) {
				user ??= Context.User;

				const int elementsPerPage = 10;
				var bindings = _uow.UserRoleColorBindings.UserBindingsOnGuild(user.Id, Context.Guild.Id).ToList();

				if(bindings.Any()) {
					await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, 0, currentPage => new EmbedBuilder().WithTitle(GetText("userrolecolorbindings_list_title", user.ToString())).WithOkColor().WithDescription(string.Join("\n", bindings.Skip(elementsPerPage * currentPage).Take(elementsPerPage))), (int)Math.Ceiling(bindings.Count * 1d / elementsPerPage), true, new[] { Context.User as IGuildUser }).ConfigureAwait(false);
				} else {
					await ErrorLocalized("userrolecolorbindings_list_empty", user.ToString()).ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Description, Usage, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.ManageRoles)]
			public async Task UserRoleColorBinding(SocketRole role, IGuildUser guildUser = null) {
				guildUser ??= Context.User as IGuildUser;
				
				if(!_uow.UserRoleColorBindings.HasBinding(guildUser.Id, role)) {
					_uow.UserRoleColorBindings.CreateBinding(guildUser.Id, role);

					await ConfirmLocalized("userrolecolorbinding_role_added", guildUser.ToString(), role.Name).ConfigureAwait(false);
				} else {
					_uow.UserRoleColorBindings.DeleteBinding(guildUser.Id, role);

					await ConfirmLocalized("userrolecolorbinding_role_removed", guildUser.ToString(), role.Name).ConfigureAwait(false);
				}

				_uow.SaveChanges(false);
			}
		}
	}
}
