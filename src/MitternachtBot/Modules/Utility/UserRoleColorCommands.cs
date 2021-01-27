using Colourful;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Common;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Database;
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
				var contextGuildUser = Context.User as IGuildUser;
				guildUser ??= contextGuildUser;

				if(contextGuildUser.GuildPermissions.Administrator || contextGuildUser.GetRoles().Max(r => r.Position) >= role.Position) {
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
					var allowedRoleColors   = (Context.User as IGuildUser).GetRoles().Select(r => r.Color).ToArray();
					var forbiddenRoleColors = Context.Guild.Roles.Where(r => r.IsHoisted && r.Id != role.Id);

					var converter            = new ConverterBuilder().FromRGB().ToLab().Build();
					var colorDifference      = new CIEDE2000ColorDifference();
					var requestedColor       = converter.Convert(color.ToRGBColor());
					var similarityRadius     = _uow.GuildConfigs.For(Context.Guild.Id).ColorMetricSimilarityRadius;
					var similarlyColoredRole = forbiddenRoleColors.FirstOrDefault(c => colorDifference.ComputeDifference(requestedColor, converter.Convert(RGBColor.FromRGB8Bit(c.Color.R, c.Color.G, c.Color.B))) < similarityRadius);

					if(similarlyColoredRole == null || allowedRoleColors.Any(c => colorDifference.ComputeDifference(requestedColor, converter.Convert(RGBColor.FromRGB8Bit(c.R, c.G, c.B))) < similarityRadius)) {
						await role.ModifyAsync(rp => rp.Color = color).ConfigureAwait(false);

						await ReplyConfirmLocalized("userrolecolor_changed", role.Name, color).ConfigureAwait(false);
					} else {
						await ReplyErrorLocalized("userrolecolor_color_too_similar", color, similarlyColoredRole.Name).ConfigureAwait(false);
					}
				} else {
					await ReplyErrorLocalized("userrolecolor_no_binding", role.Name).ConfigureAwait(false);
				}
			}
		}
	}
}
