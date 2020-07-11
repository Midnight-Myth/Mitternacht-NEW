using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Utility.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Utility {
	public partial class Utility {
		[Group]
		public class UsernameHistoryCommands : MitternachtSubmodule<UsernameHistoryService> {
			private readonly IUnitOfWork uow;

			public UsernameHistoryCommands(IUnitOfWork uow) {
				this.uow = uow;
			}

			[MitternachtCommand, Description, Usage, Aliases]
			[OwnerOnly]
			public async Task ToggleUsernameHistory() {
				var bc      = uow.BotConfig.GetOrCreate();
				var logging = bc.LogUsernames = !bc.LogUsernames;
				uow.BotConfig.Update(bc);
				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				await ConfirmLocalized("unh_log_global", GetActiveText(logging)).ConfigureAwait(false);
			}

			[MitternachtCommand, Description, Usage, Aliases]
			[OwnerOnly]
			public async Task ToggleUsernameHistoryGuild(bool? toggle = null, IGuild guild = null) {
				guild = guild ?? Context.Guild;
				if(guild == null) {
					await ErrorLocalized("unh_guild_null").ConfigureAwait(false);
					return;
				}

				var globalLogging = uow.BotConfig.GetOrCreate().LogUsernames;
				var gc            = uow.GuildConfigs.For(guild.Id);
				var loggingBefore = gc.LogUsernameHistory;
				if(loggingBefore == toggle) {
					await ErrorLocalized("unh_guild_log_equals", guild.Name, GetActiveText(toggle)).ConfigureAwait(false);
					return;
				}

				gc.LogUsernameHistory = toggle;
				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				await Context.Channel.SendConfirmAsync(GetText("unh_log_guild", guild.Name, GetActiveText(loggingBefore), GetActiveText(toggle)).Trim() + " " + GetText("unh_log_global_append", GetActiveText(globalLogging)).Trim()).ConfigureAwait(false);
			}

			[MitternachtCommand, Description, Usage, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task UsernameHistory(IUser user = null, int page = 1) {
				user = user ?? Context.User;
				await UsernameHistory(user.Id, page);
			}

			[MitternachtCommand, Description, Usage, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task UsernameHistory(ulong userId, int page = 1) {
				var username = (await Context.Guild.GetUserAsync(userId))?.ToString() ?? userId.ToString();

				var nicknames = uow.NicknameHistory.GetGuildUserNames(Context.Guild.Id, userId).ToList();
				var usernames = uow.UsernameHistory.GetUsernamesDescending(userId).ToList();
				var usernicknames = usernames.Concat(nicknames).OrderByDescending(u => u.DateSet).ToList();

				if(!usernicknames.Any()) {
					await ErrorLocalized("unh_no_names", username).ConfigureAwait(false);
					return;
				}

				if(page < 1) page = 1;

				const int elementsPerPage = 10;
				var       pagecount       = (int)Math.Ceiling(usernicknames.Count / (elementsPerPage * 1d));
				if(page > pagecount) page = pagecount;
				await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page - 1, p => {
																	var embed = new EmbedBuilder().WithOkColor().WithTitle(GetText("unh_title", username)).WithDescription(string.Join("\n", usernicknames.Skip(p * elementsPerPage).Take(elementsPerPage).Select(uhm => $"- `{uhm.Name}#{uhm.DiscordDiscriminator:D4}`{(uhm is NicknameHistoryModel ? "" : " **(G)**")} - {uhm.DateSet.ToLocalTime():dd.MM.yyyy HH:mm}{(uhm.DateReplaced.HasValue ? $" => {uhm.DateReplaced.Value.ToLocalTime():dd.MM.yyyy HH:mm}" : "")}")));
																	return embed;
																}, pagecount - 1, reactUsers: new[] {Context.User as IGuildUser}, hasPerms: gp => gp.KickMembers)
							.ConfigureAwait(false);
			}

			[MitternachtCommand, Description, Usage, Aliases]
			public async Task UsernameHistoryGlobal(IUser user = null, int page = 1) {
				user = user ?? Context.User;
				await UsernameHistoryGlobal(user.Id, page);
			}

			[MitternachtCommand, Description, Usage, Aliases]
			public async Task UsernameHistoryGlobal(ulong userId, int page = 1) {
				var username = (await Context.Client.GetUserAsync(userId))?.ToString() ?? userId.ToString();

				var usernames = uow.UsernameHistory.GetUsernamesDescending(userId).OrderByDescending(u => u.DateSet).ToList();

				if(!usernames.Any()) {
					await ErrorLocalized("unh_no_names", username).ConfigureAwait(false);
					return;
				}

				if(page < 1) page = 1;

				const int elementsPerPage = 10;
				var       pagecount       = (int)Math.Ceiling(usernames.Count / (elementsPerPage * 1d));
				if(page > pagecount) page = pagecount;
				await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page - 1, p => {
																	var embed = new EmbedBuilder().WithOkColor().WithTitle(GetText("unh_title_global", username)).WithDescription(string.Join("\n", usernames.Skip(p * elementsPerPage).Take(elementsPerPage).Select(uhm => $"- `{uhm.Name}#{uhm.DiscordDiscriminator:D4}` - {uhm.DateSet.ToLocalTime():dd.MM.yyyy HH:mm}{(uhm.DateReplaced.HasValue ? $" => {uhm.DateReplaced.Value.ToLocalTime():dd.MM.yyyy HH:mm}" : "")}")));
																	return embed;
																}, pagecount - 1, reactUsers: new[] {Context.User as IGuildUser}, hasPerms: gp => gp.KickMembers)
							.ConfigureAwait(false);
			}

			[MitternachtCommand, Description, Usage, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task UsernameHistoryGuild(IUser user = null, int page = 1) {
				user = user ?? Context.User;
				await UsernameHistoryGuild(user.Id, page);
			}

			[MitternachtCommand, Description, Usage, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task UsernameHistoryGuild(ulong userId, int page = 1) {
				var username = (await Context.Guild.GetUserAsync(userId))?.ToString() ?? userId.ToString();

				var nicknames = uow.NicknameHistory.GetGuildUserNames(Context.Guild.Id, userId).OrderByDescending(u => u.DateSet).ToList();

				if(!nicknames.Any()) {
					await ErrorLocalized("unh_no_names", username).ConfigureAwait(false);
					return;
				}

				if(page < 1) page = 1;

				const int elementsPerPage = 10;
				var       pagecount       = (int)Math.Ceiling(nicknames.Count / (elementsPerPage * 1d));
				if(page > pagecount) page = pagecount;
				await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page - 1, p => {
																	var embed = new EmbedBuilder().WithOkColor().WithTitle(GetText("unh_title_guild", username)).WithDescription(string.Join("\n", nicknames.Skip(p * elementsPerPage).Take(elementsPerPage).Select(uhm => $"- `{uhm.Name}#{uhm.DiscordDiscriminator:D4}` - {uhm.DateSet.ToLocalTime():dd.MM.yyyy HH:mm}{(uhm.DateReplaced.HasValue ? $" => {uhm.DateReplaced.Value.ToLocalTime():dd.MM.yyyy HH:mm}" : "")}")));
																	return embed;
																}, pagecount - 1, reactUsers: new[] {Context.User as IGuildUser}, hasPerms: gp => gp.KickMembers)
							.ConfigureAwait(false);
			}

			private string GetActiveText(bool? setting)
				=> GetText(setting.HasValue ? setting.Value ? "unh_active" : "unh_inactive" : "unh_global");

			[MitternachtCommand, Description, Aliases, Usage]
			[OwnerOnly]
			public async Task UpdateUsernames() {
				var (nicks, usernames, users, time) = await Service.UpdateUsernames().ConfigureAwait(false);
				await ConfirmLocalized("unh_update_usernames", nicks, usernames, users, $"{time.TotalSeconds:F2}s").ConfigureAwait(false);
			}
		}
	}
}