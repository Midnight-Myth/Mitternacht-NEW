using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Administration.Services;

namespace Mitternacht.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class TimeZoneCommands : MitternachtSubmodule<GuildTimezoneService>
        {
            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Timezones(int page = 1)
            {
                page--;

                if (page < 0 || page > 20)
                    return;

                var timezones = TimeZoneInfo.GetSystemTimeZones()
                    .OrderBy(x => x.BaseUtcOffset)
                    .ToArray();
                const int timezonesPerPage = 20;

                await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page, 
                    curPage => new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle(GetText("timezones_available"))
                        .WithDescription(string.Join("\n", timezones.Skip(curPage * timezonesPerPage).Take(timezonesPerPage).Select(x => $"`{x.Id,-25}` {(x.BaseUtcOffset < TimeSpan.Zero? "-" : "+")}{x.BaseUtcOffset:hhmm}"))),
                    timezones.Length / timezonesPerPage, reactUsers: new[] { Context.User as IGuildUser });
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Timezone([Remainder] string id = null)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    await ReplyConfirmLocalized("timezone_guild", Service.GetTimeZoneOrUtc(Context.Guild.Id)).ConfigureAwait(false);
                    return;
                }

                TimeZoneInfo tz;
                try { tz = TimeZoneInfo.FindSystemTimeZoneById(id); } catch { tz = null; }

                Service.SetTimeZone(Context.Guild.Id, tz);

                if (tz == null)
                {
                    await ReplyErrorLocalized("timezone_not_found").ConfigureAwait(false);
                    return;
                }

                await Context.Channel.SendConfirmAsync(tz.ToString()).ConfigureAwait(false);
            }
        }
    }
}
