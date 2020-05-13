using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Common.Attributes;
using Mitternacht.Common.TypeReaders;
using Mitternacht.Extensions;
using Mitternacht.Modules.Utility.Common;
using Mitternacht.Modules.Utility.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class RepeatCommands : MitternachtSubmodule<MessageRepeaterService>
        {
            private readonly DiscordSocketClient _client;
            private readonly DbService _db;

            public RepeatCommands(DiscordSocketClient client, DbService db)
            {
                _client = client;
                _db = db;
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task RepeatInvoke(int index)
            {
                if (!Service.RepeaterReady)
                    return;
                index -= 1;
                if (!Service.Repeaters.TryGetValue(Context.Guild.Id, out var rep))
                {
                    await ReplyErrorLocalized("repeat_invoke_none").ConfigureAwait(false);
                    return;
                }

                var repList = rep.ToList();

                if (index >= repList.Count)
                {
                    await ReplyErrorLocalized("index_out_of_range").ConfigureAwait(false);
                    return;
                }
                var repeater = repList[index].Repeater;
                repList[index].Reset();
                await repList[index].Trigger();

                await Context.Channel.SendMessageAsync("🔄").ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task RepeatRemove(int index)
            {
                if (!Service.RepeaterReady)
                    return;
                if (index < 1)
                    return;
                index -= 1;

                if (!Service.Repeaters.TryGetValue(Context.Guild.Id, out var rep))
                    return;

                var repeaterList = rep.ToList();

                if (index >= repeaterList.Count)
                {
                    await ReplyErrorLocalized("index_out_of_range").ConfigureAwait(false);
                    return;
                }

                var repeater = repeaterList[index];
                repeater.Stop();
                repeaterList.RemoveAt(index);

                using (var uow = _db.UnitOfWork)
                {
                    var guildConfig = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(gc => gc.GuildRepeaters));

                    guildConfig.GuildRepeaters.RemoveWhere(r => r.Id == repeater.Repeater.Id);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                if (Service.Repeaters.TryUpdate(Context.Guild.Id, new ConcurrentQueue<RepeatRunner>(repeaterList), rep))
                    await Context.Channel.SendConfirmAsync(GetText("message_repeater"),
                        GetText("repeater_stopped", index + 1) + $"\n\n{repeater}").ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [Priority(0)]
            public async Task Repeat(int minutes, [Remainder] string message)
            {
                if (!Service.RepeaterReady)
                    return;
                if (minutes < 1 || minutes > 10080)
                    return;

                if (string.IsNullOrWhiteSpace(message))
                    return;

                var toAdd = new GuildRepeater()
                {
                    ChannelId = Context.Channel.Id,
                    GuildId = Context.Guild.Id,
                    Interval = TimeSpan.FromMinutes(minutes),
                    Message = message
                };

                using (var uow = _db.UnitOfWork)
                {
                    var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.GuildRepeaters));

                    if (gc.GuildRepeaters.Count >= 5)
                        return;
                    gc.GuildRepeaters.Add(toAdd);

                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                var rep = new RepeatRunner(_client, (SocketGuild)Context.Guild, toAdd);

                Service.Repeaters.AddOrUpdate(Context.Guild.Id, new ConcurrentQueue<RepeatRunner>(new[] { rep }), (key, old) =>
                {
                    old.Enqueue(rep);
                    return old;
                });

                await Context.Channel.SendConfirmAsync(
                    "🔁 " + GetText("repeater",
                        Format.Bold(((IGuildUser)Context.User).GuildPermissions.MentionEveryone ? rep.Repeater.Message : rep.Repeater.Message.SanitizeMentions()),
                        Format.Bold(rep.Repeater.Interval.Days.ToString()),
                        Format.Bold(rep.Repeater.Interval.Hours.ToString()),
                        Format.Bold(rep.Repeater.Interval.Minutes.ToString()))).ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [Priority(1)]
            public async Task Repeat(GuildDateTime gt, [Remainder] string message)
            {
                if (!Service.RepeaterReady)
                    return;

                if (string.IsNullOrWhiteSpace(message))
                    return;

                var toAdd = new GuildRepeater()
                {
                    ChannelId = Context.Channel.Id,
                    GuildId = Context.Guild.Id,
                    Interval = TimeSpan.FromHours(24),
                    StartTimeOfDay = gt.InputTimeUtc.TimeOfDay,
                    Message = message
                };

                using (var uow = _db.UnitOfWork)
                {
                    var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.GuildRepeaters));

                    if (gc.GuildRepeaters.Count >= 5)
                        return;
                    gc.GuildRepeaters.Add(toAdd);

                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                var rep = new RepeatRunner(_client, (SocketGuild)Context.Guild, toAdd);

                Service.Repeaters.AddOrUpdate(Context.Guild.Id, new ConcurrentQueue<RepeatRunner>(new[] { rep }), (key, old) =>
                {
                    old.Enqueue(rep);
                    return old;
                });

                var secondPart = GetText("repeater_initial",
                    Format.Bold(rep.InitialInterval.Hours.ToString()),
                    Format.Bold(rep.InitialInterval.Minutes.ToString()));

                await Context.Channel.SendConfirmAsync(
                    "🔁 " + GetText("repeater",
                        Format.Bold(((IGuildUser)Context.User).GuildPermissions.MentionEveryone ? rep.Repeater.Message : rep.Repeater.Message.SanitizeMentions()),
                        Format.Bold(rep.Repeater.Interval.Days.ToString()),
                        Format.Bold(rep.Repeater.Interval.Hours.ToString()),
                        Format.Bold(rep.Repeater.Interval.Minutes.ToString())) + " " + secondPart).ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task RepeatList()
            {
                if (!Service.RepeaterReady)
                    return;
                if (!Service.Repeaters.TryGetValue(Context.Guild.Id, out var repRunners))
                {
                    await ReplyConfirmLocalized("repeaters_none").ConfigureAwait(false);
                    return;
                }

                var replist = repRunners.ToList();
                var sb = new StringBuilder();

                for (var i = 0; i < replist.Count; i++)
                {
                    var rep = replist[i];

                    sb.AppendLine($"`{i + 1}.` {rep}");
                }
                var desc = sb.ToString();

                if (string.IsNullOrWhiteSpace(desc))
                    desc = GetText("no_active_repeaters");

                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                        .WithTitle(GetText("list_of_repeaters"))
                        .WithDescription(desc))
                    .ConfigureAwait(false);
            }
        }
    }
}