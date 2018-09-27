using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Modules.Utility.Services;
using Mitternacht.Services;
using System;
using System.Threading.Tasks;

namespace Mitternacht.Modules.Utility
{
    public partial class Utility
    {
        public class VoiceStatsCommands : MitternachtSubmodule<VoiceStatsService>
        {
            private readonly DbService _db;

            public VoiceStatsCommands(DbService db)
            {
                _db = db;
            }

            [MitternachtCommand, Description, Usage, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task VoiceStats(IGuildUser user = null)
            {
                user = user ?? Context.User as IGuildUser;

                using(var uow = _db.UnitOfWork)
                {
                    if (uow.VoiceChannelStats.TryGetTime(user.Id, out var time))
                        await ConfirmLocalized("voicestats_time", user.ToString(), TimeSpan.FromSeconds(time).ToString("hh':'mm':'ss")).ConfigureAwait(false);
                    else
                        await ConfirmLocalized("voicestats_untracked", user.ToString()).ConfigureAwait(false);
                }
            }
        }
    }
}
