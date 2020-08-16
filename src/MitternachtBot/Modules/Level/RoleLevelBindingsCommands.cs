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
using Mitternacht.Services.Database;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Level
{
    public partial class Level
    {
        public class RoleLevelBindingsCommands : MitternachtSubmodule
        {
            private readonly IUnitOfWork uow;

            public RoleLevelBindingsCommands(IUnitOfWork uow)
            {
                this.uow = uow;
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

                uow.RoleLevelBindings.SetBinding(role.Id, minlevel);
                await uow.SaveChangesAsync(false).ConfigureAwait(false);

                await ConfirmLocalized("rlb_set", role.Name, minlevel);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task RemoveRoleLevelBinding(IRole role)
            {
                var wasRemoved = uow.RoleLevelBindings.Remove(role.Id);
                await uow.SaveChangesAsync(false).ConfigureAwait(false);

                if (wasRemoved) await ConfirmLocalized("rlb_removed", role.Name).ConfigureAwait(false);
                else await ErrorLocalized("rlb_already_independent", role.Name).ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task RoleLevelBindings(int page = 1)
            {
                const int elementsPerPage = 9;

                var roleLevelBindings = uow.RoleLevelBindings.GetAll().OrderByDescending(r => r.MinimumLevel).ToList();

                if (!roleLevelBindings.Any())
                {
                    await ReplyErrorLocalized("rlb_none").ConfigureAwait(false);
                    return;
                }

                var pageCount = (int) Math.Ceiling(roleLevelBindings.Count * 1d / elementsPerPage);
                if (page > pageCount)
                {
                    await ReplyErrorLocalized("rlb_page_too_high").ConfigureAwait(false);
                    return;
                }

                if (page < 1) page = 1;

                await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page - 1, currentPage => {
                        var embed = new EmbedBuilder()
                            .WithTitle(GetText("rlb_title"));
                        var rlbs = roleLevelBindings.Skip(elementsPerPage * currentPage).Take(elementsPerPage).ToList();
                        foreach (var rlb in rlbs)
                        {
                            var rolename = Context.Guild.GetRole(rlb.RoleId)?.Name ?? rlb.RoleId.ToString();
                            embed.AddField($"#{elementsPerPage * currentPage + rlbs.IndexOf(rlb) + 1} - {rolename}",
                                rlb.MinimumLevel, true);
                        }

                        return embed;
                    }, pageCount, reactUsers: new[] { Context.User as IGuildUser }).ConfigureAwait(false);
            }
        }
    }
}