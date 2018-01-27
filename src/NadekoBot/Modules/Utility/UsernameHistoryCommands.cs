using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Utility.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class UsernameHistoryCommands : NadekoSubmodule<UsernameHistoryService>
        {
            private readonly DbService _db;

            public UsernameHistoryCommands(DbService db) {
                _db = db;
            }

            [NadekoCommand, Description, Usage, Aliases]
            [OwnerOnly]
            public async Task ToggleUsernameHistory() {
                bool logging;
                using (var uow = _db.UnitOfWork) {
                    var bc = uow.BotConfig.GetOrCreate(set => set);
                    logging = bc.LogUsernames = !bc.LogUsernames;
                    uow.BotConfig.Update(bc);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                await ConfirmLocalized("unh_log_global", GetActiveText(logging)).ConfigureAwait(false);
            }

            [NadekoCommand, Description, Usage, Aliases]
            [OwnerOnly]
            public async Task ToggleUsernameHistoryGuild(bool? toggle = null, IGuild guild = null) {
                guild = guild ?? Context.Guild;
                if (guild == null)
                {
                    await ErrorLocalized("unh_guild_null").ConfigureAwait(false);
                    return;
                }

                bool? loggingBefore;
                bool globalLogging;
                using (var uow = _db.UnitOfWork) {
                    globalLogging = uow.BotConfig.GetOrCreate(set => set).LogUsernames;
                    var gc = uow.GuildConfigs.For(guild.Id, set => set);
                    loggingBefore = gc.LogUsernameHistory;
                    if (loggingBefore == toggle)
                    {
                        await ErrorLocalized("unh_guild_log_equals", guild.Name, GetActiveText(toggle)).ConfigureAwait(false);
                        return;
                    }
                    gc.LogUsernameHistory = toggle;
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                await Context.Channel.SendConfirmAsync(GetText("unh_log_guild", guild.Name, GetActiveText(loggingBefore), GetActiveText(toggle)).Trim() + " " + GetText("unh_log_global_append", GetActiveText(globalLogging)).Trim()).ConfigureAwait(false);
            }

            [NadekoCommand, Description, Usage, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task UsernameHistory(IGuildUser user = null, int page = 1) {
                user = user ?? (IGuildUser) Context.User;
                List<UsernameHistoryModel> usernicknames;
                using (var uow = _db.UnitOfWork) {
                    var nicknames = uow.NicknameHistory.GetGuildUserNames(user.GuildId, user.Id);
                    var usernames = uow.UsernameHistory.GetUserNames(user.Id);
                    usernicknames = usernames.Concat(nicknames).OrderByDescending(u => u.DateSet).ToList();
                }

                if (!usernicknames.Any()) {
                    await ErrorLocalized("unh_no_names", user.ToString()).ConfigureAwait(false);
                    return;
                }
                if (page < 1) page = 1;

                const int elementsPerPage = 10;
                var pagecount = (int)Math.Ceiling(usernicknames.Count / (elementsPerPage * 1d));
                if (page > pagecount) page = pagecount;
                await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page - 1, p => {
                        var embed = new EmbedBuilder()
                            .WithOkColor()
                            .WithTitle(GetText("unh_title", user.ToString()))
                            .WithDescription(string.Join("\n",
                                usernicknames.Skip(p * elementsPerPage).Take(elementsPerPage).Select(uhm =>
                                    $"- `{uhm.Name}#{uhm.DiscordDiscriminator:D4}`{(uhm is NicknameHistoryModel ? "" : " **(G)**")} - {uhm.DateSet:dd.MM.yyyy t}{(uhm.DateReplaced.HasValue ? $" => {uhm.DateReplaced.Value:dd.MM.yyyy t}" : "")}")));
                        return embed;
                    }, pagecount - 1).ConfigureAwait(false);
            }

            private string GetActiveText(bool? setting)
                => GetText(setting.HasValue ? setting.Value ? "unh_active" : "unh_inactive" : "unh_global");

            [NadekoCommand, Description, Aliases, Usage]
            [OwnerOnly]
            public async Task UpdateUsernames() {
                var (nicks, usernames, users, time) = await Service.UpdateUsernames().ConfigureAwait(false);
                await ConfirmLocalized("unh_update_usernames", nicks, usernames, users, $"{time.TotalSeconds:F2}s").ConfigureAwait(false);
            }
        }
    }
}