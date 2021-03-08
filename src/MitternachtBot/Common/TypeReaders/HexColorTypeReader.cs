using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace Mitternacht.Common.TypeReaders {
	public class HexColorTypeReader : TypeReader {
		public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services) {
			return Task.FromResult(HexColor.TryParse(input, out var hc)
				? TypeReaderResult.FromSuccess(hc)
				: TypeReaderResult.FromError(CommandError.ParseFailed, "input string didn't match hex color template"));
		}
	}
}