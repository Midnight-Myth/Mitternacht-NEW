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
using Mitternacht.Services.Database;
using Mitternacht.Services.Database.Models;
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
				if(Context.User.Id != user.Guild.OwnerId && user.GetRoles().Where(r => r.IsHoisted).Select(r => r.Position).FallbackIfEmpty(int.MinValue).Max() >= ((IGuildUser) Context.User).GetRoles().Where(r => r.IsHoisted).Select(r => r.Position).FallbackIfEmpty(int.MinValue).Max()) {
					await ReplyErrorLocalized("warn_hierarchy").ConfigureAwait(false);
					return;
				}

				try {
					await (await user.GetOrCreateDMChannelAsync()).EmbedAsync(new EmbedBuilder().WithErrorColor().WithDescription(GetText("warned_on_server", Context.Guild.ToString())).AddField(efb => efb.WithName(GetText("moderator")).WithValue(Context.User.ToString())).AddField(efb => efb.WithName(GetText("reason")).WithValue(reason ?? "-"))).ConfigureAwait(false);
				} catch { }

				var punishment = await Service.Warn(Context.Guild, user.Id, Context.User.ToString(), reason).ConfigureAwait(false);

				if(punishment == null) {
					await ReplyConfirmLocalized("warn_user_warned", Format.Bold(user.ToString())).ConfigureAwait(false);
				} else {
					await ReplyConfirmLocalized("warn_user_warned_and_punished", Format.Bold(user.ToString()), Format.Bold(punishment.ToString())).ConfigureAwait(false);
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
			public async Task Warnlog(int page, [Remainder] IGuildUser user = null)
				=> await Warnlog(1, user?.Id);

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[Priority(2)]
			public async Task Warnlog([Remainder] ulong? userId = null)
				=> await Warnlog(1, userId);

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[Priority(3)]
			public async Task Warnlog([Remainder] IGuildUser user = null)
				=> await Warnlog(1, user?.Id);

			private async Task InternalWarnlog(ulong userId, int page) {
				if(page < 0) return;

				const int warnsPerPage = 9;

				var guildUser   = await Context.Guild.GetUserAsync(userId).ConfigureAwait(false);
				var username    = guildUser?.ToString() ?? uow.UsernameHistory.GetUsernamesDescending(userId).FirstOrDefault()?.ToString() ?? userId.ToString();
				var allWarnings = uow.Warnings.For(Context.Guild.Id, userId).OrderByDescending(w => w.DateAdded);
				var embed       = new EmbedBuilder()
					.WithOkColor()
					.WithTitle(GetText("warnlog_for_user", username));
				var showMods    = (Context.User as IGuildUser).GuildPermissions.ViewAuditLog;
				var textKey     = showMods ? "warned_on_by" : "warned_on";

				await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page, currentPage => {
					var warnings = allWarnings.Skip(page * warnsPerPage).Take(warnsPerPage).ToArray();

					if(!warnings.Any()) {
						embed.WithDescription(GetText("warnings_none"));
					} else {
						foreach(var w in warnings) {
							var warnText = GetText(textKey, w.DateAdded, w.Moderator);

							if(w.Forgiven)
								warnText = $"{Format.Strikethrough(warnText)} {(showMods ? GetText("warn_cleared_by", w.ForgivenBy) : "")}".Trim();
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

																	return new EmbedBuilder().WithTitle(GetText("warnings_list")).WithDescription(string.Join("\n", ws));
																}, (int)Math.Ceiling(warnings.Count() * 1d / elementsPerPage), reactUsers: new[] {Context.User as IGuildUser}, pageChangeAllowedWithPermissions: gp => gp.KickMembers)
							.ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.BanMembers)]
			public Task Warnclear(IGuildUser user)
				=> Warnclear(user.Id);

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.BanMembers)]
			public async Task Warnclear(ulong userId) {
				await uow.Warnings.ForgiveAll(Context.Guild.Id, userId, Context.User.ToString()).ConfigureAwait(false);
				uow.SaveChanges(false);

				await ReplyConfirmLocalized("warnings_cleared", Format.Bold((Context.Guild as SocketGuild)?.GetUser(userId)?.ToString() ?? userId.ToString())).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.BanMembers)]
			public async Task Warnremove(int id) {
				var warning = uow.Warnings.Get(id);
				if(warning == null) {
					await ReplyErrorLocalized("warn_id_not_found", id).ConfigureAwait(false);
					return;
				}

				uow.Warnings.Remove(warning);
				await ReplyConfirmLocalized("warning_removed", Format.Bold($"{id}")).ConfigureAwait(false);
				await uow.SaveChangesAsync(false).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.KickMembers)]
			public async Task Warndetails(int id) {
				var w = uow.Warnings.Get(id);
				if(w == null) {
					await ReplyErrorLocalized("warn_id_not_found", id).ConfigureAwait(false);
					return;
				}

				var title            = GetText("warned_by", w.Moderator);
				if(w.Forgiven) title = $"{Format.Strikethrough(title)} {GetText("warn_cleared_by", w.ForgivenBy)}";
				title += $" ({w.Id:X})";
				var embed = new EmbedBuilder().WithOkColor().WithTitle(title).WithDescription(w.Reason);
				var user  = await Context.Guild.GetUserAsync(w.UserId);
				if(user == null)
					embed.WithAuthor(w.UserId.ToString());
				else
					embed.WithAuthor(user);
				if(w.DateAdded != null) embed.WithTimestamp(w.DateAdded.Value);
				await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
				await uow.SaveChangesAsync(false).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOrGuildPermission(GuildPermission.Administrator)]
			[Priority(0)]
			public async Task WarnEdit(int id, [Remainder] string reason = null) {
				var w    = uow.Warnings.Get(id);
				var user = Context.User as IGuildUser;
				if(!_bc.IsOwner(Context.User)) {
					if(user == null) return;
					if(w.GuildId != user.GuildId) {
						await ReplyErrorLocalized("warn_edit_perms", id).ConfigureAwait(false);
						return;
					}
				}

				if(w == null) {
					await ReplyErrorLocalized("warn_id_not_found", id).ConfigureAwait(false);
					return;
				}

				var oldreason = w.Reason;
				w.Reason = reason;
				uow.Warnings.Update(w);
				await uow.SaveChangesAsync(false);
				await ReplyConfirmLocalized("warn_edit", id, (await Context.Guild.GetUserAsync(w.UserId)).ToString(), string.IsNullOrWhiteSpace(oldreason) ? "null" : oldreason, string.IsNullOrWhiteSpace(reason) ? "null" : reason).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.BanMembers)]
			public async Task WarnPunish(int number, PunishmentAction punish, int time = 0) {
				if(punish != PunishmentAction.Mute && time != 0) return;
				if(number <= 0) return;

				var ps = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.WarnPunishments)).WarnPunishments;
				ps.RemoveAll(x => x.Count == number);

				ps.Add(new WarningPunishment {Count = number, Punishment = punish, Time = time});
				uow.SaveChanges(false);

				await ReplyConfirmLocalized("warn_punish_set", Format.Bold(punish.ToString()), Format.Bold(number.ToString())).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.BanMembers)]
			public async Task WarnPunish(int number) {
				if(number <= 0) return;

				var ps = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.WarnPunishments)).WarnPunishments;
				var p  = ps.FirstOrDefault(x => x.Count == number);

				if(p != null) {
					uow.Context.Remove(p);
					uow.SaveChanges(false);
				}

				await ReplyConfirmLocalized("warn_punish_rem", Format.Bold(number.ToString())).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task WarnPunishList() {
				var ps = uow.GuildConfigs.For(Context.Guild.Id, gc => gc.Include(x => x.WarnPunishments)).WarnPunishments.OrderBy(x => x.Count).ToArray();

				var list = ps.Any() ? string.Join("\n", ps.Select(x => $"{x.Count} -> {x.Punishment}")) : GetText("warnpl_none");
				await Context.Channel.SendConfirmAsync(list, GetText("warn_punish_list")).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.BanMembers)]
			[RequireBotPermission(GuildPermission.BanMembers)]
			public async Task Ban(IGuildUser user, [Remainder] string msg = null) {
				if(Context.User.Id != user.Guild.OwnerId && user.GetRoles().Where(r => r.IsHoisted).Select(r => r.Position).Max() >= ((IGuildUser) Context.User).GetRoles().Where(r => r.IsHoisted).Select(r => r.Position).Max()) {
					await ReplyErrorLocalized("hierarchy").ConfigureAwait(false);
					return;
				}

				if(!string.IsNullOrWhiteSpace(msg)) {
					try {
						await user.SendErrorAsync(GetText("bandm", Format.Bold(Context.Guild.Name), msg));
					} catch {
						// ignored
					}
				}

				await Context.Guild.AddBanAsync(user, 7, msg).ConfigureAwait(false);
				await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor().WithTitle($"⛔️ {GetText("banned_user")}").AddField(efb => efb.WithName(GetText("username")).WithValue(user.ToString()).WithIsInline(true)).AddField(efb => efb.WithName("ID").WithValue(user.Id.ToString()).WithIsInline(true))).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.BanMembers)]
			[RequireBotPermission(GuildPermission.BanMembers)]
			public async Task Unban([Remainder] string user) {
				var bans = await Context.Guild.GetBansAsync();

				var bun = bans.FirstOrDefault(x => string.Equals(x.User.ToString(), user, StringComparison.InvariantCultureIgnoreCase));

				if(bun == null) {
					await ReplyErrorLocalized("user_not_found").ConfigureAwait(false);
					return;
				}

				await UnbanInternal(bun.User).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.BanMembers)]
			[RequireBotPermission(GuildPermission.BanMembers)]
			public async Task Unban(ulong userId) {
				var bans = await Context.Guild.GetBansAsync();

				var bun = bans.FirstOrDefault(x => x.User.Id == userId);

				if(bun == null) {
					await ReplyErrorLocalized("user_not_found").ConfigureAwait(false);
					return;
				}

				await UnbanInternal(bun.User).ConfigureAwait(false);
			}

			private async Task UnbanInternal(IUser user) {
				await Context.Guild.RemoveBanAsync(user).ConfigureAwait(false);

				await ReplyConfirmLocalized("unbanned_user", Format.Bold(user.ToString())).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.KickMembers)]
			[RequireUserPermission(GuildPermission.ManageMessages)]
			[RequireBotPermission(GuildPermission.BanMembers)]
			public async Task Softban(IGuildUser user, [Remainder] string msg = null) {
				if(Context.User.Id != user.Guild.OwnerId && user.GetRoles().Where(r => r.IsHoisted).Select(r => r.Position).Max() >= ((IGuildUser) Context.User).GetRoles().Where(r => r.IsHoisted).Select(r => r.Position).Max()) {
					await ReplyErrorLocalized("hierarchy").ConfigureAwait(false);
					return;
				}

				if(!string.IsNullOrWhiteSpace(msg)) {
					try {
						await user.SendErrorAsync(GetText("sbdm", Format.Bold(Context.Guild.Name), msg));
					} catch {
						// ignored
					}
				}

				await Context.Guild.AddBanAsync(user, 7).ConfigureAwait(false);
				try {
					await Context.Guild.RemoveBanAsync(user).ConfigureAwait(false);
				} catch {
					await Context.Guild.RemoveBanAsync(user).ConfigureAwait(false);
				}

				await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor().WithTitle($"☣ {GetText("sb_user")}").AddField(efb => efb.WithName(GetText("username")).WithValue(user.ToString()).WithIsInline(true)).AddField(efb => efb.WithName("ID").WithValue(user.Id.ToString()).WithIsInline(true))).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.KickMembers)]
			[RequireBotPermission(GuildPermission.KickMembers)]
			public async Task Kick(IGuildUser user, [Remainder] string msg = null) {
				if(Context.User.Id != user.Guild.OwnerId && user.GetRoles().Select(r => r.Position).Max() >= ((IGuildUser) Context.User).GetRoles().Select(r => r.Position).Max()) {
					await ReplyErrorLocalized("hierarchy").ConfigureAwait(false);
					return;
				}

				if(!string.IsNullOrWhiteSpace(msg)) {
					try {
						await user.SendErrorAsync(GetText("kickdm", Format.Bold(Context.Guild.Name), msg));
					} catch { /*ignored*/
					}
				}

				await user.KickAsync().ConfigureAwait(false);
				await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor().WithTitle(GetText("kicked_user")).AddField(efb => efb.WithName(GetText("username")).WithValue(user.ToString()).WithIsInline(true)).AddField(efb => efb.WithName("ID").WithValue(user.Id.ToString()).WithIsInline(true))).ConfigureAwait(false);
			}
		}
	}
}
