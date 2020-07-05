using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Utility.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class CommandMapCommands : MitternachtSubmodule<CommandMapService>
        {
            private readonly IUnitOfWork uow;
            private readonly DiscordSocketClient _client;

            public CommandMapCommands(IUnitOfWork uow, DiscordSocketClient client)
            {
                this.uow = uow;
                _client = client;
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireUserPermission(GuildPermission.Administrator)]
            [RequireContext(ContextType.Guild)]
            public async Task Alias(string trigger, [Remainder] string mapping = null)
            {
                var channel = (ITextChannel)Context.Channel;

                if (string.IsNullOrWhiteSpace(trigger))
                    return;

                trigger = trigger.Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(mapping))
                {
                    if (!Service.AliasMaps.TryGetValue(Context.Guild.Id, out var maps) ||
                        !maps.TryRemove(trigger, out _))
                    {
                        await ReplyErrorLocalized("alias_remove_fail", Format.Code(trigger)).ConfigureAwait(false);
                        return;
                    }

                    var config = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.CommandAliases));
                    var toAdd = new CommandAlias()
                    {
                        Mapping = mapping,
                        Trigger = trigger
                    };
                    config.CommandAliases.RemoveWhere(x => x.Trigger == trigger);
                    uow.SaveChanges(false);

                    await ReplyConfirmLocalized("alias_removed", Format.Code(trigger)).ConfigureAwait(false);
                    return;
                }
                Service.AliasMaps.AddOrUpdate(Context.Guild.Id, (_) =>
                {
                    var config = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.CommandAliases));
                    config.CommandAliases.Add(new CommandAlias()
                    {
                        Mapping = mapping,
                        Trigger = trigger
                    });
                    uow.SaveChanges(false);

                    return new ConcurrentDictionary<string, string>(new Dictionary<string, string>() {
                        {trigger.Trim().ToLowerInvariant(), mapping.ToLowerInvariant() },
                    });
                }, (_, map) =>
                {
                    var config = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.CommandAliases));
                    var toAdd = new CommandAlias()
                    {
                        Mapping = mapping,
                        Trigger = trigger
                    };
                    config.CommandAliases.RemoveWhere(x => x.Trigger == trigger);
                    config.CommandAliases.Add(toAdd);
                    uow.SaveChanges(false);

                    map.AddOrUpdate(trigger, mapping, (key, old) => mapping);
                    return map;
                });

                await ReplyConfirmLocalized("alias_added", Format.Code(trigger), Format.Code(mapping)).ConfigureAwait(false);
            }


            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task AliasList(int page = 1)
            {
                var channel = (ITextChannel)Context.Channel;
                page -= 1;

                if (page < 0)
                    return;

                if (!Service.AliasMaps.TryGetValue(Context.Guild.Id, out var maps) || !maps.Any())
                {
                    await ReplyErrorLocalized("aliases_none").ConfigureAwait(false);
                    return;
                }

                var arr = maps.ToArray();

                await Context.Channel.SendPaginatedConfirmAsync(_client, page, curPage =>
                {
                    return new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("alias_list"))
                    .WithDescription(string.Join("\n",
                        arr.Skip(curPage * 10).Take(10).Select(x => $"`{x.Key}` => `{x.Value}`")));

                }, arr.Length / 10, reactUsers: new[] { Context.User as IGuildUser }).ConfigureAwait(false);
            }
        }
    }
}