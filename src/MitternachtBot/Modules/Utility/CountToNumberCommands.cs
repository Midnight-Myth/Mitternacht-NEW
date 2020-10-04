using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Modules.Utility.Services;
using Mitternacht.Database;

namespace Mitternacht.Modules.Utility {
	public partial class Utility {
		public class CountToNumberCommands : MitternachtSubmodule<CountToNumberService> {
			private readonly IUnitOfWork _uow;

			public CountToNumberCommands(IUnitOfWork uow) {
				_uow = uow;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[Priority(2)]
			public async Task CountToNumberChannel() {
				var channelId = _uow.GuildConfigs.For(Context.Guild.Id).CountToNumberChannelId;

				if(channelId.HasValue) {
					var channel = await Context.Guild.GetTextChannelAsync(channelId.Value).ConfigureAwait(false);

					if(channel == null) {
						await ErrorLocalized("counttonumber_channel_not_existing").ConfigureAwait(false);
					} else {
						await ConfirmLocalized("counttonumber_channel_current", channel.Mention).ConfigureAwait(false);
					}
				} else {
					await ConfirmLocalized("counttonumber_channel_not_set").ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[Priority(1)]
			public async Task CountToNumberChannel(ITextChannel channel) {
				var gc = _uow.GuildConfigs.For(Context.Guild.Id);

				if(gc.CountToNumberChannelId != channel.Id) {
					gc.CountToNumberChannelId = channel.Id;
					await _uow.SaveChangesAsync(false).ConfigureAwait(false);

					await ConfirmLocalized("counttonumber_channel_set", channel.Mention).ConfigureAwait(false);
				} else {
					await ErrorLocalized("counttonumber_channel_already_set", channel.Mention).ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[Priority(0)]
			public async Task CountToNumberChannel(string channel) {
				if(channel.Equals("null", StringComparison.OrdinalIgnoreCase)) {
					var gc = _uow.GuildConfigs.For(Context.Guild.Id);

					if(gc.CountToNumberChannelId != null) {
						gc.CountToNumberChannelId = null;
						await _uow.SaveChangesAsync(false).ConfigureAwait(false);

						await ConfirmLocalized("counttonumber_channel_removed").ConfigureAwait(false);
					} else {
						await ErrorLocalized("counttonumber_channel_already_removed").ConfigureAwait(false);
					}
				}
			}


			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task CountToNumberMessageChance() {
				var chance = _uow.GuildConfigs.For(Context.Guild.Id).CountToNumberMessageChance;

				await ConfirmLocalized("counttonumber_chance_current", chance).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task CountToNumberMessageChance(double chance) {
				var gc = _uow.GuildConfigs.For(Context.Guild.Id);

				if(gc.CountToNumberMessageChance != chance) {
					gc.CountToNumberMessageChance = chance;
					await _uow.SaveChangesAsync(false).ConfigureAwait(false);

					await ConfirmLocalized("counttonumber_chance_set", chance).ConfigureAwait(false);
				} else {
					await ErrorLocalized("counttonumber_chance_already_set", chance).ConfigureAwait(false);
				}
			}
		}
	}
}
