using System;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Mitternacht.Services;

namespace Mitternacht.Common.Attributes {
	public class OwnerOnlyAttribute : PreconditionAttribute {
		public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo executingCommand, IServiceProvider services) {
			var creds    = services.GetService<IBotCredentials>();
			var hasPerms = creds.IsOwner(context.User) || context.Client.CurrentUser.Id == context.User.Id;

			return Task.FromResult(hasPerms ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("Not owner"));
		}
	}
}