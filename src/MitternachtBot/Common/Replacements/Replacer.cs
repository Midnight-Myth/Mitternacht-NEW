using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mitternacht.Common.Replacements {
	public class Replacer {
		private readonly IEnumerable<(string Key, Func<string> Text)> _replacements;
		private readonly IEnumerable<(Regex Regex, Func<Match, string> Replacement)> _regex;

		public Replacer(IEnumerable<(string, Func<string>)> replacements, IEnumerable<(Regex, Func<Match, string>)> regex) {
			_replacements = replacements;
			_regex = regex;
		}

		public string Replace(string input) {
			if(string.IsNullOrWhiteSpace(input))
				return input;
			foreach(var (key, text) in _replacements)
				if(input.Contains(key))
					input = input.Replace(key, text());
			return _regex.Aggregate(input, (current, item) => item.Regex.Replace(current, m => item.Replacement(m)));
		}

		public void Replace(CREmbed embedData) {
			embedData.PlainText = Replace(embedData.PlainText);
			embedData.Description = Replace(embedData.Description);
			embedData.Title = Replace(embedData.Title);

			if(embedData.Fields != null)
				foreach(var f in embedData.Fields) {
					f.Name = Replace(f.Name);
					f.Value = Replace(f.Value);
				}

			if(embedData.Footer != null)
				embedData.Footer.Text = Replace(embedData.Footer.Text);
		}
	}
}
