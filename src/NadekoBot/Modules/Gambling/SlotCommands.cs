using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Services;

namespace Mitternacht.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class SlotCommands : MitternachtSubmodule
        {
            private static int _totalBet;
            private static int _totalPaidOut;

            private static readonly HashSet<ulong> RunningUsers = new HashSet<ulong>();
            private readonly IBotConfigProvider _bc;

            private readonly string[] _emojis = { ":butterfly:", ":heart:", ":dolphin:", ":sun_with_face:", ":green_apple:", ":cherry_blossom:" };

            //here is a payout chart
            //https://lh6.googleusercontent.com/-i1hjAJy_kN4/UswKxmhrbPI/AAAAAAAAB1U/82wq_4ZZc-Y/DE6B0895-6FC1-48BE-AC4F-14D1B91AB75B.jpg
            //thanks to judge for helping me with this

            private readonly CurrencyService _cs;

            public SlotCommands(IBotConfigProvider bc, CurrencyService cs)
            {
                _bc = bc;
                _cs = cs;
            }

            public class SlotMachine
            {
                public const int MaxValue = 5;

                static readonly List<Func<int[], int>> WinningCombos = new List<Func<int[], int>>
                {
                    //three flowers
                    arr => arr.All(a=>a==MaxValue) ? 30 : 0,
                    //three of the same
                    arr => !arr.Any(a => a != arr[0]) ? 10 : 0,
                    //two flowers
                    arr => arr.Count(a => a == MaxValue) == 2 ? 4 : 0,
                    //one flower
                    arr => arr.Any(a => a == MaxValue) ? 1 : 0
                };

                public static SlotResult Pull()
                {
                    var numbers = new int[3];
                    for (var i = 0; i < numbers.Length; i++)
                    {
                        numbers[i] = new NadekoRandom().Next(0, MaxValue + 1);
                    }
                    var multi = 0;
                    foreach (var t in WinningCombos)
                    {
                        multi = t(numbers);
                        if (multi != 0)
                            break;
                    }

                    return new SlotResult(numbers, multi);
                }

                public struct SlotResult
                {
                    public int[] Numbers { get; }
                    public int Multiplier { get; }
                    public SlotResult(int[] nums, int multi)
                    {
                        Numbers = nums;
                        Multiplier = multi;
                    }
                }
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task SlotStats()
            {
                //i remembered to not be a moron
                var paid = _totalPaidOut;
                var bet = _totalBet;

                if (bet <= 0)
                    bet = 1;

                var embed = new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle("Slot Stats")
                    .AddField(efb => efb.WithName("Total Bet").WithValue(bet.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName("Paid Out").WithValue(paid.ToString()).WithIsInline(true))
                    .WithFooter(efb => efb.WithText($"Payout Rate: {paid * 1.0 / bet * 100:f4}%"));

                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task SlotTest(int tests = 1000)
            {
                if (tests <= 0)
                    return;
                //multi vs how many times it occured
                var dict = new Dictionary<int, int>();
                for (var i = 0; i < tests; i++)
                {
                    var res = SlotMachine.Pull();
                    if (dict.ContainsKey(res.Multiplier))
                        dict[res.Multiplier] += 1;
                    else
                        dict.Add(res.Multiplier, 1);
                }

                var sb = new StringBuilder();
                const int bet = 1;
                var payout = 0;
                foreach (var key in dict.Keys.OrderByDescending(x => x))
                {
                    sb.AppendLine($"x{key} occured {dict[key]} times. {dict[key] * 1.0f / tests * 100}%");
                    payout += key * dict[key];
                }
                await Context.Channel.SendConfirmAsync("Slot Test Results", sb.ToString(),
                    footer: $"Total Bet: {tests * bet} | Payout: {payout * bet} | {payout * 1.0f / tests * 100}%");
            }

            [MitternachtCommand, Usage, Description, Aliases]
            public async Task Slot(int amount = 0)
            {
                if (!RunningUsers.Add(Context.User.Id)) return;

                try
                {
                    if (amount < 1)
                    {
                        await ReplyErrorLocalized("min_bet_limit", 1 + _bc.BotConfig.CurrencySign).ConfigureAwait(false);
                        return;
                    }
                    const int maxAmount = 9999;
                    if (amount > maxAmount)
                    {
                        GetText("slot_maxbet", maxAmount + _bc.BotConfig.CurrencySign);
                        await ReplyErrorLocalized("max_bet_limit", maxAmount + _bc.BotConfig.CurrencySign).ConfigureAwait(false);
                        return;
                    }

                    if (!await _cs.RemoveAsync(Context.User, "Slot Machine", amount, false))
                    {
                        await ReplyErrorLocalized("not_enough", _bc.BotConfig.CurrencySign).ConfigureAwait(false);
                        return;
                    }

                    Interlocked.Add(ref _totalBet, amount);
                    var result = SlotMachine.Pull();
                    var numbers = result.Numbers;
                    var won = amount * result.Multiplier;

                    var msg = result.Multiplier != 0 ? "" : GetText("better_luck");
                    if (result.Multiplier != 0)
                    {
                        await _cs.AddAsync(Context.User, $"Slot Machine x{result.Multiplier}", amount * result.Multiplier, false);
                        Interlocked.Add(ref _totalPaidOut, amount * result.Multiplier);
                        switch (result.Multiplier) {
                            case 1:
                                msg = GetText("slot_single", _bc.BotConfig.CurrencySign, 1);
                                break;
                            case 4:
                                msg = GetText("slot_two", _bc.BotConfig.CurrencySign, 4);
                                break;
                            case 10:
                                msg = GetText("slot_three", 10);
                                break;
                            case 30:
                                msg = GetText("slot_jackpot", 30);
                                break;
                        }
                    }

                    await Context.Channel.SendMessageAsync($"{Context.User.Mention} {msg}\n`{GetText("slot_bet")}:`{amount} `{GetText("slot_won")}:` {won}{_bc.BotConfig.CurrencySign}\n{_emojis[numbers[0]] + _emojis[numbers[1]] + _emojis[numbers[2]]}").ConfigureAwait(false);
                }
                finally
                {
                    var _ = Task.Run(async () =>
                    {
                        await Task.Delay(1500);
                        RunningUsers.Remove(Context.User.Id);
                    });
                }
            }
        }
    }
}