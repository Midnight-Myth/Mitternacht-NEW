using System.Threading.Tasks;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Modules.Utility.Services;

namespace Mitternacht.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class VerboseErrorCommands : MitternachtSubmodule<VerboseErrorsService>
        {
            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(Discord.GuildPermission.ManageMessages)]
            public async Task VerboseError()
            {
                var state = Service.ToggleVerboseErrors(Context.Guild.Id);

                if (state)
                    await ReplyConfirmLocalized("verbose_errors_enabled").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("verbose_errors_disabled").ConfigureAwait(false);
            }
        }
    }
}
