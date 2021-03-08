using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mitternacht.Extensions {
	public static class StringExtensions {
		public static string TrimTo(this string str, int maxLength, bool hideDots = false) {
			if(maxLength < 0)
				throw new ArgumentOutOfRangeException(nameof(maxLength), $"Argument {nameof(maxLength)} must not be negative.");

			return maxLength <= 3
				? string.Join("", Enumerable.Repeat(".", maxLength))
				: str.Length <= maxLength ? str : $"{str[0..(maxLength-1 - (hideDots ? 0 : 3))]}{(hideDots ? "" : "...")}";
		}

		//http://www.dotnetperls.com/levenshtein
		public static int LevenshteinDistance(this string s, string t) {
			var n = s.Length;
			var m = t.Length;
			var d = new int[n + 1, m + 1];

			// Step 1
			if(n == 0) {
				return m;
			}

			if(m == 0) {
				return n;
			}

			// Step 2
			for(var i = 0; i <= n; d[i, 0] = i++) {
			}

			for(var j = 0; j <= m; d[0, j] = j++) {
			}

			// Step 3
			for(var i = 1; i <= n; i++) {
				//Step 4
				for(var j = 1; j <= m; j++) {
					// Step 5
					var cost = t[j - 1] == s[i - 1] ? 0 : 1;

					// Step 6
					d[i, j] = Math.Min(
						Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
						d[i - 1, j - 1] + cost);
				}
			}
			// Step 7
			return d[n, m];
		}

		public static async Task<Stream> ToStream(this string str) {
			var ms = new MemoryStream();
			var sw = new StreamWriter(ms);
			await sw.WriteAsync(str);
			await sw.FlushAsync();
			ms.Position = 0;
			return ms;
		}

		private static readonly Regex FilterRegex = new Regex(@"(?:discord(?:\.gg|.me|\.com\/invite|app\.com\/invite)\/(?<id>([\w]{16}|(?:[\w]+-?){3})))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		public static bool ContainsDiscordInvite(this string str)
			=> FilterRegex.IsMatch(str);

		public static string SanitizeMentions(this string str)
			=> str.Replace("@everyone", "@everyοne").Replace("@here", "@һere");

		public static string GetInitials(this string txt, string glue = "")
			=> string.Join(glue, txt.Split(' ').Select(x => x.FirstOrDefault()));
	}
}
