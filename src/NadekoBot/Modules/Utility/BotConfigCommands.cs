using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common;
using Mitternacht.Common.Attributes;
using Mitternacht.Services;

namespace Mitternacht.Modules.Utility
{
    public partial class Utility
    {
        public class BotConfigCommands : MitternachtSubmodule
        {
            private readonly IBotConfigProvider _service;

            public BotConfigCommands(IBotConfigProvider service)
            {
                _service = service;
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task BotConfigEdit()
            {
                var names = Enum.GetNames(typeof(BotConfigEditType));
                await ReplyAsync(string.Join(", ", names)).ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task BotConfigEdit(BotConfigEditType type, [Remainder]string newValue = null)
            {
                if (string.IsNullOrWhiteSpace(newValue))
                    newValue = null;

                var success = _service.Edit(type, newValue);

                if (!success)
                    await ReplyErrorLocalized("bot_config_edit_fail", Format.Bold(type.ToString()), Format.Bold(newValue ?? "NULL")).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("bot_config_edit_success", Format.Bold(type.ToString()), Format.Bold(newValue ?? "NULL")).ConfigureAwait(false);
            }
        }
    }
}