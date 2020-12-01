using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Mitternacht.Common.TypeReaders {
	public class ModerationPointsTypeReader : TypeReader {
		public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services){
			try {
				return Task.FromResult(TypeReaderResult.FromSuccess(ModerationPoints.FromString(input)));
			} catch(ArgumentException e) {
				return Task.FromResult(TypeReaderResult.FromError(e));
			}
		}
	}
}
