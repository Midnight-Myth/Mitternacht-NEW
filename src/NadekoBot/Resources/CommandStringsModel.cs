using Newtonsoft.Json;

namespace Mitternacht.Resources
{
    public class CommandStringsModel
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("cmd")]
        public string Command { get; set; } = "";

        [JsonProperty("aliases")]
        public string[] Aliases { get; set; } = { };

        [JsonProperty("desc")]
        public string Description { get; set; } = "";

        [JsonProperty("usage")]
        public string Usage { get; set; } = "";
    }
}