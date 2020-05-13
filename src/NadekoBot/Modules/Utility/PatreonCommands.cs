using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Utility.Services;
using Mitternacht.Services;

namespace Mitternacht.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class PatreonCommands : MitternachtSubmodule<PatreonRewardsService>
        {
            private readonly IBotCredentials _creds;
            private readonly IBotConfigProvider _config;
            private readonly DbService _db;
            private readonly CurrencyService _currency;

            public PatreonCommands(IBotCredentials creds, IBotConfigProvider config, DbService db, CurrencyService currency)
            {
                _creds = creds;
                _config = config;
                _db = db;
                _currency = currency;
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [OwnerOnly]
            [RequireContext(ContextType.DM)]
            public async Task PatreonRewardsReload()
            {
                if (string.IsNullOrWhiteSpace(_creds.PatreonAccessToken))
                    return;
                await Service.RefreshPledges(true).ConfigureAwait(false);

                await Context.Channel.SendConfirmAsync("👌").ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.DM)]
            public async Task ClaimPatreonRewards()
            {
                if (string.IsNullOrWhiteSpace(_creds.PatreonAccessToken))
                    return;

                if (DateTime.UtcNow.Day < 5)
                {
                    await ReplyErrorLocalized("clpa_too_early").ConfigureAwait(false);
                    return;
                }
                int amount = 0;
                try
                {
                    amount = await Service.ClaimReward(Context.User.Id).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                }

                if (amount > 0)
                {
                    await ReplyConfirmLocalized("clpa_success", amount + _config.BotConfig.CurrencySign).ConfigureAwait(false);
                    return;
                }
                var rem = (Service.Interval - (DateTime.UtcNow - Service.LastUpdate));
                var helpcmd = Format.Code(Prefix + "donate");
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithDescription(GetText("clpa_fail"))
                    .AddField(efb => efb.WithName(GetText("clpa_fail_already_title")).WithValue(GetText("clpa_fail_already")))
                    .AddField(efb => efb.WithName(GetText("clpa_fail_wait_title")).WithValue(GetText("clpa_fail_wait")))
                    .AddField(efb => efb.WithName(GetText("clpa_fail_conn_title")).WithValue(GetText("clpa_fail_conn")))
                    .AddField(efb => efb.WithName(GetText("clpa_fail_sup_title")).WithValue(GetText("clpa_fail_sup", helpcmd)))
                    .WithFooter(efb => efb.WithText(GetText("clpa_next_update", rem))))
                    .ConfigureAwait(false);
            }
        }

    }
}