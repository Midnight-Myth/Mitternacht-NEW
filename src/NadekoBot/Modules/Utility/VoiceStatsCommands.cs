﻿using Discord;
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
                    if (uow.VoiceChannelStats.TryGetTime(user.Id, user.GuildId, out var time))
                        await ConfirmLocalized("voicestats_time", user.ToString(), TimeSpan.FromSeconds(time).ToString("hh':'mm':'ss")).ConfigureAwait(false);
                    else
                        await ConfirmLocalized("voicestats_untracked", user.ToString()).ConfigureAwait(false);
                }
            }

            [MitternachtCommand, Description, Usage, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOrGuildPermission(GuildPermission.Administrator)]
            public async Task VoiceStatsReset(IGuildUser user)
            {
                if (user == null) return;
                using(var uow = _db.UnitOfWork)
                {
                    uow.VoiceChannelStats.Reset(user.Id, user.GuildId);
                    await ConfirmLocalized("voicestats_reset", user.ToString()).ConfigureAwait(false);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
