using Discord.Commands;
using NadekoBot.Modules.Help.Services;

namespace NadekoBot.Modules.Help
{
    public partial class Help
    {
        [Group]
        public class SupportCommands : NadekoSubmodule<SupportService>
        {
            
        }
    }
}