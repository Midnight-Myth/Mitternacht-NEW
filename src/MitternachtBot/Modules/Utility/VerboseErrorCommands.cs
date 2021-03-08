using System.Threading.Tasks;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Modules.Utility.Services;
using Mitternacht.Database;

namespace Mitternacht.Modules.Utility {
	public partial class Utility {
		[Group]
		public class VerboseErrorCommands : MitternachtSubmodule<VerboseErrorsService> {
			private readonly IUnitOfWork _uow;

			public VerboseErrorCommands(IUnitOfWork uow) {
				_uow = uow;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(Discord.GuildPermission.ManageMessages)]
			public async Task VerboseError() {
				var gc = _uow.GuildConfigs.For(Context.Guild.Id);
				gc.VerboseErrors = !gc.VerboseErrors;
				await _uow.SaveChangesAsync(false).ConfigureAwait(false);

				if(gc.VerboseErrors)
					await ReplyConfirmLocalized("verbose_errors_enabled").ConfigureAwait(false);
				else
					await ReplyConfirmLocalized("verbose_errors_disabled").ConfigureAwait(false);
			}
		}
	}
}
