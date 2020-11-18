using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Modules.Utility.Services;
using Mitternacht.Database;
using System;
using System.Threading.Tasks;

namespace Mitternacht.Modules.Utility {
	public partial class Utility {
		public class VoiceStatsCommands : MitternachtSubmodule<VoiceStatsService> {
			private readonly IUnitOfWork uow;

			public VoiceStatsCommands(IUnitOfWork uow) {
				this.uow = uow;
			}

			[MitternachtCommand, Description, Usage, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task VoiceStats(IGuildUser user = null) {
				user ??= Context.User as IGuildUser;

				if(uow.VoiceChannelStats.TryGetTime(user.GuildId, user.Id, out var time)) {
					var timespan = TimeSpan.FromSeconds(time);
					await ConfirmLocalized("voicestats_time", user.ToString(), $"{(timespan.Days > 0 ? $"{timespan.Days}d" : "")}{(timespan.Hours > 0 ? $"{timespan:hh}h" : "")}{(timespan.Minutes > 0 ? $"{timespan:mm}min" : "")}{timespan:ss}s").ConfigureAwait(false);
				} else
					await ConfirmLocalized("voicestats_untracked", user.ToString()).ConfigureAwait(false);
			}

			[MitternachtCommand, Description, Usage, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOrGuildPermission(GuildPermission.Administrator)]
			public async Task VoiceStatsReset(IGuildUser user) {
				if(user == null)
					return;
				uow.VoiceChannelStats.Reset(user.GuildId, user.Id);
				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				await ConfirmLocalized("voicestats_reset", user.ToString()).ConfigureAwait(false);
			}
		}
	}
}
