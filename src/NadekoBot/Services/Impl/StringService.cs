using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NLog;

namespace Mitternacht.Services.Impl {
	public class StringService : INService {
		public const string StringsPath = @"locales";
		public const string FilenameRegex = @"(.+)\.json$";

		private readonly ImmutableDictionary<string, ImmutableDictionary<string, string>> _responseStrings;

		// Used as failsafe in case response key doesn't exist in the selected or default language.
		private readonly CultureInfo   _usCultureInfo = new CultureInfo("en-US");
		private readonly ILocalization _localization;
		private readonly Logger        _logger = LogManager.GetCurrentClassLogger();

		public StringService(ILocalization loc) {
			var log = LogManager.GetCurrentClassLogger();
			_localization = loc;

			var sw          = Stopwatch.StartNew();
			var localesDict = new Dictionary<string, ImmutableDictionary<string, string>>(); // lang:(name:value)
			var localeFiles = Directory.GetFiles(StringsPath)
								.Select(filename => (Filename: filename, Match: Regex.Match(Path.GetFileName(filename), FilenameRegex)))
								.Where((fnm) => fnm.Match.Success)
								.Select(fnm => (Filename: fnm.Filename, Locale: fnm.Match.Groups[1].Value))
								.ToArray();
			
			foreach(var (filename, locale) in localeFiles) {
				var langDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(filename));
				localesDict.Add(locale.ToLowerInvariant(), langDict.ToImmutableDictionary());
			}

			_responseStrings = localesDict.ToImmutableDictionary();
			sw.Stop();

			log.Info($"Loaded {_responseStrings.Count} locales in {sw.Elapsed.TotalSeconds:F2}s");
		}

		private string GetString(string text, CultureInfo cultureInfo) {
			if(!_responseStrings.TryGetValue(cultureInfo.Name.ToLowerInvariant(), out var strings)) return null;

			strings.TryGetValue(text, out var val);
			return val;
		}

		public string GetText(string key, ulong? guildId, string lowerModuleTypeName, params object[] replacements)
			=> GetText(key, _localization.GetCultureInfo(guildId), lowerModuleTypeName, replacements);

		public string GetText(string key, CultureInfo cultureInfo, string lowerModuleTypeName) {
			var text = GetString($"{lowerModuleTypeName}_{key}", cultureInfo);

			if(!string.IsNullOrWhiteSpace(text)) return text;
			
			_logger.Warn($"{lowerModuleTypeName}_{key} key is missing from {cultureInfo} response strings. PLEASE REPORT THIS.");
			
			text = GetString($"{lowerModuleTypeName}_{key}", _usCultureInfo) ?? $"Error: Key {lowerModuleTypeName}_{key} not found!";
			return !string.IsNullOrWhiteSpace(text) ? text : $"I can't tell you if the command is executed, because there was an error printing out the response. Key '{lowerModuleTypeName}_{key}' is missing from resources. Please report this.";
		}

		public string GetText(string key, CultureInfo cultureInfo, string lowerModuleTypeName, params object[] replacements) {
			try {
				return string.Format(GetText(key, cultureInfo, lowerModuleTypeName), replacements);
			} catch(FormatException) {
				return "I can't tell you if the command is executed, because there was an error printing out the response. Key '" + lowerModuleTypeName + "_" + key + "' " + "is not properly formatted. Please report this.";
			}
		}
	}
}