using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace NadekoBot.Common.Attributes
{
    public class RequireNoBotAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo cmd, IServiceProvider services) {
            return Task.FromResult(!context.User.IsBot ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("User is bot"));
        }
    }
}