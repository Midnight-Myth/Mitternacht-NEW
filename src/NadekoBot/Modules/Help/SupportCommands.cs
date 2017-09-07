using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Help.Services;

namespace NadekoBot.Modules.Help
{
    public partial class Help
    {
        [Group]
        public class SupportCommands : NadekoSubmodule<SupportService>
        {
            public SupportCommands() {
                
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Support(string text) {
                var supportChannelId = _service.GetSupportChannelId(Context.Guild.Id);
                if (!supportChannelId.HasValue) {
                    await Context.Channel.SendErrorAsync("Für diesen Server ist kein Supportkanal festgelegt!");
                    return;
                }
                var supportChannel = (await Context.Guild.GetTextChannelsAsync()).FirstOrDefault(t => t.Id == supportChannelId.Value);
                if (supportChannel == null) {
                    await Context.Channel.SendErrorAsync("Der gespeicherte Supportkanal existiert nicht. Bitte melde das einem Botowner!");
                    return;
                }
                
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task SetSupportChannel([Remainder]ITextChannel channel = null) {
                await _service.SetSupportChannel(Context.Guild.Id, channel?.Id);
                await Context.Channel.SendConfirmAsync($"Supportkanal wurde {(channel == null ? "deaktiviert" : $"auf Kanal {channel.Mention} gesetzt")}.");
            }
        }
    }
}