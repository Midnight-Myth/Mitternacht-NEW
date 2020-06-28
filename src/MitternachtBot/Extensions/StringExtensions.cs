using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mitternacht.Extensions {
	public static class StringExtensions {
		/// <summary>
		/// Easy use of fast, efficient case-insensitive Contains check with StringComparison Member Types 
		/// CurrentCulture, CurrentCultureIgnoreCase, InvariantCulture, InvariantCultureIgnoreCase, Ordinal, OrdinalIgnoreCase
		/// </summary>    
		public static bool ContainsNoCase(this string str, string contains, StringComparison compare) {
			return str.IndexOf(contains, compare) >= 0;
		}

		public static string TrimTo(this string str, int maxLength, bool hideDots = false) {
			if(maxLength < 0)
				throw new ArgumentOutOfRangeException(nameof(maxLength), $"Argument {nameof(maxLength)} can't be negative.");
			return maxLength == 0
				? string.Empty
				: maxLength <= 3
					? string.Concat(str.Select(c => '.'))
					: str.Length < maxLength ? str : string.Concat(str.Take(maxLength - 3)) + (hideDots ? "" : "...");
		}

		public static string ToTitleCase(this string str)
			=> string.Join(" ", str.Split(" ", StringSplitOptions.RemoveEmptyEntries).Select(s => s.Substring(0, 1).ToUpper() + s.Substring(1)));

		/// <summary>
		/// Removes trailing S or ES (if specified) on the given string if the num is 1
		/// </summary>
		/// <param name="str"></param>
		/// <param name="num"></param>
		/// <param name="es"></param>
		/// <returns>String with the correct singular/plural form</returns>
		public static string SnPl(this string str, int? num, bool es = false) {
			if(str == null)
				throw new ArgumentNullException(nameof(str));
			if(num == null)
				throw new ArgumentNullException(nameof(num));
			return num == 1 ? str.Remove(str.Length - 1, es ? 2 : 1) : str;
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

		public static string Unmention(this string str)
			=> str.Replace("@", "ම");

		public static string SanitizeMentions(this string str)
			=> str.Replace("@everyone", "@everyοne").Replace("@here", "@һere");

		public static string ToBase64(this string plainText)
			=> Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));

		public static string GetInitials(this string txt, string glue = "")
			=> string.Join(glue, txt.Split(' ').Select(x => x.FirstOrDefault()));
	}
}
