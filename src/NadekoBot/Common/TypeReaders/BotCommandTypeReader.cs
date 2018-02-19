﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Mitternacht.Modules.CustomReactions.Services;
using Mitternacht.Services;

namespace Mitternacht.Common.TypeReaders
{
    public class CommandTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            var cmds = ((INServiceProvider)services).GetService<CommandService>();
            var cmdHandler = ((INServiceProvider)services).GetService<CommandHandler>();
            input = input.ToUpperInvariant();
            var prefix = cmdHandler.GetPrefix(context.Guild);
            if (!input.StartsWith(prefix.ToUpperInvariant(), StringComparison.Ordinal))
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "No such command found."));

            input = input.Substring(prefix.Length);

            var cmd = cmds.Commands.FirstOrDefault(c => 
                c.Aliases.Select(a => a.ToUpperInvariant()).Contains(input));
            return Task.FromResult(cmd == null ? TypeReaderResult.FromError(CommandError.ParseFailed, "No such command found.") : TypeReaderResult.FromSuccess(cmd));
        }
    }

    public class CommandOrCrTypeReader : CommandTypeReader
    {
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            input = input.ToUpperInvariant();

            var crs = ((INServiceProvider)services).GetService<CustomReactionsService>();

            if (crs.GlobalReactions.Any(x => x.Trigger.ToUpperInvariant() == input))
            {
                return TypeReaderResult.FromSuccess(new CommandOrCrInfo(input));
            }
            var guild = context.Guild;
            if (guild != null)
            {
                if (crs.GuildReactions.TryGetValue(guild.Id, out var crs2))
                {
                    if (crs2.Concat(crs.GlobalReactions).Any(x => x.Trigger.ToUpperInvariant() == input))
                    {
                        return TypeReaderResult.FromSuccess(new CommandOrCrInfo(input));
                    }
                }
            }

            var cmd = await base.ReadAsync(context, input, services);
            return cmd.IsSuccess ? TypeReaderResult.FromSuccess(new CommandOrCrInfo(((CommandInfo)cmd.Values.First().Value).Name)) : TypeReaderResult.FromError(CommandError.ParseFailed, "No such command or cr found.");
        }
    }

    public class CommandOrCrInfo
    {
        public string Name { get; set; }

        public CommandOrCrInfo(string input)
        {
            Name = input;
        }
    }
}
