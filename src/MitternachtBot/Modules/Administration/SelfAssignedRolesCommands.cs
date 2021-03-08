using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Common.Attributes;
using Mitternacht.Common.Collections;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Database;
using Mitternacht.Database.Models;

namespace Mitternacht.Modules.Administration {
	public partial class Administration {
		[Group]
		public class SelfAssignedRolesCommands : MitternachtSubmodule {
			private readonly IUnitOfWork uow;

			public SelfAssignedRolesCommands(IUnitOfWork uow) {
				this.uow = uow;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.ManageMessages)]
			public async Task AdSarm() {
				var config = uow.GuildConfigs.For(Context.Guild.Id);
				var newval = config.AutoDeleteSelfAssignedRoleMessages = !config.AutoDeleteSelfAssignedRoleMessages;
				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				await Context.Channel.SendConfirmAsync($"â„¹ï¸ Automatic deleting of `iam` and `iamn` confirmations has been {(newval ? "**enabled**" : "**disabled**")}.")
							 .ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.ManageRoles)]
			public async Task Asar([Remainder] IRole role) {
				var guser = (IGuildUser)Context.User;
				if(Context.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= role.Position)
					return;

				string msg;
				var error = false;

				var roles = uow.SelfAssignedRoles.GetFromGuild(Context.Guild.Id);
				if(roles.Any(s => s.RoleId == role.Id && s.GuildId == role.Guild.Id)) {
					msg = GetText("role_in_list", Format.Bold(role.Name));
					error = true;
				} else {
					uow.SelfAssignedRoles.Add(new SelfAssignedRole {
						RoleId = role.Id,
						GuildId = role.Guild.Id
					});
					await uow.SaveChangesAsync(false);
					msg = GetText("role_added", Format.Bold(role.Name));
				}

				if(error)
					await Context.Channel.SendErrorAsync(msg).ConfigureAwait(false);
				else
					await Context.Channel.SendConfirmAsync(msg).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.ManageRoles)]
			public async Task Rsar([Remainder] IRole role) {
				var guser = (IGuildUser)Context.User;
				if(Context.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= role.Position)
					return;

				var success = uow.SelfAssignedRoles.DeleteByGuildAndRoleId(role.Guild.Id, role.Id);
				await uow.SaveChangesAsync(false);

				if(!success) {
					await ReplyErrorLocalized("self_assign_not", Format.Bold(role.Name)).ConfigureAwait(false);
					return;
				}
				await ReplyConfirmLocalized("self_assign_rem", Format.Bold(role.Name)).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task Lsar(int page = 1) {
				var roleModels = uow.SelfAssignedRoles.GetFromGuild(Context.Guild.Id).ToList();

				if(--page < 0) {

					var rms = (from rm in roleModels
							let role = Context.Guild.Roles.FirstOrDefault(r => r.Id == rm.RoleId)
							where role != null
							select role).ToList();

					uow.SelfAssignedRoles.RemoveRange(roleModels.Where(rm => rms.All(r => r.Id != rm.RoleId)).ToArray());
					await uow.SaveChangesAsync(false);

					await Context.Channel.SendMessageAsync("", embed: new EmbedBuilder().WithTitle(GetText("self_assign_list", rms.Count)).WithDescription(rms.Aggregate("", (s, r) => s + Format.Bold(r.Name) + ", ", s => s.Substring(0, s.Length - 2))).WithOkColor().Build());
					return;
				}

				var toRemove = new ConcurrentHashSet<SelfAssignedRole>();
				var roles = new List<string>();
				var roleCnt = 0;

				foreach(var roleModel in roleModels) {
					var role = Context.Guild.Roles.FirstOrDefault(r => r.Id == roleModel.RoleId);
					if(role == null) {
						toRemove.Add(roleModel);
						uow.SelfAssignedRoles.Remove(roleModel);
					} else {
						roles.Add(Format.Bold(role.Name));
						roleCnt++;
					}
				}
				roles.AddRange(toRemove.Select(role => GetText("role_clean", role.RoleId)));
				await uow.SaveChangesAsync(false);

				const int elementsPerPage = 10;

				await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page, currentPage => new EmbedBuilder()
					.WithTitle(GetText("self_assign_list", roleCnt))
					.WithDescription(string.Join("\n", roles.Skip(currentPage * elementsPerPage).Take(elementsPerPage)))
					.WithOkColor(), (int)Math.Ceiling(roles.Count * 1d / elementsPerPage), reactUsers: new[] { Context.User as IGuildUser });
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.ManageRoles)]
			public async Task Tesar() {
				var config = uow.GuildConfigs.For(Context.Guild.Id);

				var areExclusive = config.ExclusiveSelfAssignedRoles = !config.ExclusiveSelfAssignedRoles;
				await uow.SaveChangesAsync(false);

				if(areExclusive)
					await ReplyConfirmLocalized("self_assign_excl").ConfigureAwait(false);
				else
					await ReplyConfirmLocalized("self_assign_no_excl").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task Iam([Remainder] IRole role) {
				var guildUser = (IGuildUser)Context.User;

				var conf = uow.GuildConfigs.For(Context.Guild.Id);
				var roles = uow.SelfAssignedRoles.GetFromGuild(Context.Guild.Id);

				if(!roles.Any(r => r.RoleId == role.Id)) {
					await ReplyErrorLocalized("self_assign_not", Format.Bold(role.Name)).ConfigureAwait(false);
					return;
				}
				if(guildUser.RoleIds.Contains(role.Id)) {
					await ReplyErrorLocalized("self_assign_already", Format.Bold(role.Name)).ConfigureAwait(false);
					return;
				}

				var roleIds = roles.Select(x => x.RoleId).ToArray();
				if(conf.ExclusiveSelfAssignedRoles) {
					var sameRoles = guildUser.RoleIds.Where(r => roleIds.Contains(r));

					foreach(var roleId in sameRoles) {
						var sameRole = Context.Guild.GetRole(roleId);
						if(sameRole == null)
							continue;
						try {
							await guildUser.RemoveRoleAsync(sameRole).ConfigureAwait(false);
							await Task.Delay(300).ConfigureAwait(false);
						} catch(Exception ex) {
							_log.Warn(ex);
						}
					}
				}
				try {
					await guildUser.AddRoleAsync(role).ConfigureAwait(false);
				} catch(Exception ex) {
					await ReplyErrorLocalized("self_assign_perms").ConfigureAwait(false);
					_log.Info(ex);
					return;
				}
				var msg = await ReplyConfirmLocalized("self_assign_success", Format.Bold(role.Name)).ConfigureAwait(false);

				if(conf.AutoDeleteSelfAssignedRoleMessages) {
					msg.DeleteAfter(3);
					Context.Message.DeleteAfter(3);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task Iamnot([Remainder] IRole role) {
				var guildUser = (IGuildUser)Context.User;

				var autoDeleteSelfAssignedRoleMessages = uow.GuildConfigs.For(Context.Guild.Id).AutoDeleteSelfAssignedRoleMessages;
				var roles = uow.SelfAssignedRoles.GetFromGuild(Context.Guild.Id);

				if(!roles.Any(r => r.RoleId == role.Id)) {
					await ReplyErrorLocalized("self_assign_not", Format.Bold(role.Name)).ConfigureAwait(false);
					return;
				}
				if(!guildUser.RoleIds.Contains(role.Id)) {
					await ReplyErrorLocalized("self_assign_not_have", Format.Bold(role.Name)).ConfigureAwait(false);
					return;
				}
				try {
					await guildUser.RemoveRoleAsync(role).ConfigureAwait(false);
				} catch(Exception) {
					await ReplyErrorLocalized("self_assign_perms").ConfigureAwait(false);
					return;
				}
				var msg = await ReplyConfirmLocalized("self_assign_remove", Format.Bold(role.Name)).ConfigureAwait(false);

				if(autoDeleteSelfAssignedRoleMessages) {
					msg.DeleteAfter(3);
					Context.Message.DeleteAfter(3);
				}
			}
		}
	}
}