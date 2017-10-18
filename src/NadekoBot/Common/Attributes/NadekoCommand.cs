using System.Runtime.CompilerServices;
using Discord.Commands;
using Mitternacht.Services.Impl;

namespace Mitternacht.Common.Attributes
{
    public class NadekoCommand : CommandAttribute
    {
        public NadekoCommand([CallerMemberName] string memberName="") : base(Localization.LoadCommandString(memberName.ToLowerInvariant() + "_cmd").Split(' ')[0])
        {

        }
    }
}
