using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Common;
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
				var bindings = _uow.UserRoleColorBindings.UserBindingsOnGuild(user.Id, Context.Guild.Id).AsEnumerable().Select(b => Context.Guild.GetRole(b.RoleId)?.Name ?? b.RoleId.ToString()).ToList();

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
				
				if((Context.User as IGuildUser).GetRoles().Max(r => r.Position) >= role.Position) {
					if(!_uow.UserRoleColorBindings.HasBinding(guildUser.Id, role)) {
						_uow.UserRoleColorBindings.CreateBinding(guildUser.Id, role);

						await ConfirmLocalized("userrolecolorbinding_role_added", guildUser.ToString(), role.Name).ConfigureAwait(false);
					} else {
						_uow.UserRoleColorBindings.DeleteBinding(guildUser.Id, role);

						await ConfirmLocalized("userrolecolorbinding_role_removed", guildUser.ToString(), role.Name).ConfigureAwait(false);
					}

					_uow.SaveChanges(false);
				} else {
					await ErrorLocalized("userrolecolorbinding_position_too_high", role.Name).ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Description, Usage, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task UserRoleColor(SocketRole role, HexColor color) {
				if(_uow.UserRoleColorBindings.HasBinding(Context.User.Id, role)) {
					var level = _uow.LevelModel.Get(Context.Guild.Id, Context.User.Id)?.Level ?? 0;
					var levelRoles = _uow.RoleLevelBindings.GetAll().Where(rl => rl.MinimumLevel <= level).Select(rl => rl.RoleId).ToArray();
					var forbiddenRoleColors = Context.Guild.Roles.Where(r => r.IsHoisted && !levelRoles.Contains(r.Id) && r.Id != role.Id);

					var requestedColor = color.ToColor();
					var similarlyColoredRole = forbiddenRoleColors.FirstOrDefault(c => c.Color.Difference(requestedColor) < _uow.GuildConfigs.For(Context.Guild.Id).ColorMetricSimilarityRadius);

					if(similarlyColoredRole == null) {
						await role.ModifyAsync(rp => rp.Color = requestedColor).ConfigureAwait(false);

						await ReplyConfirmLocalized("userrolecolor_changed", role.Name, requestedColor).ConfigureAwait(false);
					} else {
						await ReplyErrorLocalized("userrolecolor_color_too_similar", requestedColor, similarlyColoredRole.Name).ConfigureAwait(false);
					}
				} else {
					await ReplyErrorLocalized("userrolecolor_no_binding", role.Name).ConfigureAwait(false);
				}
			}
		}
	}
}
