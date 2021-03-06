﻿using System;
using System.IO;
using System.Linq;
using Mitternacht.Common.Collections;
using NLog;
using YamlDotNet.Serialization;

namespace Mitternacht.Resources {
	public class CommandStrings {
		private static readonly Logger _logger;
		private static readonly string CmdStringPath = Path.Combine(Path.GetDirectoryName(typeof(CommandStrings).Assembly.Location), "_strings/commandstrings.yml");
		private static ConcurrentHashSet<CommandStringsModel> _commandStrings;

		static CommandStrings() {
			_logger = LogManager.GetCurrentClassLogger();
			LoadCommandStrings();
		}

		public static void LoadCommandStrings() {
			try {
				var yml          = File.ReadAllText(CmdStringPath);
				var deserializer = new Deserializer();
				_commandStrings = deserializer.Deserialize<ConcurrentHashSet<CommandStringsModel>>(yml);
			} catch(Exception e) {
				_logger.Error(e);
			}
		}

		public static CommandStringsModel GetCommandStringModel(string name)
			=> _commandStrings.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? throw new Exception($"CommandStringsModel for command '{name}' not found.");
	}
}