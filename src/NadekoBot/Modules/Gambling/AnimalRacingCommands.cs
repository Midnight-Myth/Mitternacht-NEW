﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Gambling.Common.AnimalRacing;
using Mitternacht.Modules.Gambling.Common.AnimalRacing.Exceptions;
using Mitternacht.Services;

namespace Mitternacht.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class AnimalRacingCommands : NadekoSubmodule
        {
            private readonly IBotConfigProvider _bc;
            private readonly CurrencyService _cs;
            private readonly DiscordSocketClient _client;


            public static ConcurrentDictionary<ulong, AnimalRace> AnimalRaces { get; } = new ConcurrentDictionary<ulong, AnimalRace>();

            public AnimalRacingCommands(IBotConfigProvider bc, CurrencyService cs, DiscordSocketClient client)
            {
                _bc = bc;
                _cs = cs;
                _client = client;
            }

            private IUserMessage raceMessage = null;

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public Task Race()
            {
                var ar = new AnimalRace(_cs, _bc.BotConfig.RaceAnimals.Shuffle().ToArray());
                if (!AnimalRaces.TryAdd(Context.Guild.Id, ar))
                    return Context.Channel.SendErrorAsync(GetText("animal_race"), GetText("animal_race_already_started"));
                ar.Initialize();

                ar.OnStartingFailed += Ar_OnStartingFailed;
                ar.OnStateUpdate += Ar_OnStateUpdate;
                ar.OnEnded += Ar_OnEnded;
                ar.OnStarted += Ar_OnStarted;

                return Context.Channel.SendConfirmAsync(GetText("animal_race"), GetText("animal_race_starting"),
                                    footer: GetText("animal_race_join_instr", Prefix));
            }

            private Task Ar_OnStarted(AnimalRace race)
            {
                if (race.Users.Length == race.MaxUsers)
                    return Context.Channel.SendConfirmAsync(GetText("animal_race"), GetText("animal_race_full"));
                else
                    return Context.Channel.SendConfirmAsync(GetText("animal_race"), GetText("animal_race_starting_with_x", race.Users.Length));
            }

            private Task Ar_OnEnded(AnimalRace race)
            {
                AnimalRaces.TryRemove(Context.Guild.Id, out _);
                var winner = race.FinishedUsers[0];
                if (race.FinishedUsers[0].Bet > 0)
                {
                    return Context.Channel.SendConfirmAsync(GetText("animal_race"),
                                        GetText("animal_race_won_money", Format.Bold(winner.Username),
                                            winner.Animal.Icon, (race.FinishedUsers[0].Bet * (race.Users.Length - 1)) + _bc.BotConfig.CurrencySign));
                }
                else
                {
                    return Context.Channel.SendConfirmAsync(GetText("animal_race"),
                        GetText("animal_race_won", Format.Bold(winner.Username), winner.Animal.Icon));
                }
            }

            private async Task Ar_OnStateUpdate(AnimalRace race)
            {
                var text = $@"|🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🔚|
{String.Join("\n", race.Users.Select(p =>
                {
                    var index = race.FinishedUsers.IndexOf(p);
                    var extra = (index == -1 ? "" : $"#{index + 1} {(index == 0 ? "🏆" : "")}");
                    return $"{(int)(p.Progress / 60f * 100),-2}%|{new string('‣', p.Progress) + p.Animal.Icon + extra}";
                }))}
|🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🔚|";

                if (raceMessage == null)
                    raceMessage = await Context.Channel.SendConfirmAsync(text)
                        .ConfigureAwait(false);
                else
                    await raceMessage.ModifyAsync(x => x.Embed = new EmbedBuilder()
                        .WithTitle(GetText("animal_race"))
                        .WithDescription(text)
                        .WithOkColor()
                        .Build())
                            .ConfigureAwait(false);
            }

            private Task Ar_OnStartingFailed(AnimalRace race)
            {
                AnimalRaces.TryRemove(Context.Guild.Id, out _);
                return ReplyErrorLocalized("animal_race_failed");
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task JoinRace(int amount = 0)
            {
                if (!AnimalRaces.TryGetValue(Context.Guild.Id, out var ar))
                {
                    await ReplyErrorLocalized("race_not_exist").ConfigureAwait(false);
                    return;
                }
                try
                {
                    var user = await ar.JoinRace(Context.User.Id, Context.User.ToString(), amount)
                        .ConfigureAwait(false);
                    if (amount > 0)
                        await Context.Channel.SendConfirmAsync(GetText("animal_race_join_bet", Context.User.Mention, user.Animal.Icon, amount + _bc.BotConfig.CurrencySign)).ConfigureAwait(false);
                    else
                        await Context.Channel.SendConfirmAsync(GetText("animal_race_join", Context.User.Mention, user.Animal.Icon)).ConfigureAwait(false);
                }
                catch (ArgumentOutOfRangeException)
                {
                    //ignore if user inputed an invalid amount
                }
                catch (AlreadyJoinedException)
                {
                    // just ignore this
                }
                catch (AlreadyStartedException)
                {
                    //ignore
                }
                catch (AnimalRaceFullException)
                {
                    await Context.Channel.SendConfirmAsync(GetText("animal_race"), GetText("animal_race_full"))
                        .ConfigureAwait(false);
                }
                catch (NotEnoughFundsException)
                {
                    await Context.Channel.SendErrorAsync(GetText("not_enough", _bc.BotConfig.CurrencySign)).ConfigureAwait(false);
                }
            }
        }
    }
}