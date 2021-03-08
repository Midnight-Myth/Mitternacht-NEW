using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Administration.Services;
using Mitternacht.Database;

namespace Mitternacht.Modules.Administration {
	public partial class Administration
    {
        [Group]
        public class AutoAssignRoleCommands : MitternachtSubmodule<AutoAssignRoleService>
        {
            private readonly IUnitOfWork uow;

            public AutoAssignRoleCommands(IUnitOfWork uow)
            {
                this.uow = uow;
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task AutoAssignRole([Remainder] IRole role = null)
            {
                var guser = (IGuildUser)Context.User;
                if (role != null)
                    if (Context.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= role.Position)
                        return;

                var conf = uow.GuildConfigs.For(Context.Guild.Id);
                if (role == null)
                {
                    conf.AutoAssignRoleId = 0;
                }
                else
                {
                    conf.AutoAssignRoleId = role.Id;
                }

                await uow.SaveChangesAsync(false).ConfigureAwait(false);

                if (role == null)
                {
                    await ReplyConfirmLocalized("aar_disabled").ConfigureAwait(false);
                    return;
                }

                await ReplyConfirmLocalized("aar_enabled").ConfigureAwait(false);
            }
        }
    }
}
