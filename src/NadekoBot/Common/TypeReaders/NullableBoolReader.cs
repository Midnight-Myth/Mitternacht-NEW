using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace Mitternacht.Common.TypeReaders
{
    public class NullableBoolReader : TypeReader
    {
        public override Task<TypeReaderResult> Read(ICommandContext context, string input, IServiceProvider services) {
            input = input.Trim();
            bool? result;
            if (string.Equals(input, "true", StringComparison.OrdinalIgnoreCase)) result = true;
            else if (string.Equals(input, "false", StringComparison.OrdinalIgnoreCase))
                result = false;
            else if (string.Equals(input, "null", StringComparison.OrdinalIgnoreCase))
                result = null;
            else
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, ""));

            return Task.FromResult(TypeReaderResult.FromSuccess(result));
        }
    }
}