using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Administration.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class ServerGreetCommands : MitternachtSubmodule<GreetSettingsService>
        {
            private readonly DbService _db;

            public ServerGreetCommands(DbService db)
            {
                _db = db;
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task GreetDel(int timer = 30)
            {
                if (timer < 0 || timer > 600)
                    return;

                await Service.SetGreetDel(Context.Guild.Id, timer).ConfigureAwait(false);

                if (timer > 0)
                    await ReplyConfirmLocalized("greetdel_on", timer).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("greetdel_off").ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task Greet()
            {
                var enabled = await Service.SetGreet(Context.Guild.Id, Context.Channel.Id).ConfigureAwait(false);

                if (enabled)
                    await ReplyConfirmLocalized("greet_on").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("greet_off").ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task GreetMsg([Remainder] string text = null)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    string channelGreetMessageText;
                    using (var uow = _db.UnitOfWork)
                    {
                        channelGreetMessageText = uow.GuildConfigs.For(Context.Guild.Id, set => set).ChannelGreetMessageText;
                    }
                    await ReplyConfirmLocalized("greetmsg_cur", channelGreetMessageText?.SanitizeMentions()).ConfigureAwait(false);
                    return;
                }

                var sendGreetEnabled = Service.SetGreetMessage(Context.Guild.Id, ref text);

                await ReplyConfirmLocalized("greetmsg_new").ConfigureAwait(false);
                if (!sendGreetEnabled)
                    await ReplyConfirmLocalized("greetmsg_enable", $"`{Prefix}greet`").ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task GreetDm()
            {
                var enabled = await Service.SetGreetDm(Context.Guild.Id).ConfigureAwait(false);

                if (enabled)
                    await ReplyConfirmLocalized("greetdm_on").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("greetdm_off").ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task GreetDmMsg([Remainder] string text = null)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    GuildConfig config;
                    using (var uow = _db.UnitOfWork)
                    {
                        config = uow.GuildConfigs.For(Context.Guild.Id);
                    }
                    await ReplyConfirmLocalized("greetdmmsg_cur", config.DmGreetMessageText?.SanitizeMentions()).ConfigureAwait(false);
                    return;
                }

                var sendGreetEnabled = Service.SetGreetDmMessage(Context.Guild.Id, ref text);

                await ReplyConfirmLocalized("greetdmmsg_new").ConfigureAwait(false);
                if (!sendGreetEnabled)
                    await ReplyConfirmLocalized("greetdmmsg_enable", $"`{Prefix}greetdm`").ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task Bye()
            {
                var enabled = await Service.SetBye(Context.Guild.Id, Context.Channel.Id).ConfigureAwait(false);

                if (enabled)
                    await ReplyConfirmLocalized("bye_on").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("bye_off").ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task ByeMsg([Remainder] string text = null)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    string byeMessageText;
                    using (var uow = _db.UnitOfWork)
                    {
                        byeMessageText = uow.GuildConfigs.For(Context.Guild.Id, set => set).ChannelByeMessageText;
                    }
                    await ReplyConfirmLocalized("byemsg_cur", byeMessageText?.SanitizeMentions()).ConfigureAwait(false);
                    return;
                }

                var sendByeEnabled = Service.SetByeMessage(Context.Guild.Id, ref text);

                await ReplyConfirmLocalized("byemsg_new").ConfigureAwait(false);
                if (!sendByeEnabled)
                    await ReplyConfirmLocalized("byemsg_enable", $"`{Prefix}bye`").ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task ByeDel(int timer = 30)
            {
                await Service.SetByeDel(Context.Guild.Id, timer).ConfigureAwait(false);

                if (timer > 0)
                    await ReplyConfirmLocalized("byedel_on", timer).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("byedel_off").ConfigureAwait(false);
            }

        }
    }
}