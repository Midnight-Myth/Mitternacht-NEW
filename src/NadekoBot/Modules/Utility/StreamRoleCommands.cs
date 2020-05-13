using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Common.TypeReaders;
using Mitternacht.Modules.Utility.Common;
using Mitternacht.Modules.Utility.Services;

namespace Mitternacht.Modules.Utility
{
    public partial class Utility
    {
        public class StreamRoleCommands : MitternachtSubmodule<StreamRoleService>
        {
            [MitternachtCommand, Usage, Description, Aliases]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireContext(ContextType.Guild)]
            public async Task StreamRole(IRole fromRole, IRole addRole)
            {
                await this.Service.SetStreamRole(fromRole, addRole).ConfigureAwait(false);

                await ReplyConfirmLocalized("stream_role_enabled", Format.Bold(fromRole.ToString()), Format.Bold(addRole.ToString())).ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireContext(ContextType.Guild)]
            public async Task StreamRole()
            {
                await this.Service.StopStreamRole(Context.Guild).ConfigureAwait(false);
                await ReplyConfirmLocalized("stream_role_disabled").ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireContext(ContextType.Guild)]
            public async Task StreamRoleKeyword([Remainder]string keyword = null)
            {
                string kw = await this.Service.SetKeyword(Context.Guild, keyword).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(keyword))
                    await ReplyConfirmLocalized("stream_role_kw_reset").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("stream_role_kw_set", Format.Bold(kw)).ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireContext(ContextType.Guild)]
            public async Task StreamRoleBlacklist(AddRemove action, [Remainder] IGuildUser user)
            {
                var success = await this.Service.ApplyListAction(StreamRoleListType.Blacklist, Context.Guild, action, user.Id, user.ToString())
                    .ConfigureAwait(false);

                if (action == AddRemove.Add)
                    if (success)
                        await ReplyConfirmLocalized("stream_role_bl_add", Format.Bold(user.ToString())).ConfigureAwait(false);
                    else
                        await ReplyConfirmLocalized("stream_role_bl_add_fail", Format.Bold(user.ToString())).ConfigureAwait(false);
                else
                    if (success)
                    await ReplyConfirmLocalized("stream_role_bl_rem", Format.Bold(user.ToString())).ConfigureAwait(false);
                else
                    await ReplyErrorLocalized("stream_role_bl_rem_fail", Format.Bold(user.ToString())).ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireContext(ContextType.Guild)]
            public async Task StreamRoleWhitelist(AddRemove action, [Remainder] IGuildUser user)
            {
                var success = await this.Service.ApplyListAction(StreamRoleListType.Whitelist, Context.Guild, action, user.Id, user.ToString())
                    .ConfigureAwait(false);

                if (action == AddRemove.Add)
                    if (success)
                        await ReplyConfirmLocalized("stream_role_wl_add", Format.Bold(user.ToString())).ConfigureAwait(false);
                    else
                        await ReplyConfirmLocalized("stream_role_wl_add_fail", Format.Bold(user.ToString())).ConfigureAwait(false);
                else
                    if (success)
                    await ReplyConfirmLocalized("stream_role_wl_rem", Format.Bold(user.ToString())).ConfigureAwait(false);
                else
                    await ReplyErrorLocalized("stream_role_wl_rem_fail", Format.Bold(user.ToString())).ConfigureAwait(false);
            }
        }
    }
}