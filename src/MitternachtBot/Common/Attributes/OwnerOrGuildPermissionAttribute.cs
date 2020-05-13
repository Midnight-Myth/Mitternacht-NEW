using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Services;

namespace Mitternacht.Common.Attributes
{
    public class OwnerOrGuildPermissionAttribute : PreconditionAttribute
    {
        private readonly GuildPermission _permLevel;

        public OwnerOrGuildPermissionAttribute(GuildPermission perm)
        {
            _permLevel = perm;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo cmd, IServiceProvider services)
        {
            var creds = (IBotCredentials)services.GetService(typeof(IBotCredentials));
            //var strings = (StringService) services.GetService(typeof(StringService));
            var hasPerms = creds.IsOwner(context.User) 
                         || context.Client.CurrentUser.Id == context.User.Id 
                         || context.User is IGuildUser gu && gu.GuildPermissions.Has(_permLevel);

            //if (!hasPerms)
            //    await context.Channel
            //        .SendErrorAsync($"{context.User.Mention} {strings.GetText("perms_missing", context.Guild.Id, "precattr", _permLevel.ToString())}")
            //        .ConfigureAwait(false);

            return Task.FromResult(hasPerms ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("Not owner"));
        }
    }
}