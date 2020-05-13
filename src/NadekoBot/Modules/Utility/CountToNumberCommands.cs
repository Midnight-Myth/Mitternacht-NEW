using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Modules.Utility.Services;

namespace Mitternacht.Modules.Utility {
	public partial class Utility {
		public class CountToNumberCommands : MitternachtSubmodule<CountToNumberService> {

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[Priority(2)]
			public async Task CountToNumberChannel() {
				var channelId = Service.GetCountToNumberChannelId(Context.Guild.Id);
				if(channelId.HasValue) {
					var channel = await Context.Guild.GetTextChannelAsync(channelId.Value);
					if(channel == null)
						await ErrorLocalized("counttonumber_channel_not_existing");
					else
						await ConfirmLocalized("counttonumber_channel_current", channel.Mention);
				} else {
					await ConfirmLocalized("counttonumber_channel_not_set");
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[Priority(1)]
			public async Task CountToNumberChannel(ITextChannel channel) {
				var success = Service.SetCountToNumberChannel(channel.Guild, channel);
				if(success)
					await ConfirmLocalized("counttonumber_channel_set", channel.Mention);
				else
					await ErrorLocalized("counttonumber_channel_already_set", channel.Mention);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[Priority(0)]
			public async Task CountToNumberChannel(string channel) {
				if(channel.Equals("null", StringComparison.OrdinalIgnoreCase)) {
					var success = Service.SetCountToNumberChannel(Context.Guild, null);
					if(success)
						await ConfirmLocalized("counttonumber_channel_removed");
					else
						await ErrorLocalized("counttonumber_channel_already_removed");
				}
			}


			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task CountToNumberMessageChance() {
				var chance = Service.GetCountToNumberMessageChance(Context.Guild.Id);
				await ConfirmLocalized("counttonumber_chance_current", chance);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task CountToNumberMessageChance(double chance) {
				var success = Service.SetCountToNumberMessageChance(Context.Guild.Id, chance);
				if(success)
					await ConfirmLocalized("counttonumber_chance_set", chance);
				else
					await ErrorLocalized("counttonumber_chance_already_set", chance);
			}
		}
	}
}
