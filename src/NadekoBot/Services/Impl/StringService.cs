using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using NLog;

namespace Mitternacht.Services.Impl {
	public class StringService : INService {
		public const string StringsPath = @"_strings/responsestrings";

		private readonly ImmutableDictionary<string, ImmutableDictionary<string, string>> _responseStrings;

		// Used as failsafe in case response key doesn't exist in the selected or default language.
		private readonly CultureInfo   _usCultureInfo = new CultureInfo("en-US");
		private readonly ILocalization _localization;
		private readonly Logger        _logger = LogManager.GetCurrentClassLogger();

		public StringService(ILocalization loc) {
			var log = LogManager.GetCurrentClassLogger();
			_localization = loc;

			var sw           = Stopwatch.StartNew();
			var allLangsDict = new Dictionary<string, ImmutableDictionary<string, string>>(); // lang:(name:value)
			foreach(var file in Directory.GetFiles(StringsPath)) {
				var langDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(file));

				allLangsDict.Add(GetLocaleName(file).ToLowerInvariant(), langDict.ToImmutableDictionary());
			}

			_responseStrings = allLangsDict.ToImmutableDictionary();
			sw.Stop();

			log.Info($"Loaded {_responseStrings.Count} languages in {sw.Elapsed.TotalSeconds:F2}s");
		}

		private string GetLocaleName(string fileName) {
			var dotIndex       = fileName.IndexOf('.') + 1;
			var secondDotIndex = fileName.LastIndexOf('.');
			return fileName.Substring(dotIndex, secondDotIndex - dotIndex);
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