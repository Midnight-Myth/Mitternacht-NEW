using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Common.Attributes;
using Mitternacht.Common.TypeReaders;
using Mitternacht.Common.TypeReaders.Models;
using Mitternacht.Extensions;
using Mitternacht.Modules.Permissions.Common;
using Mitternacht.Modules.Permissions.Services;
using Mitternacht.Database;
using Mitternacht.Database.Models;

namespace Mitternacht.Modules.Permissions {
	public partial class Permissions : MitternachtTopLevelModule<PermissionService> {
		private readonly IUnitOfWork uow;

		public Permissions(IUnitOfWork uow) {
			this.uow = uow;
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task Verbose(PermissionAction action) {
			var config = uow.GuildConfigs.GcWithPermissionsv2For(Context.Guild.Id);
			config.VerbosePermissions = action.Value;
			await uow.SaveChangesAsync(false).ConfigureAwait(false);
			Service.UpdateCache(config);

			if(action.Value) {
				await ReplyConfirmLocalized("verbose_true").ConfigureAwait(false);
			} else {
				await ReplyConfirmLocalized("verbose_false").ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task PermRole([Remainder] IRole role = null) {
			if(role != null && role == role.Guild.EveryoneRole)
				return;

			var config = uow.GuildConfigs.GcWithPermissionsv2For(Context.Guild.Id);
			if(role == null) {
				await ReplyConfirmLocalized("permrole", Format.Bold(config.PermissionRole)).ConfigureAwait(false);
				return;
			}
			config.PermissionRole = role.Name.Trim();
			await uow.SaveChangesAsync(false).ConfigureAwait(false);
			Service.UpdateCache(config);

			await ReplyConfirmLocalized("permrole_changed", Format.Bold(role.Name)).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task ListPerms(int page = 1) {
			const int permsPerPage = 20;

			IList<Permissionv2> perms = Service.Cache.TryGetValue(Context.Guild.Id, out var permCache) ? permCache.Permissions.Source.ToList() : Permissionv2.GetDefaultPermlist;
			var pageCount = (int)Math.Ceiling(perms.Count * 1d / permsPerPage);
			page--;
			if(page < 0)
				page = 0;
			if(page >= pageCount)
				page = pageCount-1;

			await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page, currentPage =>
				   new EmbedBuilder()
					   .WithOkColor()
					   .WithTitle(GetText("page"))
					   .WithDescription(string.Join("\n", perms.Reverse()
						   .Skip(permsPerPage * currentPage)
						   .Take(permsPerPage)
						   .Select(p => $"`{p.Index + 1}.` {Format.Bold(p.GetCommand(Prefix, (SocketGuild)Context.Guild))}{(p.Index == 0 ? $" [{GetText("uneditable")}]" : "")}"))),
				pageCount, reactUsers: new[] { (IGuildUser)Context.User }).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task RemovePerm(int index) {
			index -= 1;
			if(index < 0)
				return;
			try {
				var config = uow.GuildConfigs.GcWithPermissionsv2For(Context.Guild.Id);
				var permsCol = new PermissionsCollection(config.Permissions);
				var p = permsCol[index];
				permsCol.RemoveAt(index);
				uow.Context.Remove(p);
				await uow.SaveChangesAsync(false).ConfigureAwait(false);
				Service.UpdateCache(config);

				await ReplyConfirmLocalized("removed", index + 1, Format.Code(p.GetCommand(Prefix, (SocketGuild)Context.Guild))).ConfigureAwait(false);
			} catch(IndexOutOfRangeException) {
				await ReplyErrorLocalized("perm_out_of_range").ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task MovePerm(int from, int to) {
			from -= 1;
			to -= 1;
			if(!(from == to || from < 0 || to < 0)) {
				try {
					var config = uow.GuildConfigs.GcWithPermissionsv2For(Context.Guild.Id);
					var permsCol = new PermissionsCollection(config.Permissions);

					var fromFound = from < permsCol.Count;
					var toFound = to < permsCol.Count;

					if(!fromFound) {
						await ReplyErrorLocalized("not_found", ++from).ConfigureAwait(false);
						return;
					}

					if(!toFound) {
						await ReplyErrorLocalized("not_found", ++to).ConfigureAwait(false);
						return;
					}
					var fromPerm = permsCol[from];

					permsCol.RemoveAt(from);
					permsCol.Insert(to, fromPerm);
					await uow.SaveChangesAsync(false).ConfigureAwait(false);
					Service.UpdateCache(config);

					await ReplyConfirmLocalized("moved_permission",
							Format.Code(fromPerm.GetCommand(Prefix, (SocketGuild)Context.Guild)),
							++from,
							++to)
						.ConfigureAwait(false);
					return;
				} catch(Exception e) when(e is ArgumentOutOfRangeException || e is IndexOutOfRangeException) {
				}
			}
			await ReplyErrorLocalized("perm_out_of_range").ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task SrvrCmd(CommandOrCrInfo command, PermissionAction action) {
			await Service.AddPermissions(Context.Guild.Id, new Permissionv2 {
				PrimaryTarget = PrimaryPermissionType.Server,
				PrimaryTargetId = 0,
				SecondaryTarget = SecondaryPermissionType.Command,
				SecondaryTargetName = command.Name.ToLowerInvariant(),
				State = action.Value,
			});

			if(action.Value) {
				await ReplyConfirmLocalized("sx_enable",
					Format.Code(command.Name),
					GetText("of_command")).ConfigureAwait(false);
			} else {
				await ReplyConfirmLocalized("sx_disable",
					Format.Code(command.Name),
					GetText("of_command")).ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task SrvrMdl(ModuleOrCrInfo module, PermissionAction action) {
			await Service.AddPermissions(Context.Guild.Id, new Permissionv2 {
				PrimaryTarget = PrimaryPermissionType.Server,
				PrimaryTargetId = 0,
				SecondaryTarget = SecondaryPermissionType.Module,
				SecondaryTargetName = module.Name.ToLowerInvariant(),
				State = action.Value,
			});

			if(action.Value) {
				await ReplyConfirmLocalized("sx_enable",
					Format.Code(module.Name),
					GetText("of_module")).ConfigureAwait(false);
			} else {
				await ReplyConfirmLocalized("sx_disable",
					Format.Code(module.Name),
					GetText("of_module")).ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task UsrCmd(CommandOrCrInfo command, PermissionAction action, [Remainder] IGuildUser user) {
			await Service.AddPermissions(Context.Guild.Id, new Permissionv2 {
				PrimaryTarget = PrimaryPermissionType.User,
				PrimaryTargetId = user.Id,
				SecondaryTarget = SecondaryPermissionType.Command,
				SecondaryTargetName = command.Name.ToLowerInvariant(),
				State = action.Value,
			});

			if(action.Value) {
				await ReplyConfirmLocalized("ux_enable",
					Format.Code(command.Name),
					GetText("of_command"),
					Format.Code(user.ToString())).ConfigureAwait(false);
			} else {
				await ReplyConfirmLocalized("ux_disable",
					Format.Code(command.Name),
					GetText("of_command"),
					Format.Code(user.ToString())).ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task UsrMdl(ModuleOrCrInfo module, PermissionAction action, [Remainder] IGuildUser user) {
			await Service.AddPermissions(Context.Guild.Id, new Permissionv2 {
				PrimaryTarget = PrimaryPermissionType.User,
				PrimaryTargetId = user.Id,
				SecondaryTarget = SecondaryPermissionType.Module,
				SecondaryTargetName = module.Name.ToLowerInvariant(),
				State = action.Value,
			});

			if(action.Value) {
				await ReplyConfirmLocalized("ux_enable",
					Format.Code(module.Name),
					GetText("of_module"),
					Format.Code(user.ToString())).ConfigureAwait(false);
			} else {
				await ReplyConfirmLocalized("ux_disable",
					Format.Code(module.Name),
					GetText("of_module"),
					Format.Code(user.ToString())).ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task RoleCmd(CommandOrCrInfo command, PermissionAction action, [Remainder] IRole role) {
			if(role == role.Guild.EveryoneRole)
				return;

			await Service.AddPermissions(Context.Guild.Id, new Permissionv2 {
				PrimaryTarget = PrimaryPermissionType.Role,
				PrimaryTargetId = role.Id,
				SecondaryTarget = SecondaryPermissionType.Command,
				SecondaryTargetName = command.Name.ToLowerInvariant(),
				State = action.Value,
			});

			if(action.Value) {
				await ReplyConfirmLocalized("rx_enable",
					Format.Code(command.Name),
					GetText("of_command"),
					Format.Code(role.Name)).ConfigureAwait(false);
			} else {
				await ReplyConfirmLocalized("rx_disable",
					Format.Code(command.Name),
					GetText("of_command"),
					Format.Code(role.Name)).ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task RoleMdl(ModuleOrCrInfo module, PermissionAction action, [Remainder] IRole role) {
			if(role == role.Guild.EveryoneRole)
				return;

			await Service.AddPermissions(Context.Guild.Id, new Permissionv2 {
				PrimaryTarget = PrimaryPermissionType.Role,
				PrimaryTargetId = role.Id,
				SecondaryTarget = SecondaryPermissionType.Module,
				SecondaryTargetName = module.Name.ToLowerInvariant(),
				State = action.Value,
			});


			if(action.Value) {
				await ReplyConfirmLocalized("rx_enable",
					Format.Code(module.Name),
					GetText("of_module"),
					Format.Code(role.Name)).ConfigureAwait(false);
			} else {
				await ReplyConfirmLocalized("rx_disable",
					Format.Code(module.Name),
					GetText("of_module"),
					Format.Code(role.Name)).ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task ChnlCmd(CommandOrCrInfo command, PermissionAction action, [Remainder] ITextChannel chnl) {
			await Service.AddPermissions(Context.Guild.Id, new Permissionv2 {
				PrimaryTarget = PrimaryPermissionType.Channel,
				PrimaryTargetId = chnl.Id,
				SecondaryTarget = SecondaryPermissionType.Command,
				SecondaryTargetName = command.Name.ToLowerInvariant(),
				State = action.Value,
			});

			if(action.Value) {
				await ReplyConfirmLocalized("cx_enable",
					Format.Code(command.Name),
					GetText("of_command"),
					Format.Code(chnl.Name)).ConfigureAwait(false);
			} else {
				await ReplyConfirmLocalized("cx_disable",
					Format.Code(command.Name),
					GetText("of_command"),
					Format.Code(chnl.Name)).ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task ChnlMdl(ModuleOrCrInfo module, PermissionAction action, [Remainder] ITextChannel chnl) {
			await Service.AddPermissions(Context.Guild.Id, new Permissionv2 {
				PrimaryTarget = PrimaryPermissionType.Channel,
				PrimaryTargetId = chnl.Id,
				SecondaryTarget = SecondaryPermissionType.Module,
				SecondaryTargetName = module.Name.ToLowerInvariant(),
				State = action.Value,
			});

			if(action.Value) {
				await ReplyConfirmLocalized("cx_enable",
					Format.Code(module.Name),
					GetText("of_module"),
					Format.Code(chnl.Name)).ConfigureAwait(false);
			} else {
				await ReplyConfirmLocalized("cx_disable",
					Format.Code(module.Name),
					GetText("of_module"),
					Format.Code(chnl.Name)).ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task AllChnlMdls(PermissionAction action, [Remainder] ITextChannel chnl) {
			await Service.AddPermissions(Context.Guild.Id, new Permissionv2 {
				PrimaryTarget = PrimaryPermissionType.Channel,
				PrimaryTargetId = chnl.Id,
				SecondaryTarget = SecondaryPermissionType.AllModules,
				SecondaryTargetName = "*",
				State = action.Value,
			});

			if(action.Value) {
				await ReplyConfirmLocalized("acm_enable",
					Format.Code(chnl.Name)).ConfigureAwait(false);
			} else {
				await ReplyConfirmLocalized("acm_disable",
					Format.Code(chnl.Name)).ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task AllRoleMdls(PermissionAction action, [Remainder] IRole role) {
			if(role == role.Guild.EveryoneRole)
				return;

			await Service.AddPermissions(Context.Guild.Id, new Permissionv2 {
				PrimaryTarget = PrimaryPermissionType.Role,
				PrimaryTargetId = role.Id,
				SecondaryTarget = SecondaryPermissionType.AllModules,
				SecondaryTargetName = "*",
				State = action.Value,
			});

			if(action.Value) {
				await ReplyConfirmLocalized("arm_enable",
					Format.Code(role.Name)).ConfigureAwait(false);
			} else {
				await ReplyConfirmLocalized("arm_disable",
					Format.Code(role.Name)).ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task AllUsrMdls(PermissionAction action, [Remainder] IUser user) {
			await Service.AddPermissions(Context.Guild.Id, new Permissionv2 {
				PrimaryTarget = PrimaryPermissionType.User,
				PrimaryTargetId = user.Id,
				SecondaryTarget = SecondaryPermissionType.AllModules,
				SecondaryTargetName = "*",
				State = action.Value,
			});

			if(action.Value) {
				await ReplyConfirmLocalized("aum_enable",
					Format.Code(user.ToString())).ConfigureAwait(false);
			} else {
				await ReplyConfirmLocalized("aum_disable",
					Format.Code(user.ToString())).ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task AllSrvrMdls(PermissionAction action) {
			var newPerm = new Permissionv2 {
				PrimaryTarget = PrimaryPermissionType.Server,
				PrimaryTargetId = 0,
				SecondaryTarget = SecondaryPermissionType.AllModules,
				SecondaryTargetName = "*",
				State = action.Value,
			};

			var allowUser = new Permissionv2 {
				PrimaryTarget = PrimaryPermissionType.User,
				PrimaryTargetId = Context.User.Id,
				SecondaryTarget = SecondaryPermissionType.AllModules,
				SecondaryTargetName = "*",
				State = true,
			};

			await Service.AddPermissions(Context.Guild.Id,
				newPerm,
				allowUser);

			if(action.Value) {
				await ReplyConfirmLocalized("asm_enable").ConfigureAwait(false);
			} else {
				await ReplyConfirmLocalized("asm_disable").ConfigureAwait(false);
			}
		}
	}
}