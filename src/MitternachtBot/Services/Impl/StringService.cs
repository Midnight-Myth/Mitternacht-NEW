using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using YamlDotNet.Serialization;

namespace Mitternacht.Services.Impl {
	public class StringService : IMService {
		public const string StringsPath = @"locales";
		public const string FilenameRegex = @"(.+)\.yml$";

		private readonly ImmutableDictionary<string, ImmutableDictionary<string, ImmutableDictionary<string, string>>> _responseStrings;

		// Used as failsafe in case response key doesn't exist in the selected or default language.
		private readonly CultureInfo   _fallbackCultureInfo = new CultureInfo("de-DE");
		private readonly ILocalization _localization;
		private readonly Logger        _logger = LogManager.GetCurrentClassLogger();

		public event Action<string, string, CultureInfo> UnknownKeyRequested = delegate{ };

		public StringService(ILocalization loc) {
			var log = LogManager.GetCurrentClassLogger();
			_localization = loc;

			var sw          = Stopwatch.StartNew();
			var localesDict = new Dictionary<string, ImmutableDictionary<string, ImmutableDictionary<string, string>>>();
			var localeFiles = Directory.GetFiles(StringsPath)
								.Select(filename => (Filename: filename, Match: Regex.Match(Path.GetFileName(filename), FilenameRegex)))
								.Where((fnm) => fnm.Match.Success)
								.Select(fnm => (Filename: fnm.Filename, Locale: fnm.Match.Groups[1].Value))
								.ToArray();
			
			var deserializer = new Deserializer();

			foreach(var (filename, locale) in localeFiles) {
				var langDict = deserializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(filename));
				localesDict.Add(locale.ToLowerInvariant(), langDict.ToImmutableDictionary(k => k.Key, v => v.Value.ToImmutableDictionary()));
			}

			_responseStrings = localesDict.ToImmutableDictionary();
			sw.Stop();

			log.Info($"Loaded {_responseStrings.Count} locales in {sw.Elapsed.TotalSeconds:F2}s");
		}

		private string GetString(string moduleName, string key, CultureInfo cultureInfo)
			=> _responseStrings.TryGetValue(cultureInfo.Name.ToLowerInvariant(), out var moduleStrings) && moduleStrings.TryGetValue(moduleName.ToLowerInvariant(), out var strings) && strings.TryGetValue(key, out var val) ? val : null;

		public string GetText(string moduleTypeName, string key, ulong? guildId, params object[] replacements)
			=> GetText(moduleTypeName, key, _localization.GetCultureInfo(guildId), replacements);

		public string GetText(string moduleTypeName, string key, CultureInfo cultureInfo) {
			var text = GetString(moduleTypeName, key, cultureInfo);

			if(!string.IsNullOrWhiteSpace(text)) return text;
			
			_logger.Warn($"Key {moduleTypeName}.{key} is missing from {cultureInfo} response strings. PLEASE REPORT THIS.");
			UnknownKeyRequested(moduleTypeName, key, cultureInfo);

			text = GetString(moduleTypeName, key, _fallbackCultureInfo) ?? $"Error: Key {moduleTypeName}.{key} not found!";
			return !string.IsNullOrWhiteSpace(text) ? text : $"I can't tell you if the command is executed, because there was an error printing out the response. Key '{moduleTypeName}.{key}' is missing from resources. Please report this.";
		}

		public string GetText(string moduleTypeName, string key, CultureInfo cultureInfo, params object[] replacements) {
			try {
				return string.Format(GetText(moduleTypeName, key, cultureInfo), replacements);
			} catch(FormatException) {
				return $"I can't tell you if the command is executed, because there was an error printing out the response. Key '{moduleTypeName}.{key}' is not properly formatted. Please report this.";
			}
		}
	}
}