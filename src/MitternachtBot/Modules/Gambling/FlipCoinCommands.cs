using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using SixLabors.ImageSharp;
using Mitternacht.Common;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Image = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.PixelFormats;
using Mitternacht.Services.Impl;

namespace Mitternacht.Modules.Gambling {
	public partial class Gambling {
		[Group]
		public class FlipCoinCommands : MitternachtSubmodule {
			private readonly IImagesService _images;
			private readonly IBotConfigProvider _bc;
			private readonly CurrencyService _cs;

			private readonly NadekoRandom rng = new NadekoRandom();

			public FlipCoinCommands(IImagesService images, CurrencyService cs, IBotConfigProvider bc) {
				_images = images;
				_bc = bc;
				_cs = cs;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task Flip(int count = 1) {
				if(count == 1) {
					if(rng.Next(0, 2) == 1) {
						using var heads = _images.Heads.ToStream();
						await Context.Channel.SendFileAsync(heads, "heads.jpg", $"{Context.User.Mention} {GetText("flipped", Format.Bold(GetText("heads")))}").ConfigureAwait(false);
					} else {
						using var tails = _images.Tails.ToStream();
						await Context.Channel.SendFileAsync(tails, "tails.jpg", $"{Context.User.Mention} {GetText("flipped", Format.Bold(GetText("tails")))}").ConfigureAwait(false);
					}
					return;
				}
				if(count > 10 || count < 1) {
					await ReplyErrorLocalized("flip_invalid", 10).ConfigureAwait(false);
					return;
				}
				var imgs = new Image<Rgba32>[count];
				for(var i = 0; i < count; i++) {
					using var heads = _images.Heads.ToStream();
					using var tails = _images.Tails.ToStream();
					imgs[i] = rng.Next(0, 10) < 5 ? Image.Load<Rgba32>(heads) : Image.Load<Rgba32>(tails);
				}
				await Context.Channel.SendFileAsync(imgs.Merge().ToStream(), $"{count} coins.png").ConfigureAwait(false);
			}

			public enum BetFlipGuess {
				H = 1,
				Head = 1,
				Heads = 1,
				T = 2,
				Tail = 2,
				Tails = 2
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task Betflip(int amount, BetFlipGuess guess) {
				var user = (IGuildUser) Context.User;

				if(amount < _bc.BotConfig.MinimumBetAmount) {
					await ReplyErrorLocalized("min_bet_limit", $"{_bc.BotConfig.MinimumBetAmount}{_bc.BotConfig.CurrencySign}").ConfigureAwait(false);
					return;
				}
				var removed = await _cs.RemoveAsync(user, "Betflip Gamble", amount).ConfigureAwait(false);
				if(!removed) {
					await ReplyErrorLocalized("not_enough", _bc.BotConfig.CurrencyPluralName).ConfigureAwait(false);
					return;
				}
				
				(IEnumerable<byte> imageToSend, var result) = rng.Next(0, 2) == 1 ? (_images.Heads, BetFlipGuess.Heads) : (_images.Tails, BetFlipGuess.Tails);

				string str;
				if(guess == result) {
					var toWin = (int)Math.Round(amount * _bc.BotConfig.BetflipMultiplier);
					str = $"{user.Mention} {GetText("flip_guess", $"{toWin}{_bc.BotConfig.CurrencySign}")}";
					await _cs.AddAsync(user, "Betflip Gamble", toWin).ConfigureAwait(false);
				} else {
					str = $"{user.Mention} {GetText("better_luck")}";
				}

				using var toSend = imageToSend.ToStream();
				await Context.Channel.SendFileAsync(toSend, "result.png", str).ConfigureAwait(false);
			}
		}
	}
}