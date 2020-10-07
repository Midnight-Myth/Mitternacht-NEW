using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Administration.Services;
using Mitternacht.Services;
using Mitternacht.Database;
using Mitternacht.Database.Models;

namespace Mitternacht.Modules.Administration {
	public partial class Administration {
		[Group]
		public class ServerGreetCommands : MitternachtSubmodule<ServerGreetService> {
			private readonly IUnitOfWork uow;

			public ServerGreetCommands(IUnitOfWork uow) {
				this.uow = uow;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.ManageGuild)]
			public async Task GreetDel(int timer = 30) {
				var gc = uow.GuildConfigs.For(Context.Guild.Id);
				gc.AutoDeleteGreetMessagesTimer = timer;

				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				if(timer > 0)
					await ReplyConfirmLocalized("greetdel_on", timer).ConfigureAwait(false);
				else
					await ReplyConfirmLocalized("greetdel_off").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.ManageGuild)]
			public async Task Greet() {
				var gc = uow.GuildConfigs.For(Context.Guild.Id);
				gc.SendChannelGreetMessage = !gc.SendChannelGreetMessage;
				gc.GreetMessageChannelId = Context.Channel.Id;

				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				if(gc.SendChannelGreetMessage)
					await ReplyConfirmLocalized("greet_on").ConfigureAwait(false);
				else
					await ReplyConfirmLocalized("greet_off").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.ManageGuild)]
			public async Task GreetMsg([Remainder] string text = null) {
				if(string.IsNullOrWhiteSpace(text)) {
					var channelGreetMessageText = uow.GuildConfigs.For(Context.Guild.Id).ChannelGreetMessageText;

					await ReplyConfirmLocalized("greetmsg_cur", channelGreetMessageText?.SanitizeMentions()).ConfigureAwait(false);
					return;
				} else {
					var gc = uow.GuildConfigs.For(Context.Guild.Id);
					gc.ChannelGreetMessageText = text.SanitizeMentions();

					await uow.SaveChangesAsync(false).ConfigureAwait(false);

					await ReplyConfirmLocalized("greetmsg_new").ConfigureAwait(false);
					if(!gc.SendChannelGreetMessage)
						await ReplyConfirmLocalized("greetmsg_enable", $"`{Prefix}greet`").ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.ManageGuild)]
			public async Task GreetDm() {
				var gc = uow.GuildConfigs.For(Context.Guild.Id);
				gc.SendDmGreetMessage = !gc.SendDmGreetMessage;

				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				if(gc.SendDmGreetMessage)
					await ReplyConfirmLocalized("greetdm_on").ConfigureAwait(false);
				else
					await ReplyConfirmLocalized("greetdm_off").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.ManageGuild)]
			public async Task GreetDmMsg([Remainder] string text = null) {
				if(string.IsNullOrWhiteSpace(text)) {
					var config = uow.GuildConfigs.For(Context.Guild.Id);

					await ReplyConfirmLocalized("greetdmmsg_cur", config.DmGreetMessageText?.SanitizeMentions()).ConfigureAwait(false);
					return;
				} else {
					var gc = uow.GuildConfigs.For(Context.Guild.Id);
					gc.DmGreetMessageText = text.SanitizeMentions();

					await uow.SaveChangesAsync(false).ConfigureAwait(false);

					await ReplyConfirmLocalized("greetdmmsg_new").ConfigureAwait(false);
					if(!gc.SendDmGreetMessage)
						await ReplyConfirmLocalized("greetdmmsg_enable", $"`{Prefix}greetdm`").ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.ManageGuild)]
			public async Task Bye() {
				var gc = uow.GuildConfigs.For(Context.Guild.Id);
				gc.SendChannelByeMessage = !gc.SendChannelByeMessage;
				gc.ByeMessageChannelId = Context.Channel.Id;
				
				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				if(gc.SendChannelByeMessage)
					await ReplyConfirmLocalized("bye_on").ConfigureAwait(false);
				else
					await ReplyConfirmLocalized("bye_off").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.ManageGuild)]
			public async Task ByeMsg([Remainder] string text = null) {
				if(string.IsNullOrWhiteSpace(text)) {
					var byeMessageText = uow.GuildConfigs.For(Context.Guild.Id).ChannelByeMessageText;

					await ReplyConfirmLocalized("byemsg_cur", byeMessageText?.SanitizeMentions()).ConfigureAwait(false);
					return;
				} else {
					var gc = uow.GuildConfigs.For(Context.Guild.Id);
					gc.ChannelByeMessageText = text.SanitizeMentions();

					await uow.SaveChangesAsync(false).ConfigureAwait(false);

					await ReplyConfirmLocalized("byemsg_new").ConfigureAwait(false);
					if(!gc.SendChannelByeMessage)
						await ReplyConfirmLocalized("byemsg_enable", $"`{Prefix}bye`").ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.ManageGuild)]
			public async Task ByeDel(int timer = 30) {
				var gc = uow.GuildConfigs.For(Context.Guild.Id);
				gc.AutoDeleteByeMessagesTimer = timer;

				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				if(timer > 0)
					await ReplyConfirmLocalized("byedel_on", timer).ConfigureAwait(false);
				else
					await ReplyConfirmLocalized("byedel_off").ConfigureAwait(false);
			}

		}
	}
}