using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Mitternacht.Extensions;

namespace Mitternacht.Common.TypeReaders
{
    public class ModuleTypeReader : TypeReader
    {
        private readonly CommandService _cmds;

        public ModuleTypeReader(CommandService cmds)
        {
            _cmds = cmds;
        }

        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider _)
        {
            input = input.ToUpperInvariant();
            var module = _cmds.Modules.GroupBy(m => m.GetTopLevelModule()).FirstOrDefault(m => m.Key.Name.ToUpperInvariant() == input)?.Key;
            return Task.FromResult(module == null ? TypeReaderResult.FromError(CommandError.ParseFailed, "No such module found.") : TypeReaderResult.FromSuccess(module));
        }
    }

    public class ModuleOrCrTypeReader : TypeReader
    {
        private readonly CommandService _cmds;

        public ModuleOrCrTypeReader(CommandService cmds)
        {
            _cmds = cmds;
        }

        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider _)
        {
            input = input.ToLowerInvariant();
            var module = _cmds.Modules.GroupBy(m => m.GetTopLevelModule()).FirstOrDefault(m => m.Key.Name.ToLowerInvariant() == input)?.Key;
            if (module == null && input != "actualcustomreactions")
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "No such module found."));

            return Task.FromResult(TypeReaderResult.FromSuccess(new ModuleOrCrInfo
            {
                Name = input,
            }));
        }
    }

    public class ModuleOrCrInfo
    {
        public string Name { get; set; }
    }
}
