using System.Threading.Tasks;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Modules.Utility.Services;

namespace Mitternacht.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class VerboseErrorCommands : NadekoSubmodule<VerboseErrorsService>
        {
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(Discord.GuildPermission.ManageMessages)]
            public async Task VerboseError()
            {
                var state = _service.ToggleVerboseErrors(Context.Guild.Id);

                if (state)
                    await ReplyConfirmLocalized("verbose_errors_enabled").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("verbose_errors_disabled").ConfigureAwait(false);
            }
        }
    }
}
