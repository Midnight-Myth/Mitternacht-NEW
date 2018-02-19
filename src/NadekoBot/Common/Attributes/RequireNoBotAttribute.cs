using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace Mitternacht.Common.Attributes
{
    public class RequireNoBotAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo cmd, IServiceProvider services) 
            => Task.FromResult(!context.User.IsBot ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("User is bot"));
    }
}