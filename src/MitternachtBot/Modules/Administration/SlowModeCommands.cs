using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Administration.Common;
using Mitternacht.Modules.Administration.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class SlowModeCommands : MitternachtSubmodule<SlowmodeService>
        {
            private readonly DbService _db;

            public SlowModeCommands(DbService db)
            {
                _db = db;
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task SlowmodeNative(int? msgPerSec = null)
            {
                if(Context.Channel is ITextChannel tch)
                {
                    if (!msgPerSec.HasValue)
                        msgPerSec = 0;

                    if (tch.SlowModeInterval == 0 && msgPerSec.Value == 0)
                    {
                        await ReplyErrorLocalized("slowmode_already_disabled").ConfigureAwait(false);
                        return;
                    }

                    try
                    {
                        await tch.ModifyAsync((TextChannelProperties tcp) => tcp.SlowModeInterval = msgPerSec.Value).ConfigureAwait(false);
                        await ReplyConfirmLocalized(msgPerSec.Value == 0 ? "slowmode_disabled" : "slowmode_enabled").ConfigureAwait(false);
                    }
                    catch (ArgumentException e)
                    {
                        await Context.Channel.SendErrorAsync(e.Message, GetText("invalid_params")).ConfigureAwait(false);
                    }
                }
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task Slowmode()
            {
                if (Service.RatelimitingChannels.TryRemove(Context.Channel.Id, out Ratelimiter removed))
                {
                    removed.CancelSource.Cancel();
                    await ReplyConfirmLocalized("slowmode_disabled").ConfigureAwait(false);
                }
                else await ReplyErrorLocalized("slowmode_already_disabled").ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task Slowmode(int msg, int perSec)
            {
                await Slowmode().ConfigureAwait(false); // disable if exists

                if (msg < 1 || perSec < 1 || msg > 100 || perSec > 3600)
                {
                    await ReplyErrorLocalized("invalid_params").ConfigureAwait(false);
                    return;
                }
                var toAdd = new Ratelimiter(Service)
                {
                    ChannelId = Context.Channel.Id,
                    MaxMessages = msg,
                    PerSeconds = perSec,
                };
                if (Service.RatelimitingChannels.TryAdd(Context.Channel.Id, toAdd))
                {
                    await Context.Channel.SendConfirmAsync(GetText("slowmode_desc", Format.Bold(toAdd.MaxMessages.ToString()), Format.Bold(toAdd.PerSeconds.ToString())),
						GetText("slowmode_enabled"))
												.ConfigureAwait(false);
                }
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [Priority(0)]
            public async Task SlowmodeWhitelist(IGuildUser user)
            {
                var siu = new SlowmodeIgnoredUser
                {
                    UserId = user.Id
                };

                HashSet<SlowmodeIgnoredUser> usrs;
                bool removed;
                using (var uow = _db.UnitOfWork)
                {
                    usrs = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.SlowmodeIgnoredUsers))
                        .SlowmodeIgnoredUsers;

                    if (!(removed = usrs.Remove(siu)))
                        usrs.Add(siu);

                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                Service.IgnoredUsers.AddOrUpdate(Context.Guild.Id, new HashSet<ulong>(usrs.Select(x => x.UserId)), (key, old) => new HashSet<ulong>(usrs.Select(x => x.UserId)));

                if (removed)
                    await ReplyConfirmLocalized("slowmodewl_user_stop", Format.Bold(user.ToString())).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("slowmodewl_user_start", Format.Bold(user.ToString())).ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [Priority(1)]
            public async Task SlowmodeWhitelist(IRole role)
            {
                var sir = new SlowmodeIgnoredRole
                {
                    RoleId = role.Id
                };

                HashSet<SlowmodeIgnoredRole> roles;
                bool removed;
                using (var uow = _db.UnitOfWork)
                {
                    roles = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.SlowmodeIgnoredRoles))
                        .SlowmodeIgnoredRoles;

                    if (!(removed = roles.Remove(sir)))
                        roles.Add(sir);

                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                Service.IgnoredRoles.AddOrUpdate(Context.Guild.Id, new HashSet<ulong>(roles.Select(x => x.RoleId)), (key, old) => new HashSet<ulong>(roles.Select(x => x.RoleId)));

                if (removed)
                    await ReplyConfirmLocalized("slowmodewl_role_stop", Format.Bold(role.ToString())).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("slowmodewl_role_start", Format.Bold(role.ToString())).ConfigureAwait(false);
            }
        }
    }
}