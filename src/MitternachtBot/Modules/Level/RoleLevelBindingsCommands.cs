using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Level
{
    public partial class Level
    {
        public class RoleLevelBindingsCommands : MitternachtSubmodule
        {
            private readonly DbService _db;

            public RoleLevelBindingsCommands(DbService db)
            {
                _db = db;
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task SetRoleLevelBinding(IRole role, int minlevel)
            {
                if (minlevel < 0)
                {
                    await ErrorLocalized("rlb_set_minlevel").ConfigureAwait(false);
                    return;
                }

                using (var uow = _db.UnitOfWork)
                {
                    uow.RoleLevelBinding.SetBinding(role.Id, minlevel);
                    await uow.SaveChangesAsync().ConfigureAwait(false);
                }

                await ConfirmLocalized("rlb_set", role.Name, minlevel);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task RemoveRoleLevelBinding(IRole role)
            {
                bool wasRemoved;
                using (var uow = _db.UnitOfWork)
                {
                    wasRemoved = uow.RoleLevelBinding.Remove(role.Id);
                    await uow.SaveChangesAsync().ConfigureAwait(false);
                }

                if (wasRemoved) await ConfirmLocalized("rlb_removed", role.Name).ConfigureAwait(false);
                else await ErrorLocalized("rlb_already_independent", role.Name).ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task RoleLevelBindings(int page = 1)
            {
                const int elementsPerPage = 9;

                List<RoleLevelBinding> roleLevelBindings;
                using (var uow = _db.UnitOfWork)
                {
                    roleLevelBindings = uow.RoleLevelBinding.GetAll().OrderByDescending(r => r.MinimumLevel).ToList();
                }

                if (!roleLevelBindings.Any())
                {
                    await ReplyErrorLocalized("rlb_none").ConfigureAwait(false);
                    return;
                }

                var pagecount = (int) Math.Ceiling(roleLevelBindings.Count * 1d / elementsPerPage);
                if (page > pagecount)
                {
                    await ReplyErrorLocalized("rlb_page_too_high").ConfigureAwait(false);
                    return;
                }

                if (page < 1) page = 1;

                await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page - 1, p =>
                    {
                        var embed = new EmbedBuilder()
                            .WithTitle(GetText("rlb_title"));
                        var rlbs = roleLevelBindings.Skip(elementsPerPage * p).Take(elementsPerPage).ToList();
                        foreach (var rlb in rlbs)
                        {
                            var rolename = Context.Guild.GetRole(rlb.RoleId)?.Name ?? rlb.RoleId.ToString();
                            embed.AddField($"#{elementsPerPage * p + rlbs.IndexOf(rlb) + 1} - {rolename}",
                                rlb.MinimumLevel, true);
                        }

                        return embed;
                    }, pagecount - 1, reactUsers: new[] { Context.User as IGuildUser }, hasPerms: gp => gp.KickMembers).ConfigureAwait(false);
            }
        }
    }
}