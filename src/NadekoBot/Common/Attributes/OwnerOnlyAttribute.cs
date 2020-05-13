using System;
using System.Threading.Tasks;
using Discord.Commands;
using Mitternacht.Services;

namespace Mitternacht.Common.Attributes
{
    public class OwnerOnlyAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo executingCommand, IServiceProvider services)
        {
            var creds = (IBotCredentials) services.GetService(typeof(IBotCredentials));
            //var strings = (StringService) services.GetService(typeof(StringService));
            var hasPerms = creds.IsOwner(context.User) || context.Client.CurrentUser.Id == context.User.Id;

            //if (!hasPerms)
            //    await context.Channel.SendErrorAsync(strings.GetText("owner_only", context.Guild?.Id, "precattr"))
            //        .ConfigureAwait(false);

            return Task.FromResult(hasPerms ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("Not owner"));
        }
    }
}