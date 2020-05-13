using System;
using System.Threading.Tasks;
using Discord.Commands;
using Mitternacht.Services;

namespace Mitternacht.Common.Attributes
{
    public class UserVerifiedAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var db = (DbService) services.GetService(typeof(DbService));
            using (var uow = db.UnitOfWork)
            {
                return Task.FromResult(uow.VerifiedUsers.IsDiscordUserVerified(context.Guild.Id, context.User.Id)
                    ? PreconditionResult.FromSuccess()
                    : PreconditionResult.FromError("User is not verified"));
            }
        }
    }
}