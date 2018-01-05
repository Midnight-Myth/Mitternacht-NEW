using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Utility.Services;
using Mitternacht.Services;

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
                    var bc = uow.BotConfig.GetOrCreate(set => set.Include(s => s.LogUsernames));
                    logging = bc.LogUsernames = !bc.LogUsernames;
                    uow.BotConfig.Update(bc);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                await ConfirmLocalized("unh_log_global", GetActiveText(logging)).ConfigureAwait(false);
            }

            [NadekoCommand, Description, Usage, Aliases]
            [OwnerOnly]
            public async Task ToggleUsernameHistoryGuild(IGuild guild, bool? toggle = null) {
                guild = guild ?? Context.Guild;
                if (guild == null)
                {
                    await ErrorLocalized("unh_guild_null").ConfigureAwait(false);
                    return;
                }

                bool? loggingBefore;
                bool globalLogging;
                using (var uow = _db.UnitOfWork) {
                    globalLogging = uow.BotConfig.GetOrCreate(set => set.Include(s => s.LogUsernames)).LogUsernames;
                    var gc = uow.GuildConfigs.For(guild.Id, set => set.Include(s => s.LogUsernameHistory));
                    loggingBefore = gc.LogUsernameHistory;
                    if (loggingBefore == toggle)
                    {
                        await ErrorLocalized("unh_guild_log_equals", guild.Name, GetActiveText(toggle)).ConfigureAwait(false);
                        return;
                    }
                    gc.LogUsernameHistory = toggle;
                }

                await Context.Channel.SendConfirmAsync(GetText("unh_log_guild", guild.Name, GetActiveText(loggingBefore), GetActiveText(toggle)).Trim() + " " + GetText("unh_log_global_append", GetActiveText(globalLogging)).Trim()).ConfigureAwait(false);
            }

            [NadekoCommand, Description, Usage, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task UsernameHistory(IGuildUser user)
            {

            }

            private string GetActiveText(bool? setting)
                => GetText(setting.HasValue ? setting.Value ? "unh_active" : "unh_inactive" : "unh_global");
        }
    }
}