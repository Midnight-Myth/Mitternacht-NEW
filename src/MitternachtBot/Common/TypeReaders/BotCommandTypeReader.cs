using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Mitternacht.Modules.CustomReactions.Services;
using Mitternacht.Services;

namespace Mitternacht.Common.TypeReaders {
	public class CommandTypeReader : TypeReader {
		public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services) {
			var cmds       = services.GetService<CommandService>();
			var cmdHandler = services.GetService<CommandHandler>();

			var prefix = cmdHandler.GetPrefix(context.Guild);

			CommandInfo cmd = null;
			if(input.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
				var inputWithoutPrefix = input.Substring(prefix.Length);

				cmd = cmds.Commands.FirstOrDefault(c => c.Aliases.Any(ca => ca.Equals(inputWithoutPrefix, StringComparison.OrdinalIgnoreCase)));
			}
			if(cmd == null) {
				cmd = cmds.Commands.FirstOrDefault(c => c.Aliases.Any(ca => ca.Equals(input, StringComparison.OrdinalIgnoreCase)));
			}

			return Task.FromResult(cmd == null ? TypeReaderResult.FromError(CommandError.ParseFailed, "No such command found.") : TypeReaderResult.FromSuccess(cmd));
		}
	}

	public class CommandOrCrTypeReader : CommandTypeReader {
		public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services) {
			var crs = services.GetService<CustomReactionsService>();

			if(!crs.GlobalReactions.Any(x => x.Trigger.Equals(input, StringComparison.OrdinalIgnoreCase))) {
				var guild = context.Guild;
				if(guild != null && crs.GuildReactions.TryGetValue(guild.Id, out var crs2) && crs2.Concat(crs.GlobalReactions).Any(x => x.Trigger.Equals(input, StringComparison.OrdinalIgnoreCase))) {
					return TypeReaderResult.FromSuccess(new CommandOrCrInfo(input));
				} else {
					var cmd = await base.ReadAsync(context, input, services);
					return cmd.IsSuccess ? TypeReaderResult.FromSuccess(new CommandOrCrInfo(((CommandInfo)cmd.Values.First().Value).Name)) : TypeReaderResult.FromError(CommandError.ParseFailed, "No such command or cr found.");
				}
			} else {
				return TypeReaderResult.FromSuccess(new CommandOrCrInfo(input));
			}
		}
	}

	public class CommandOrCrInfo {
		public string Name { get; set; }

		public CommandOrCrInfo(string input) {
			Name = input;
		}
	}
}
