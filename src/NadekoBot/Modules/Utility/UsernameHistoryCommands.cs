using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Common.Attributes;
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
                //todo: messages
            }

            [NadekoCommand, Description, Usage, Aliases]
            [OwnerOnly]
            public async Task ToggleUsernameHistoryGuild(IGuild guild, bool? toggle = null) {
                guild = guild ?? Context.Guild;
                if (guild == null) {
                    //todo: message
                    return;
                }

                bool? loggingBefore;
                bool globalLogging;
                using (var uow = _db.UnitOfWork) {
                    globalLogging = uow.BotConfig.GetOrCreate(set => set.Include(s => s.LogUsernames)).LogUsernames;
                    var gc = uow.GuildConfigs.For(guild.Id, set => set.Include(s => s.LogUsernameHistory));
                    loggingBefore = gc.LogUsernameHistory;
                    gc.LogUsernameHistory = toggle;
                }
                //todo: messages
            }
        }
    }
}