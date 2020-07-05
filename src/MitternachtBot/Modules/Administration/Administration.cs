using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common;
using Mitternacht.Common.Attributes;
using Mitternacht.Common.Replacements;
using Mitternacht.Extensions;
using Mitternacht.Modules.Administration.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Administration {
	public partial class Administration : MitternachtTopLevelModule<AdministrationService> {
		private readonly IUnitOfWork uow;

		public Administration(IUnitOfWork uow) {
			this.uow = uow;
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[RequireUserPermission(GuildPermission.Administrator)]
		[RequireBotPermission(GuildPermission.ManageMessages)]
		public async Task Delmsgoncmd() {
			var gc = uow.GuildConfigs.For(Context.Guild.Id);
			gc.DeleteMessageOnCommand = !gc.DeleteMessageOnCommand;
			await uow.SaveChangesAsync(false);

			if(gc.DeleteMessageOnCommand) {
				await ReplyConfirmLocalized("delmsg_on").ConfigureAwait(false);
			} else {
				await ReplyConfirmLocalized("delmsg_off").ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		[RequireBotPermission(GuildPermission.ManageRoles)]
		public async Task Setrole(IGuildUser usr, [Remainder] IRole role) {
			var guser = (IGuildUser)Context.User;
			var maxRole = guser.GetRoles().Max(x => x.Position);
			if(Context.User.Id != Context.Guild.OwnerId && (maxRole <= role.Position || maxRole <= usr.GetRoles().Max(x => x.Position)))
				return;
			try {
				await usr.AddRoleAsync(role).ConfigureAwait(false);
				await ReplyConfirmLocalized("setrole", Format.Bold(role.Name), Format.Bold(usr.ToString()))
					.ConfigureAwait(false);
			} catch(Exception ex) {
				await ReplyErrorLocalized("setrole_err").ConfigureAwait(false);
				_log.Info(ex);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		[RequireBotPermission(GuildPermission.ManageRoles)]
		public async Task Removerole(IGuildUser usr, [Remainder] IRole role) {
			var guser = (IGuildUser)Context.User;
			if(Context.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= usr.GetRoles().Max(x => x.Position))
				return;
			try {
				await usr.RemoveRoleAsync(role).ConfigureAwait(false);
				await ReplyConfirmLocalized("remrole", Format.Bold(role.Name), Format.Bold(usr.ToString())).ConfigureAwait(false);
			} catch {
				await ReplyErrorLocalized("remrole_err").ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[RequireUserPermission(GuildPermission.Administrator)]
		[RequireBotPermission(GuildPermission.ManageRoles)]
		public async Task AddRoleForAll([Remainder] IRole role) {
			try {
				var users = await Context.Guild.GetUsersAsync().ConfigureAwait(false);
				foreach(var user in users) {
					await user.AddRoleAsync(role).ConfigureAwait(false);
					await Task.Delay(50);
				}

				await ReplyConfirmLocalized("addroleforall", Format.Bold(role.Name)).ConfigureAwait(false);
			} catch(Exception) {
				await ReplyErrorLocalized("addroleforall_error", Format.Bold(role.Name)).ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[RequireUserPermission(GuildPermission.Administrator)]
		[RequireBotPermission(GuildPermission.ManageRoles)]
		public async Task RemoveRoleForAll([Remainder] IRole role) {
			try {
				var users = await Context.Guild.GetUsersAsync().ConfigureAwait(false);
				foreach(var user in users) {
					await user.RemoveRoleAsync(role).ConfigureAwait(false);
					await Task.Delay(50);
				}

				await ReplyConfirmLocalized("removeroleforall", Format.Bold(role.Name)).ConfigureAwait(false);
			} catch(Exception) {
				await ReplyErrorLocalized("removeroleforall_error", Format.Bold(role.Name)).ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		[RequireBotPermission(GuildPermission.ManageRoles)]
		public async Task RenameRole(IRole roleToEdit, string newname) {
			var guser = (IGuildUser)Context.User;
			if(Context.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= roleToEdit.Position)
				return;
			try {
				if(roleToEdit.Position > (await Context.Guild.GetCurrentUserAsync().ConfigureAwait(false)).GetRoles().Max(r => r.Position)) {
					await ReplyErrorLocalized("renrole_perms").ConfigureAwait(false);
					return;
				}
				await roleToEdit.ModifyAsync(g => g.Name = newname).ConfigureAwait(false);
				await ReplyConfirmLocalized("renrole").ConfigureAwait(false);
			} catch(Exception) {
				await ReplyErrorLocalized("renrole_err").ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		[RequireBotPermission(GuildPermission.ManageRoles)]
		public async Task RemoveAllRoles([Remainder] IGuildUser user) {
			var guser = (IGuildUser)Context.User;

			var userRoles = user.GetRoles().Except(new[] { guser.Guild.EveryoneRole }).ToList();
			if(user.Id == Context.Guild.OwnerId || (Context.User.Id != Context.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= userRoles.Max(x => x.Position)))
				return;
			try {
				await user.RemoveRolesAsync(userRoles).ConfigureAwait(false);
				await ReplyConfirmLocalized("rar", Format.Bold(user.ToString())).ConfigureAwait(false);
			} catch(Exception) {
				await ReplyErrorLocalized("rar_err").ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		[RequireBotPermission(GuildPermission.ManageRoles)]
		public async Task CreateRole([Remainder] string roleName = null) {
			if(string.IsNullOrWhiteSpace(roleName))
				return;

			var r = await Context.Guild.CreateRoleAsync(roleName, isMentionable: false).ConfigureAwait(false);
			await ReplyConfirmLocalized("cr", Format.Bold(r.Name)).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		[RequireBotPermission(GuildPermission.ManageRoles)]
		public async Task RoleHoist(IRole role) {
			await role.ModifyAsync(r => r.Hoist = !role.IsHoisted).ConfigureAwait(false);
			await ReplyConfirmLocalized("rh", Format.Bold(role.Name), Format.Bold(role.IsHoisted.ToString())).ConfigureAwait(false);
		}


		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		[RequireBotPermission(GuildPermission.ManageRoles)]
		public async Task RoleColor(IRole role, HexColor hc) {
			if(role == null) {
				await ReplyErrorLocalized("rc_not_exist").ConfigureAwait(false);
				return;
			}

			var gu = (IGuildUser) Context.User;
			if(!gu.GuildPermissions.Administrator && (!gu.RoleIds.Any() || gu.GetRoles().Max(r => r.Position) < role.Position)) {
				await ReplyErrorLocalized("rc_perms_pos").ConfigureAwait(false);
				return;
			}

			try {
				await role.ModifyAsync(r => r.Color = hc).ConfigureAwait(false);
				await ReplyConfirmLocalized("rc", Format.Bold(role.Name), Format.Bold(hc.ToString())).ConfigureAwait(false);
			} catch(Exception) {
				await ReplyErrorLocalized("rc_perms").ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		[RequireBotPermission(GuildPermission.ManageRoles)]
		public async Task RoleColor(IRole role, byte red, byte green, byte blue)
			=> await RoleColor(role, new HexColor(red, green, blue)).ConfigureAwait(false);


		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[RequireUserPermission(GuildPermission.DeafenMembers)]
		[RequireBotPermission(GuildPermission.DeafenMembers)]
		public async Task Deafen(params IGuildUser[] users) {
			if(!users.Any())
				return;
			foreach(var u in users) {
				try {
					await u.ModifyAsync(usr => usr.Deaf = true).ConfigureAwait(false);
				} catch {
					// ignored
				}
			}
			await ReplyConfirmLocalized("deafen").ConfigureAwait(false);

		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[RequireUserPermission(GuildPermission.DeafenMembers)]
		[RequireBotPermission(GuildPermission.DeafenMembers)]
		public async Task UnDeafen(params IGuildUser[] users) {
			if(!users.Any())
				return;

			foreach(var u in users) {
				try {
					await u.ModifyAsync(usr => usr.Deaf = false).ConfigureAwait(false);
				} catch {
					// ignored
				}
			}
			await ReplyConfirmLocalized("undeafen").ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[RequireUserPermission(GuildPermission.ManageChannels)]
		[RequireBotPermission(GuildPermission.ManageChannels)]
		public async Task DelVoiChanl([Remainder] IVoiceChannel voiceChannel) {
			await voiceChannel.DeleteAsync().ConfigureAwait(false);
			await ReplyConfirmLocalized("delvoich", Format.Bold(voiceChannel.Name)).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[RequireUserPermission(GuildPermission.ManageChannels)]
		[RequireBotPermission(GuildPermission.ManageChannels)]
		public async Task CreatVoiChanl([Remainder] string channelName) {
			var ch = await Context.Guild.CreateVoiceChannelAsync(channelName).ConfigureAwait(false);
			await ReplyConfirmLocalized("createvoich", Format.Bold(ch.Name)).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[RequireUserPermission(GuildPermission.ManageChannels)]
		[RequireBotPermission(GuildPermission.ManageChannels)]
		public async Task DelTxtChanl([Remainder] ITextChannel toDelete) {
			await toDelete.DeleteAsync().ConfigureAwait(false);
			await ReplyConfirmLocalized("deltextchan", Format.Bold(toDelete.Name)).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[RequireUserPermission(GuildPermission.ManageChannels)]
		[RequireBotPermission(GuildPermission.ManageChannels)]
		public async Task CreaTxtChanl([Remainder] string channelName) {
			var txtCh = await Context.Guild.CreateTextChannelAsync(channelName).ConfigureAwait(false);
			await ReplyConfirmLocalized("createtextchan", Format.Bold(txtCh.Name)).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[RequireUserPermission(GuildPermission.ManageChannels)]
		[RequireBotPermission(GuildPermission.ManageChannels)]
		public async Task SetTopic([Remainder] string topic = null) {
			var channel = (ITextChannel)Context.Channel;
			topic ??= "";
			await channel.ModifyAsync(c => c.Topic = topic);
			await ReplyConfirmLocalized("set_topic").ConfigureAwait(false);

		}
		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[RequireUserPermission(GuildPermission.ManageChannels)]
		[RequireBotPermission(GuildPermission.ManageChannels)]
		public async Task SetChanlName([Remainder] string name) {
			var channel = (ITextChannel)Context.Channel;
			await channel.ModifyAsync(c => c.Name = name).ConfigureAwait(false);
			await ReplyConfirmLocalized("set_channel_name").ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[RequireUserPermission(GuildPermission.MentionEveryone)]
		public async Task MentionRole(params IRole[] roles) {
			var send = "❕" + GetText("menrole", Context.User.Mention);
			foreach(var role in roles) {
				send += $"\n**{role.Name}**\n";
				send += string.Join(", ", (await Context.Guild.GetUsersAsync())
					.Where(u => u.GetRoles().Contains(role))
					.Take(50).Select(u => u.Mention));
			}

			while(send.Length > 2000) {
				var curstr = send.Substring(0, 2000);
				await Context.Channel.SendMessageAsync(curstr.Substring(0,
						curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1)).ConfigureAwait(false);
				send = curstr.Substring(curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1) +
					   send.Substring(2000);
			}
			await Context.Channel.SendMessageAsync(send).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		public async Task Donators() {
			var donatorsOrdered = uow.Donators.GetDonatorsOrdered();

			await Context.Channel.SendConfirmAsync(string.Join("⭐", donatorsOrdered.Select(d => d.Name)), GetText("donators")).ConfigureAwait(false);
			//await Context.Channel.SendConfirmAsync("Patreon supporters", string.Join("⭐", usrs.Select(d => d.Username))).ConfigureAwait(false);
		}


		[MitternachtCommand, Usage, Description, Aliases]
		[OwnerOnly]
		public async Task Donadd(IUser donator, int amount) {
			var don = uow.Donators.AddOrUpdateDonator(donator.Id, donator.Username, amount);
			await uow.SaveChangesAsync(false);
			await ReplyConfirmLocalized("donadd", don.Amount).ConfigureAwait(false);
		}


		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOnly]
		[Priority(0)]
		public async Task Edit(IMessageChannel channel, ulong messageId, [Remainder] string text) {
			if(string.IsNullOrWhiteSpace(text) || channel == null)
				return;

			var imsg = await channel.GetMessageAsync(messageId).ConfigureAwait(false);
			if(!(imsg is IUserMessage msg) || imsg.Author.Id != Context.Client.CurrentUser.Id)
				return;

			var rep = new ReplacementBuilder()
					.WithDefault(Context)
					.Build();

			if(CREmbed.TryParse(text, out var crembed)) {
				rep.Replace(crembed);
				await msg.ModifyAsync(x => {
					x.Embed = crembed.ToEmbedBuilder().Build();
					x.Content = crembed.PlainText?.SanitizeMentions() ?? "";
				}).ConfigureAwait(false);
			} else {
				await msg.ModifyAsync(x => x.Content = text.SanitizeMentions())
					.ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOnly]
		[Priority(1)]
		public async Task Edit(ulong messageId, [Remainder] string text)
			=> await Edit(Context.Channel, messageId, text).ConfigureAwait(false);
	}
}
