using System;
using System.IO;
using System.Linq;
using Mitternacht.Common.Collections;
using Newtonsoft.Json;
using NLog;

namespace Mitternacht.Resources
{
    public class CommandStrings
    {
        private static readonly Logger Log;
        private static readonly string CmdStringPath = @"data/commandstrings.json";
        private static ConcurrentHashSet<CommandStringsModel> _commandStrings;

        static CommandStrings() {
            Log = LogManager.GetCurrentClassLogger();
            LoadCommandStrings();
        }

        public static bool LoadCommandStrings() {
            try {
                var json = File.ReadAllText(CmdStringPath);
                _commandStrings = JsonConvert.DeserializeObject<ConcurrentHashSet<CommandStringsModel>>(json);
            }
            catch (Exception e) {
                Log.Error(e);
                return false;
            }
            return true;
        }

        public static CommandStringsModel GetCommandStringModel(string name) 
            => _commandStrings.FirstOrDefault(c => c.Name == name) ?? new CommandStringsModel { Name = name, Command = name + "_cmd" };
    }
}