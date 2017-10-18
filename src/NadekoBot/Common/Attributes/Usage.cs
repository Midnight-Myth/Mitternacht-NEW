using System.Runtime.CompilerServices;
using Discord.Commands;
using Mitternacht.Services.Impl;

namespace Mitternacht.Common.Attributes
{
    public class Usage : RemarksAttribute
    {
        public Usage([CallerMemberName] string memberName="") : base(Localization.LoadCommandString(memberName.ToLowerInvariant()+"_usage"))
        {

        }
    }
}
