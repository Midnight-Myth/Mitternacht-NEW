using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Administration.Services;
using Mitternacht.Database;

namespace Mitternacht.Modules.Administration {
	public partial class Administration {
		[Group]
		public class AutoAssignRoleCommands : MitternachtSubmodule<AutoAssignRoleService> {
			private readonly IUnitOfWork uow;

			public AutoAssignRoleCommands(IUnitOfWork uow) {
				this.uow = uow;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.ManageRoles)]
			public async Task AutoAssignRole([Remainder] IRole role = null) {
				var guildUser = Context.User as IGuildUser;

				if(role == null || Context.User.Id == guildUser.Guild.OwnerId || guildUser.GetRoles().Max(x => x.Position) > role.Position) {
					var conf = uow.GuildConfigs.For(Context.Guild.Id);
					conf.AutoAssignRoleId = role == null ? 0 : role.Id;

					await uow.SaveChangesAsync(false).ConfigureAwait(false);

					if(role == null) {
						await ReplyConfirmLocalized("aar_disabled").ConfigureAwait(false);
					} else {
						await ReplyConfirmLocalized("aar_enabled").ConfigureAwait(false);
					}
				}
			}
		}
	}
}
