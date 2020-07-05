using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Extensions;
using Mitternacht.Modules.Help.Services;
using Mitternacht.Services;

namespace Mitternacht.Modules.Utility.Services {
	public class VerboseErrorsService : IMService {
		private readonly DbService _db;
		private readonly HelpService _hs;

		public VerboseErrorsService(DbService db, CommandHandler ch, HelpService hs) {
			_db = db;
			_hs = hs;

			ch.CommandErrored += LogVerboseError;
		}

		private async Task LogVerboseError(CommandInfo cmd, ITextChannel channel, string reason) {
			if(channel == null)
				return;

			using var uow = _db.UnitOfWork;
			
			if(!uow.GuildConfigs.For(channel.GuildId).VerboseErrors)
				return;

			try {
				var embed = _hs.GetCommandHelp(cmd, channel.Guild)
					.WithTitle("Command Error")
					.WithDescription(reason)
					.WithErrorColor();

				await channel.EmbedAsync(embed).ConfigureAwait(false);
			} catch { }
		}
	}
}
