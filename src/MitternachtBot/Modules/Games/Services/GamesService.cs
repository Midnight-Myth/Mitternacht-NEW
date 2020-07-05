using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Mitternacht.Modules.Games.Common;
using Mitternacht.Services;

namespace Mitternacht.Modules.Games.Services {
	public class GamesService : IMService {
		private readonly IBotConfigProvider _bcp;

		public readonly ConcurrentDictionary<ulong, GirlRating> GirlRatings = new ConcurrentDictionary<ulong, GirlRating>();
		
		public string[] EightBallResponses => _bcp.BotConfig.EightBallResponses.Select(ebr => ebr.Text).ToArray();

		public readonly string TypingArticlesPath = "data/typing_articles2.json";

		public GamesService(IBotConfigProvider bcp) {
			_bcp = bcp;

			var timer = new Timer(_ => {
				GirlRatings.Clear();
			}, null, TimeSpan.FromDays(1), TimeSpan.FromDays(1));

		}
	}
}
