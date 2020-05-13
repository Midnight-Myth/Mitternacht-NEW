using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Mitternacht.Extensions;
using Mitternacht.Services.Impl;

namespace Mitternacht.Modules.Games.Common
{
    public class Poll
    {
        private readonly IUserMessage _originalMessage;
        private readonly IGuild _guild;
        private readonly string[] _answers;
        private readonly ConcurrentDictionary<ulong, int> _participants = new ConcurrentDictionary<ulong, int>();
        private readonly string _question;
        private readonly StringService _strings;
        private bool _running;

        public event Action<ulong> OnEnded = delegate { };

        public Poll(StringService strings, IUserMessage umsg, string question, IEnumerable<string> enumerable)
        {
            _strings = strings;

            _originalMessage = umsg;
            _guild = ((ITextChannel)umsg.Channel).Guild;
            _question = question;
            _answers = enumerable as string[] ?? enumerable.ToArray();
        }

        public EmbedBuilder GetStats(string title)
        {
            var results = _participants.GroupBy(kvp => kvp.Value)
                                .ToDictionary(x => x.Key, x => x.Sum(kvp => 1))
                                .OrderByDescending(kvp => kvp.Value)
                                .ToArray();

            var eb = new EmbedBuilder().WithTitle(title);

            var sb = new StringBuilder()
                .AppendLine(Format.Bold(_question))
                .AppendLine();

            var totalVotesCast = 0;
            if (results.Length == 0)
            {
                sb.AppendLine(GetText("no_votes_cast"));
            }
            else {
                foreach (var result in results) {
                    sb.AppendLine(GetText("poll_result",
                        result.Key,
                        Format.Bold(_answers[result.Key - 1]),
                        Format.Bold(result.Value.ToString())));
                    totalVotesCast += result.Value;
                }
            }


            eb.WithDescription(sb.ToString())
              .WithFooter(efb => efb.WithText(GetText("x_votes_cast", totalVotesCast)));

            return eb;
        }

        public async Task StartPoll()
        {
            var msgToSend = GetText("poll_created", Format.Bold(_originalMessage.Author.Username)) + "\n\n" + Format.Bold(_question) + "\n";
            var num = 1;
            msgToSend = _answers.Aggregate(msgToSend, (current, answ) => current + $"`{num++}.` **{answ}**\n");
            msgToSend += "\n" + Format.Bold(GetText("poll_vote_public"));

            await _originalMessage.Channel.SendConfirmAsync(msgToSend).ConfigureAwait(false);
            _running = true;
        }

        public async Task StopPoll()
        {
            _running = false;
            OnEnded(_guild.Id);
            await _originalMessage.Channel.EmbedAsync(GetStats("POLL CLOSED")).ConfigureAwait(false);
        }

        public async Task<bool> TryVote(IUserMessage msg)
        {
            // has to be a user message
            if (msg == null || msg.Author.IsBot || !_running)
                return false;

            // has to be an integer
            if (!int.TryParse(msg.Content, out int vote))
                return false;
            if (vote < 1 || vote > _answers.Length)
                return false;

            //if public, channel must be the same the poll started in
            if (_originalMessage.Channel.Id != msg.Channel.Id)
                return false;
            var ch = msg.Channel;

            //user can vote only once
            if (!_participants.TryAdd(msg.Author.Id, vote)) return false;
            var toDelete = await ch.SendConfirmAsync(GetText("poll_voted", Format.Bold(msg.Author.ToString()))).ConfigureAwait(false);
            toDelete.DeleteAfter(5);
            try { await msg.DeleteAsync().ConfigureAwait(false); } catch { /*ignored*/ }
            return true;
        }

        private string GetText(string key, params object[] replacements)
            => _strings.GetText("Games".ToLowerInvariant(),
				key,
				_guild.Id,
				replacements);
    }
}