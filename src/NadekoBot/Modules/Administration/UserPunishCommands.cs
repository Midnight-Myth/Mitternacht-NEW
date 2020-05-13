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
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Administration {
	public partial class Administration {
		[Group]
		public class UserPunishCommands : MitternachtSubmodule<UserPunishService> {
			private readonly DbService       _db;
			private readonly IBotCredentials _bc;

			public UserPunishCommands(DbService db, IBotCredentials bc) {
				_db = db;
				_bc = bc;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.KickMembers)]
			public async Task Warn(IGuildUser user, [Remainder] string reason = null) {
				if(Context.User.Id != user.Guild.OwnerId && user.GetRoles().Where(r => r.IsHoisted).Select(r => r.Position).Max() >= ((IGuildUser) Context.User).GetRoles().Where(r => r.IsHoisted).Select(r => r.Position).Max()) {
					await ReplyErrorLocalized("hierarchy").ConfigureAwait(false);
					return;
				}

				try {
					await (await user.GetOrCreateDMChannelAsync()).EmbedAsync(new EmbedBuilder().WithErrorColor().WithDescription(GetText("warned_on", Context.Guild.ToString())).AddField(efb => efb.WithName(GetText("moderator")).WithValue(Context.User.ToString())).AddField(efb => efb.WithName(GetText("reason")).WithValue(reason ?? "-"))).ConfigureAwait(false);
				} catch { /*ignored*/
				}

				var punishment = await Service.Warn(Context.Guild, user.Id, Context.User.ToString(), reason).ConfigureAwait(false);

				if(punishment == null) {
					await ReplyConfirmLocalized("user_warned", Format.Bold(user.ToString())).ConfigureAwait(false);
				} else {
					await ReplyConfirmLocalized("user_warned_and_punished", Format.Bold(user.ToString()), Format.Bold(punishment.ToString())).ConfigureAwait(false);
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
				Warning[] allWarnings;
				using(var uow = _db.UnitOfWork) {
					allWarnings = uow.Warnings.For(Context.Guild.Id, userId);
				}

				await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page, p => {
																	var warnings = allWarnings.Skip(page * warnsPerPage).Take(warnsPerPage).ToArray();
																	var embed    = new EmbedBuilder().WithOkColor().WithTitle(GetText("warnlog_for", (Context.Guild as SocketGuild)?.GetUser(userId)?.ToString() ?? userId.ToString()));

																	if(!warnings.Any())
																		embed.WithDescription(GetText("warnings_none"));
																	else
																		foreach(var w in warnings) {
																			var name = GetText("warned_on_by", w.DateAdded?.ToString("dd.MM.yyy"), w.DateAdded?.ToString("HH:mm"), w.Moderator);

																			if(w.Forgiven) name = $"{Format.Strikethrough(name)} {GetText("warn_cleared_by", w.ForgivenBy)}";
																			name += $" ({w.Id.ToHex()})";
																			embed.AddField(x => x.WithName(name).WithValue(w.Reason));
																		}

																	return embed;
																}, allWarnings.Length / warnsPerPage, reactUsers: new[] {Context.User as IGuildUser}, hasPerms: gp => gp.KickMembers);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.KickMembers)]
			public async Task WarnlogAll(int page = 1) {
				if(--page < 0) return;

				IGrouping<ulong, Warning>[] warnings;
				using(var uow = _db.UnitOfWork) {
					warnings = uow.Warnings.GetForGuild(Context.Guild.Id).GroupBy(x => x.UserId).ToArray();
				}

				await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page, async curPage => {
																	var ws = await Task.WhenAll(warnings.Skip(curPage * 15)
																										.Take(15)
																										.ToArray()
																										.Select(async x => {
																											var all      = x.Count();
																											var forgiven = x.Count(y => y.Forgiven);
																											var total    = all - forgiven;
																											return $"{(await Context.Guild.GetUserAsync(x.Key).ConfigureAwait(false))?.ToString() ?? x.Key.ToString()} | {total} ({all} - {forgiven})";
																										}));

																	return new EmbedBuilder().WithTitle(GetText("warnings_list")).WithDescription(string.Join("\n", ws));
																}, warnings.Length / 15, reactUsers: new[] {Context.User as IGuildUser}, hasPerms: gp => gp.KickMembers)
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
				using(var uow = _db.UnitOfWork) {
					await uow.Warnings.ForgiveAll(Context.Guild.Id, userId, Context.User.ToString()).ConfigureAwait(false);
					uow.Complete();
				}

				await ReplyConfirmLocalized("warnings_cleared", Format.Bold((Context.Guild as SocketGuild)?.GetUser(userId)?.ToString() ?? userId.ToString())).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.BanMembers)]
			public async Task Warnremove(IGuildUser user, string hexid) {
				var id = hexid.FromHexToInt();
				if(id == null) {
					await ReplyErrorLocalized("warn_hexid_parsefail", hexid).ConfigureAwait(false);
					return;
				}

				using var uow = _db.UnitOfWork;
				var warning = uow.Warnings.Get(id.Value);
				if(warning == null) {
					await ReplyErrorLocalized("warn_hexid_no_entry", hexid).ConfigureAwait(false);
					return;
				}

				if(warning.UserId != user.Id) {
					await ReplyErrorLocalized("warning_remove_other_user", Format.Bold(hexid)).ConfigureAwait(false);
					return;
				}

				uow.Warnings.Remove(warning);
				await ReplyConfirmLocalized("warning_removed", Format.Bold(hexid), Format.Bold(user.ToString())).ConfigureAwait(false);
				await uow.CompleteAsync().ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.KickMembers)]
			public async Task Warnid(string hexid) {
				var id = hexid.FromHexToInt();
				if(id == null) {
					await ReplyErrorLocalized("warn_hexid_parsefail", hexid).ConfigureAwait(false);
					return;
				}

				using var uow = _db.UnitOfWork;
				var w = uow.Warnings.Get(id.Value);
				if(w == null) {
					await ReplyErrorLocalized("warn_hexid_no_entry", hexid).ConfigureAwait(false);
					return;
				}

				var title            = GetText("warned_by", w.Moderator);
				if(w.Forgiven) title = $"{Format.Strikethrough(title)} {GetText("warn_cleared_by", w.ForgivenBy)}";
				title += $" ({w.Id.ToHex()})";
				var embed = new EmbedBuilder().WithOkColor().WithTitle(title).WithDescription(w.Reason);
				var user  = await Context.Guild.GetUserAsync(w.UserId);
				if(user == null)
					embed.WithAuthor(w.UserId.ToString());
				else
					embed.WithAuthor(user);
				if(w.DateAdded != null) embed.WithTimestamp(w.DateAdded.Value);
				await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
				await uow.CompleteAsync().ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOrGuildPermission(GuildPermission.Administrator)]
			[Priority(0)]
			public async Task WarnEdit(string hexid, [Remainder] string reason = null) {
				var id = hexid.FromHexToInt();
				if(id == null) {
					await ReplyErrorLocalized("warn_hexid_parsefail", hexid).ConfigureAwait(false);
					return;
				}

				using var uow = _db.UnitOfWork;
				var w    = uow.Warnings.Get(id.Value);
				var user = Context.User as IGuildUser;
				if(!_bc.IsOwner(Context.User)) {
					if(user == null) return;
					if(w.GuildId != user.GuildId) {
						await ReplyErrorLocalized("warn_edit_perms", hexid).ConfigureAwait(false);
						return;
					}
				}

				if(w == null) {
					await ReplyErrorLocalized("warn_hexid_no_entry", hexid).ConfigureAwait(false);
					return;
				}

				var oldreason = w.Reason;
				w.Reason = reason;
				uow.Warnings.Update(w);
				await uow.CompleteAsync();
				await ReplyConfirmLocalized("warn_edit", hexid, (await Context.Guild.GetUserAsync(w.UserId)).ToString(), string.IsNullOrWhiteSpace(oldreason) ? "null" : oldreason, string.IsNullOrWhiteSpace(reason) ? "null" : reason).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.BanMembers)]
			public async Task WarnPunish(int number, PunishmentAction punish, int time = 0) {
				if(punish != PunishmentAction.Mute && time != 0) return;
				if(number <= 0) return;

				using(var uow = _db.UnitOfWork) {
					var ps = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.WarnPunishments)).WarnPunishments;
					ps.RemoveAll(x => x.Count == number);

					ps.Add(new WarningPunishment {Count = number, Punishment = punish, Time = time});
					uow.Complete();
				}

				await ReplyConfirmLocalized("warn_punish_set", Format.Bold(punish.ToString()), Format.Bold(number.ToString())).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.BanMembers)]
			public async Task WarnPunish(int number) {
				if(number <= 0) return;

				using(var uow = _db.UnitOfWork) {
					var ps = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.WarnPunishments)).WarnPunishments;
					var p  = ps.FirstOrDefault(x => x.Count == number);

					if(p != null) {
						uow.Context.Remove(p);
						uow.Complete();
					}
				}

				await ReplyConfirmLocalized("warn_punish_rem", Format.Bold(number.ToString())).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task WarnPunishList() {
				WarningPunishment[] ps;
				using(var uow = _db.UnitOfWork) {
					ps = uow.GuildConfigs.For(Context.Guild.Id, gc => gc.Include(x => x.WarnPunishments)).WarnPunishments.OrderBy(x => x.Count).ToArray();
				}

				var list = ps.Any() ? string.Join("\n", ps.Select(x => $"{x.Count} -> {x.Punishment}")) : GetText("warnpl_none");
				await Context.Channel.SendConfirmAsync(GetText("warn_punish_list"), list).ConfigureAwait(false);
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
