using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Mitternacht.Common;
using Mitternacht.Modules.Games.Common.Hangman.Exceptions;
using Newtonsoft.Json;

namespace Mitternacht.Modules.Games.Common.Hangman {
	public class TermPool {
		const string termsPath = "data/word_images.json";
		public static IReadOnlyDictionary<string, HangmanObject[]> Data { get; } = new Dictionary<string, HangmanObject[]>();
		static TermPool() {
			try {
				Data = JsonConvert.DeserializeObject<Dictionary<string, HangmanObject[]>>(File.ReadAllText(termsPath));
			} catch { }
		}

		private static readonly ImmutableArray<TermType> _termTypes = Enum.GetValues(typeof(TermType)).Cast<TermType>().ToImmutableArray();

		public static HangmanObject GetTerm(TermType type) {
			var rng = new NadekoRandom();

			if(type == TermType.Random) {
				type = _termTypes[rng.Next(0, _termTypes.Length - 1)];
			}

			if(Data.TryGetValue(type.ToString(), out var termTypes) && termTypes.Length != 0) {
				var obj = termTypes[rng.Next(0, termTypes.Length)];

				obj.Word = obj.Word.Trim().ToLowerInvariant();
				return obj;
			} else {
				throw new TermNotFoundException(type);
			}
		}
	}
}