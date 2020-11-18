using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Administration.Services;
using Mitternacht.Services;
using Mitternacht.Database;
using Mitternacht.Database.Models;
using MoreLinq;

namespace Mitternacht.Modules.Administration {
	public partial class Administration {
		[Group]
		public class UserPunishCommands : MitternachtSubmodule<UserPunishService> {
			private readonly IUnitOfWork uow;
			private readonly IBotCredentials _bc;

			public UserPunishCommands(IUnitOfWork uow, IBotCredentials bc) {
				this.uow = uow;
				_bc = bc;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.KickMembers)]
			public async Task Warn(IGuildUser user, [Remainder] string reason = null) {
				if(Context.User.Id == user.Guild.OwnerId || user.GetRoles().Where(r => r.IsHoisted).Select(r => r.Position).FallbackIfEmpty(int.MinValue).Max() < ((IGuildUser)Context.User).GetRoles().Where(r => r.IsHoisted).Select(r => r.Position).FallbackIfEmpty(int.MinValue).Max()) {
					try {
						await (await user.GetOrCreateDMChannelAsync()).EmbedAsync(new EmbedBuilder().WithErrorColor().WithDescription(GetText("userpunish_warn_warned_on_server", Context.Guild.ToString())).AddField(efb => efb.WithName(GetText("userpunish_warn_reason")).WithValue(reason ?? "-"))).ConfigureAwait(false);
					} catch { }

					var punishment = await Service.Warn(Context.Guild, user.Id, Context.User.ToString(), reason).ConfigureAwait(false);

					if(punishment == null) {
						await ConfirmLocalized("userpunish_warn_user_warned", Format.Bold(user.ToString())).ConfigureAwait(false);
					} else {
						await ConfirmLocalized("userpunish_warn_user_warned_and_punished", Format.Bold(user.ToString()), Format.Bold(punishment.ToString())).ConfigureAwait(false);
					}
				} else {
					await ErrorLocalized("userpunish_warn_hierarchy").ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[Priority(0)]
			public async Task Warnlog(int page, [Remainder] ulong? userId = null)
				=> await (!userId.HasValue ? InternalWarnlog(Context.User.Id, page - 1) : Context.User.Id == userId || ((IGuildUser) Context.User).GuildPermissions.KickMembers ? InternalWarnlog(userId.Value, page - 1) : Task.CompletedTask);

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[Priority(1)]
			public Task Warnlog(int page, [Remainder] IGuildUser user = null)
				=> Warnlog(page, user?.Id);

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[Priority(2)]
			public Task Warnlog([Remainder] ulong? userId = null)
				=> Warnlog(1, userId);

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[Priority(3)]
			public Task Warnlog([Remainder] IGuildUser user = null)
				=> Warnlog(1, user?.Id);

			private async Task InternalWarnlog(ulong userId, int page) {
				page = page < 0 ? 0 : page;

				const int warnsPerPage = 9;

				var guildUser   = await Context.Guild.GetUserAsync(userId).ConfigureAwait(false);
				var username    = guildUser?.ToString() ?? uow.UsernameHistory.GetUsernamesDescending(userId).FirstOrDefault()?.ToString() ?? userId.ToString();
				var allWarnings = uow.Warnings.For(Context.Guild.Id, userId).OrderByDescending(w => w.DateAdded);
				var embed       = new EmbedBuilder()
					.WithOkColor()
					.WithTitle(GetText("userpunish_warnlog_for_user", username));
				var showMods    = (Context.User as IGuildUser).GuildPermissions.ViewAuditLog;
				var textKey     = showMods ? "userpunish_warnlog_warned_on_by" : "userpunish_warnlog_warned_on";

				await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page, currentPage => {
					var warnings = allWarnings.Skip(page * warnsPerPage).Take(warnsPerPage).ToArray();

					if(!warnings.Any()) {
						embed.WithDescription(GetText("userpunish_warnlog_warnings_none"));
					} else {
						foreach(var w in warnings) {
							var warnText = GetText(textKey, w.DateAdded, w.Moderator);

							if(w.Forgiven)
								warnText = $"{Format.Strikethrough(warnText)} {(showMods ? GetText("userpunish_warnlog_warn_cleared_by", w.ForgivenBy) : "")}".Trim();
							warnText = $"({w.Id}) {warnText}";

							embed.AddField(x => x.WithName(warnText).WithValue(w.Reason));
						}
					}

					return embed;
				}, (int)Math.Ceiling(allWarnings.Count() * 1d / warnsPerPage), reactUsers: new[] {Context.User as IGuildUser}, pageChangeAllowedWithPermissions: gp => gp.KickMembers);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.KickMembers)]
			public async Task WarnlogAll(int page = 1) {
				if(--page < 0) return;

				var warnings = uow.Warnings.GetForGuild(Context.Guild.Id).OrderByDescending(w => w.DateAdded).ToList().GroupBy(x => x.UserId);

				const int elementsPerPage = 15;
				await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page, async currentPage => {
																	var ws = await Task.WhenAll(warnings.Skip(currentPage * elementsPerPage)
																										.Take(elementsPerPage)
																										.ToArray()
																										.Select(async x => {
																											var all      = x.Count();
																											var forgiven = x.Count(y => y.Forgiven);
																											var total    = all - forgiven;
																											return $"{(await Context.Guild.GetUserAsync(x.Key).ConfigureAwait(false))?.ToString() ?? x.Key.ToString()} | {total} ({all} - {forgiven})";
																										}));

																	return new EmbedBuilder().WithTitle(GetText("userpunish_warnlogall_warnings_list")).WithDescription(string.Join("\n", ws));
																}, (int)Math.Ceiling(warnings.Count() * 1d / elementsPerPage), reactUsers: new[] {Context.User as IGuildUser}, pageChangeAllowedWithPermissions: gp => gp.KickMembers)
							.ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.BanMembers)]
			public Task WarnClear(IGuildUser user)
				=> WarnClear(user.Id);

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.BanMembers)]
			public async Task WarnClear(ulong userId) {
				await uow.Warnings.ForgiveAll(Context.Guild.Id, userId, Context.User.ToString()).ConfigureAwait(false);
				uow.SaveChanges(false);

				await ConfirmLocalized("userpunish_warnclear_warnings_cleared", Format.Bold((Context.Guild as SocketGuild)?.GetUser(userId)?.ToString() ?? userId.ToString())).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.BanMembers)]
			public async Task WarnRemove(int id) {
				var warning = uow.Warnings.Get(id);
				
				if(warning != null) {
					uow.Warnings.Remove(warning);
					await ConfirmLocalized("userpunish_warnremove_warning_removed", Format.Bold($"{id}")).ConfigureAwait(false);
					await uow.SaveChangesAsync(false).ConfigureAwait(false);
				} else {
					await ErrorLocalized("userpunish_warnremove_warn_id_not_found", id).ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.KickMembers)]
			public async Task WarnDetails(int id) {
				var warn = uow.Warnings.Get(id);

				if(warn != null) {
					var title = GetText("userpunish_warndetails_warned_by", warn.Moderator);

					if(warn.Forgiven) {
						title = $"{Format.Strikethrough(title)} {GetText("userpunish_warndetails_warn_cleared_by", warn.ForgivenBy)}";
					}

					title += $" ({warn.Id:X})";

					var embedBuilder = new EmbedBuilder()
						.WithOkColor()
						.WithTitle(title)
						.WithDescription(warn.Reason);
					var user = await Context.Guild.GetUserAsync(warn.UserId);

					// This cannot be simplified due to different overloads of the same method being used.
					if(user == null) {
						embedBuilder.WithAuthor(warn.UserId.ToString());
					} else {
						embedBuilder.WithAuthor(user);
					}
					
					embedBuilder.WithTimestamp(warn.DateAdded);

					await Context.Channel.EmbedAsync(embedBuilder).ConfigureAwait(false);
					await uow.SaveChangesAsync(false).ConfigureAwait(false);
				} else {
					await ErrorLocalized("userpunish_warndetails_warn_id_not_found", id).ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOrGuildPermission(GuildPermission.Administrator)]
			[Priority(0)]
			public async Task WarnEdit(int id, [Remainder] string reason = null) {
				var warn = uow.Warnings.Get(id);

				if(_bc.IsOwner(Context.User)) {
					if(warn != null) {
						var oldReason = warn.Reason;
						warn.Reason = reason;

						uow.Warnings.Update(warn);
						await uow.SaveChangesAsync(false);
						await ConfirmLocalized("userpunish_warnedit_warn_edit", id, (await Context.Guild.GetUserAsync(warn.UserId)).ToString(), string.IsNullOrWhiteSpace(oldReason) ? "null" : oldReason, string.IsNullOrWhiteSpace(reason) ? "null" : reason).ConfigureAwait(false);
					} else {
						await ErrorLocalized("userpunish_warnedit_warn_id_not_found", id).ConfigureAwait(false);
					}
				} else if(Context.User is IGuildUser user && warn.GuildId != user.GuildId) {
					await ErrorLocalized("userpunish_warnedit_warn_edit_perms", id).ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.BanMembers)]
			public async Task WarnPunish(int numberOfWarns, PunishmentAction punish, int time = 0) {
				if((punish == PunishmentAction.Mute || time == 0) && numberOfWarns > 0) {
					var warnPunishments = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.WarnPunishments)).WarnPunishments;
					
					warnPunishments.RemoveAll(x => x.Count == numberOfWarns);
					warnPunishments.Add(new WarningPunishment {
						Count = numberOfWarns,
						Punishment = punish,
						Time = time,
					});

					uow.SaveChanges(false);
					await ConfirmLocalized("userpunish_warnpunish_warn_punish_set", Format.Bold(punish.ToString()), Format.Bold(numberOfWarns.ToString())).ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.BanMembers)]
			public async Task WarnPunish(int numberOfWarns) {
				if(numberOfWarns > 0) {
					var warnPunishments = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.WarnPunishments)).WarnPunishments;

					warnPunishments.RemoveAll(x => x.Count == numberOfWarns);
					uow.SaveChanges(false);
					await ConfirmLocalized("userpunish_warnpunish_warn_punish_rem", Format.Bold(numberOfWarns.ToString())).ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task WarnPunishList() {
				var warnPunishments = uow.GuildConfigs.For(Context.Guild.Id, gc => gc.Include(x => x.WarnPunishments)).WarnPunishments.OrderBy(x => x.Count).ToArray();

				var list = warnPunishments.Any() ? string.Join("\n", warnPunishments.Select(x => $"{x.Count} -> {x.Punishment}")) : GetText("warnpl_none");
				await Context.Channel.SendConfirmAsync(list, GetText("userpunish_warnpunishlist_warn_punish_list")).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.BanMembers)]
			[RequireBotPermission(GuildPermission.BanMembers)]
			public async Task Ban(IGuildUser user, [Remainder] string msg = null) {
				if(Context.User.Id == user.Guild.OwnerId || user.GetRoles().Where(r => r.IsHoisted).Select(r => r.Position).Max() < ((IGuildUser)Context.User).GetRoles().Where(r => r.IsHoisted).Select(r => r.Position).Max()) {
					if(!string.IsNullOrWhiteSpace(msg)) {
						try {
							await user.SendErrorAsync(GetText("userpunish_ban_bandm", Format.Bold(Context.Guild.Name), msg));
						} catch { }
					}

					await Context.Guild.AddBanAsync(user, 7, msg).ConfigureAwait(false);
					
					var embedBuilder = new EmbedBuilder()
						.WithOkColor()
						.WithTitle($"⛔️ {GetText("userpunish_ban_banned_user")}")
						.AddField(efb => efb
							.WithName(GetText("userpunish_ban_username"))
							.WithValue(user.ToString())
							.WithIsInline(true))
						.AddField(efb => efb
							.WithName("ID")
							.WithValue(user.Id.ToString())
							.WithIsInline(true));
					
					await Context.Channel.EmbedAsync(embedBuilder).ConfigureAwait(false);
				} else {
					await ErrorLocalized("userpunish_ban_hierarchy").ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.BanMembers)]
			[RequireBotPermission(GuildPermission.BanMembers)]
			public async Task Unban([Remainder] string user) {
				var bans = await Context.Guild.GetBansAsync().ConfigureAwait(false);
				var ban = bans.FirstOrDefault(b => string.Equals(b.User.ToString(), user, StringComparison.InvariantCultureIgnoreCase));

				await UnbanInternal(ban).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.BanMembers)]
			[RequireBotPermission(GuildPermission.BanMembers)]
			public async Task Unban(ulong userId) {
				var bans = await Context.Guild.GetBansAsync().ConfigureAwait(false);
				var ban = bans.FirstOrDefault(b => b.User.Id == userId);

				await UnbanInternal(ban).ConfigureAwait(false);
			}

			private async Task UnbanInternal(IBan ban) {
				if(ban != null) {
					await Context.Guild.RemoveBanAsync(ban.User).ConfigureAwait(false);

					await ConfirmLocalized("userpunish_unban_unbanned_user", Format.Bold(ban.ToString())).ConfigureAwait(false);
				} else {
					await ErrorLocalized("userpunish_unban_user_not_found").ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.KickMembers)]
			[RequireUserPermission(GuildPermission.ManageMessages)]
			[RequireBotPermission(GuildPermission.BanMembers)]
			public async Task Softban(IGuildUser user, [Remainder] string msg = null) {
				if(Context.User.Id == user.Guild.OwnerId || user.GetRoles().Where(r => r.IsHoisted).Select(r => r.Position).Max() < ((IGuildUser)Context.User).GetRoles().Where(r => r.IsHoisted).Select(r => r.Position).Max()) {
					if(!string.IsNullOrWhiteSpace(msg)) {
						try {
							await user.SendErrorAsync(GetText("userpunish_softban_sbdm", Format.Bold(Context.Guild.Name), msg)).ConfigureAwait(false);
						} catch { }
					}

					await Context.Guild.AddBanAsync(user, 7).ConfigureAwait(false);
					try {
						await Context.Guild.RemoveBanAsync(user).ConfigureAwait(false);
					} catch {
						await Context.Guild.RemoveBanAsync(user).ConfigureAwait(false);
					}

					var embedBuilder = new EmbedBuilder()
						.WithOkColor()
						.WithTitle($"☣ {GetText("userpunish_softban_sb_user")}")
						.AddField(efb => efb
							.WithName(GetText("userpunish_softban_username"))
							.WithValue(user.ToString())
							.WithIsInline(true))
						.AddField(efb => efb
							.WithName("ID")
							.WithValue(user.Id.ToString())
							.WithIsInline(true));

					await Context.Channel.EmbedAsync(embedBuilder).ConfigureAwait(false);
				} else {
					await ErrorLocalized("userpunish_softban_hierarchy").ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.KickMembers)]
			[RequireBotPermission(GuildPermission.KickMembers)]
			public async Task Kick(IGuildUser user, [Remainder] string msg = null) {
				if(Context.User.Id == user.Guild.OwnerId || user.GetRoles().Select(r => r.Position).Max() < ((IGuildUser)Context.User).GetRoles().Select(r => r.Position).Max()) {
					if(!string.IsNullOrWhiteSpace(msg)) {
						try {
							await user.SendErrorAsync(GetText("userpunish_kick_kickdm", Format.Bold(Context.Guild.Name), msg)).ConfigureAwait(false);
						} catch { }
					}

					await user.KickAsync().ConfigureAwait(false);

					var embedBuilder = new EmbedBuilder()
						.WithOkColor()
						.WithTitle(GetText("userpunish_kick_kicked_user"))
						.AddField(efb => efb
							.WithName(GetText("userpunish_kick_username"))
							.WithValue(user.ToString())
							.WithIsInline(true))
						.AddField(efb => efb
							.WithName("ID")
							.WithValue(user.Id.ToString())
							.WithIsInline(true));

					await Context.Channel.EmbedAsync(embedBuilder).ConfigureAwait(false);
				} else {
					await ErrorLocalized("userpunish_kick_hierarchy").ConfigureAwait(false);
				}
			}
		}
	}
}
