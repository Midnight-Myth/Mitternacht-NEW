using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.EntityFrameworkCore.Internal;
using Mitternacht.Common.TypeReaders.Models;

namespace Mitternacht.Common.TypeReaders {
	/// <summary>
	/// Used instead of bool for more flexible keywords for true/false only in the permission module
	/// </summary>
	public class PermissionActionTypeReader : TypeReader {
		private static readonly string[] EnabledStrings  = { "1", "t", "true", "enable", "enabled", "allow", "permit", "unban" };
		private static readonly string[] DisabledStrings = { "0", "f", "false", "deny", "disable", "disabled", "disallow", "ban" };

		public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
			=> Task.FromResult(EnabledStrings.Any(s => s.Equals(input, StringComparison.OrdinalIgnoreCase))
				? TypeReaderResult.FromSuccess(PermissionAction.Enable)
				: DisabledStrings.Any(s => s.Equals(input, StringComparison.OrdinalIgnoreCase))
				? TypeReaderResult.FromSuccess(PermissionAction.Disable)
				: TypeReaderResult.FromError(CommandError.ParseFailed, "Did not receive a valid boolean value"));
	}
}
