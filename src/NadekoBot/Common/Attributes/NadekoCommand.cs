using System.Runtime.CompilerServices;
using Discord.Commands;
using Mitternacht.Resources;

namespace Mitternacht.Common.Attributes
{
    public class NadekoCommand : CommandAttribute
    {
        public NadekoCommand([CallerMemberName] string memberName="") 
            : base(CommandStrings.GetCommandStringModel(memberName.ToLowerInvariant()).Command)
        {

        }
    }
}
