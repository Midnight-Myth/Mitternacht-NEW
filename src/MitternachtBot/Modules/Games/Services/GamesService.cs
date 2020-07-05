using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Mitternacht.Modules.Games.Common;
using Mitternacht.Services;

namespace Mitternacht.Modules.Games.Services {
	public class GamesService : IMService {
		private readonly IBotConfigProvider _bc;

		public readonly ConcurrentDictionary<ulong, GirlRating> GirlRatings = new ConcurrentDictionary<ulong, GirlRating>();
		public readonly ImmutableArray<string> EightBallResponses;

		public readonly string TypingArticlesPath = "data/typing_articles2.json";

		public GamesService(IBotConfigProvider bc) {
			_bc = bc;

			EightBallResponses = _bc.BotConfig.EightBallResponses.Select(ebr => ebr.Text).ToImmutableArray();

			var timer = new Timer(_ => {
				GirlRatings.Clear();
			}, null, TimeSpan.FromDays(1), TimeSpan.FromDays(1));

		}
	}
}
