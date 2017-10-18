using System.Runtime.CompilerServices;
using Discord.Commands;
using Mitternacht.Services.Impl;

namespace Mitternacht.Common.Attributes
{
    public class Description : SummaryAttribute
    {
        public Description([CallerMemberName] string memberName="") : base(Localization.LoadCommandString(memberName.ToLowerInvariant() + "_desc"))
        {

        }
    }
}
