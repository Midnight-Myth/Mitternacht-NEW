using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Mitternacht.Common {
	public class Shard0Precondition : PreconditionAttribute {
		public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services) {
			var c = context.Client as DiscordSocketClient;
			return Task.FromResult(c.ShardId != 0 ? PreconditionResult.FromError("Must be ran from shard #0") : PreconditionResult.FromSuccess());
		}
	}
}
