﻿using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using GommeHDnetForumAPI.DataModels;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Forum.Services;
using Mitternacht.Modules.Verification.Services;
using Mitternacht.Services;

namespace Mitternacht.Modules.Verification
{
    public partial class Verification
    {
        public class GommeTeamSyncCommands : MitternachtSubmodule<GommeTeamSyncService>
        {
            private readonly DbService _db;
            private readonly ForumService _fs;

            public GommeTeamSyncCommands(DbService db, ForumService fs)
            {
                _db = db;
                _fs = fs;
            }


            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task GommeTeamRole()
            {
                using (var uow = _db.UnitOfWork)
                {
                    var gc = uow.GuildConfigs.For(Context.Guild.Id);
                    var gtmr = gc.GommeTeamMemberRoleId;
                    var role = gtmr == null ? null : Context.Guild.GetRole(gtmr.Value);
                    await ReplyConfirmLocalized("gtr", Format.Bold(role?.Name ?? gtmr?.ToString() ?? GetText("gtr_not_set"))).ConfigureAwait(false);
                }
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task GommeTeamRoleSet(IRole role = null)
            {
                using (var uow = _db.UnitOfWork)
                {
                    var gc = uow.GuildConfigs.For(Context.Guild.Id);
                    var oldRoleId = gc.GommeTeamMemberRoleId;
                    var oldRole = oldRoleId == null ? null : Context.Guild.GetRole(oldRoleId.Value);
                    gc.GommeTeamMemberRoleId = role?.Id;
                    uow.GuildConfigs.Update(gc);
                    await uow.CompleteAsync().ConfigureAwait(false);
                    await ReplyConfirmLocalized("gtr_set", Format.Bold(oldRole?.Name ?? oldRoleId?.ToString() ?? GetText("gtr_not_set")), Format.Bold(role?.Name ?? GetText("gtr_not_set"))).ConfigureAwait(false);
                }
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task GommeTeamRanks()
            {
                var memberslist = await _fs.Forum.GetMembersList(MembersListType.Staff).ConfigureAwait(false);
                var ranks = memberslist.GroupBy(ui => ui.UserTitle).Select(g => $"- {g.Key} ({g.Count()})").ToList();
                var embed = new EmbedBuilder().WithOkColor().WithTitle(GetText("ranks_title", ranks.Count)).WithDescription(string.Join("\n", ranks));
                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

        }
    }
}