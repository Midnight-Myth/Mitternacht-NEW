using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace Mitternacht.Common.TypeReaders {
	public class ModuleTypeReader : TypeReader {
		private readonly CommandService _cmds;

		public ModuleTypeReader(CommandService cmds) {
			_cmds = cmds;
		}

		public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services) {
			var module = _cmds.Modules.Where(m => !m.IsSubmodule).FirstOrDefault(m => m.Name.Equals(input,StringComparison.OrdinalIgnoreCase));

			return Task.FromResult(module != null ? TypeReaderResult.FromSuccess(module) : TypeReaderResult.FromError(CommandError.ParseFailed, "No such module found."));
		}
	}

	public class ModuleOrCrTypeReader : TypeReader {
		private readonly CommandService _cmds;

		public ModuleOrCrTypeReader(CommandService cmds) {
			_cmds = cmds;
		}

		public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider _) {
			var module = _cmds.Modules.Where(m => !m.IsSubmodule).FirstOrDefault(m => m.Name.Equals(input,StringComparison.OrdinalIgnoreCase));

			return module != null || input.Equals("actualcustomreactions", StringComparison.OrdinalIgnoreCase)
				? Task.FromResult(TypeReaderResult.FromSuccess(new ModuleOrCrInfo {
					Name = input,
				}))
				: Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "No such module found."));
		}
	}

	public class ModuleOrCrInfo {
		public string Name { get; set; }
	}
}
