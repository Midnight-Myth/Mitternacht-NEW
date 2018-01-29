﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Common.Attributes;
using Mitternacht.Modules.Games.Common.Nunchi;

namespace Mitternacht.Modules.Games
{
    public partial class Games
    {
        public class NunchiCommands : MitternachtSubmodule
        {
            public static readonly ConcurrentDictionary<ulong, Nunchi> Games = new ConcurrentDictionary<ulong, Common.Nunchi.Nunchi>();
            private readonly DiscordSocketClient _client;

            public NunchiCommands(DiscordSocketClient client)
            {
                _client = client;
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Nunchi()
            {
                var newNunchi = new Nunchi(Context.User.Id, Context.User.ToString());
                Nunchi nunchi;

                //if a game was already active
                if ((nunchi = Games.GetOrAdd(Context.Guild.Id, newNunchi)) != newNunchi)
                {
                    // join it
                    if (!await nunchi.Join(Context.User.Id, Context.User.ToString()))
                    {
                        // if you failed joining, that means game is running or just ended
                        // await ReplyErrorLocalized("nunchi_already_started").ConfigureAwait(false);
                        return;
                    }

                    await ReplyConfirmLocalized("nunchi_joined", nunchi.ParticipantCount).ConfigureAwait(false);
                    return;
                }


                try { await ConfirmLocalized("nunchi_created").ConfigureAwait(false); } catch { }

                nunchi.OnGameEnded += Nunchi_OnGameEnded;
                //nunchi.OnGameStarted += Nunchi_OnGameStarted;
                nunchi.OnRoundEnded += Nunchi_OnRoundEnded;
                nunchi.OnUserGuessed += Nunchi_OnUserGuessed;
                nunchi.OnRoundStarted += Nunchi_OnRoundStarted;
                _client.MessageReceived += _client_MessageReceived;

                var success = await nunchi.Initialize().ConfigureAwait(false);
                if (!success)
                {
                    if (Games.TryRemove(Context.Guild.Id, out var game))
                        game.Dispose();
                    await ConfirmLocalized("nunchi_failed_to_start").ConfigureAwait(false);
                }

                Task _client_MessageReceived(SocketMessage arg)
                {
                    var _ = Task.Run(async () =>
                    {
                        if (arg.Channel.Id != Context.Channel.Id)
                            return;

                        if (!int.TryParse(arg.Content, out var number))
                            return;
                        try
                        {
                            await nunchi.Input(arg.Author.Id, arg.Author.ToString(), number).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    });
                    return Task.CompletedTask;
                }

                Task Nunchi_OnGameEnded(Nunchi arg1, string arg2)
                {
                    if (Games.TryRemove(Context.Guild.Id, out var game))
                    {
                        _client.MessageReceived -= _client_MessageReceived;
                        game.Dispose();
                    }

                    if (arg2 == null)
                        return ConfirmLocalized("nunchi_ended_no_winner", Format.Bold(arg2));
                    else
                        return ConfirmLocalized("nunchi_ended", Format.Bold(arg2));
                }
            }

            private Task Nunchi_OnRoundStarted(Nunchi arg, int cur)
            {
                return ConfirmLocalized("nunchi_round_started",
                    Format.Bold(arg.ParticipantCount.ToString()),
                    Format.Bold(cur.ToString()));
            }

            private Task Nunchi_OnUserGuessed(Nunchi arg)
            {
                return ConfirmLocalized("nunchi_next_number", Format.Bold(arg.CurrentNumber.ToString()));
            }

            private Task Nunchi_OnRoundEnded(Nunchi arg1, (ulong Id, string Name)? arg2)
            {
                if (arg2.HasValue)
                    return ConfirmLocalized("nunchi_round_ended", Format.Bold(arg2.Value.Name));
                else
                    return ConfirmLocalized("nunchi_round_ended_boot",
                        Format.Bold("\n" + string.Join("\n, ", arg1.Participants.Select(x => x.Name)))); // this won't work if there are too many users
            }

            private Task Nunchi_OnGameStarted(Nunchi arg)
            {
                return ConfirmLocalized("nunchi_started", Format.Bold(arg.ParticipantCount.ToString()));
            }
        }
    }
}