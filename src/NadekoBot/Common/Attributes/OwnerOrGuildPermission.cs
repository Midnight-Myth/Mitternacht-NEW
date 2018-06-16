using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Services;

namespace Mitternacht.Common.Attributes
{
    public class OwnerOrGuildPermission : PreconditionAttribute
    {
        private readonly GuildPermission _permLevel;

        public OwnerOrGuildPermission(GuildPermission perm)
        {
            _permLevel = perm;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var creds = (IBotCredentials)services.GetService(typeof(IBotCredentials));

            return Task.FromResult(creds.IsOwner(context.User) || context.Client.CurrentUser.Id == context.User.Id || (context.User is IGuildUser gu && gu.GuildPermissions.Has(_permLevel)) ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("Not owner"));
        }
    }
}