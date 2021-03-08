using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Services.Impl;
using Wof = Mitternacht.Modules.Gambling.Common.WheelOfFortune;

namespace Mitternacht.Modules.Gambling {
	public partial class Gambling {
		public class WheelOfFortuneCommands : MitternachtSubmodule {
			private readonly CurrencyService _cs;
			private readonly IBotConfigProvider _bc;

			public WheelOfFortuneCommands(CurrencyService cs, IBotConfigProvider bc) {
				_cs = cs;
				_bc = bc;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task WheelOfFortune(int bet) {
				const int minBet = 10;
				if(bet < minBet) {
					await ReplyErrorLocalized("min_bet_limit", minBet + _bc.BotConfig.CurrencySign).ConfigureAwait(false);
					return;
				}

				if(!await _cs.RemoveAsync((IGuildUser) Context.User, "Wheel Of Fortune - bet", bet).ConfigureAwait(false)) {
					await ReplyErrorLocalized("not_enough", _bc.BotConfig.CurrencySign).ConfigureAwait(false);
					return;
				}

				var wof = new Wof();

				var amount = (int)(bet * wof.Multiplier);

				if(amount > 0)
					await _cs.AddAsync((IGuildUser)Context.User, "Wheel Of Fortune - won", amount).ConfigureAwait(false);

				await Context.Channel.SendConfirmAsync(
Format.Bold($@"{Context.User} won: {amount + _bc.BotConfig.CurrencySign}

   ã€Ž{Wof.Multipliers[1]}ã€   ã€Ž{Wof.Multipliers[0]}ã€   ã€Ž{Wof.Multipliers[7]}ã€

ã€Ž{Wof.Multipliers[2]}ã€      {wof.Emoji}      ã€Ž{Wof.Multipliers[6]}ã€

     ã€Ž{Wof.Multipliers[3]}ã€   ã€Ž{Wof.Multipliers[4]}ã€   ã€Ž{Wof.Multipliers[5]}ã€")).ConfigureAwait(false);
			}
		}
	}
}