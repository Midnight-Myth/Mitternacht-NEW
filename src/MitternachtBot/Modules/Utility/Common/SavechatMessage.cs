using System.Collections.Generic;
using Newtonsoft.Json;

namespace Mitternacht.Modules.Utility.Common
{
    public class SavechatMessage
    {
        [JsonProperty("author")]
        public string Author { get; set; }
        [JsonProperty("time")]
        public string Time { get; set; }
        [JsonProperty("msg")]
        public string Message { get; set; }
        [JsonProperty("attachments")]
        public List<string> Attachments { get; set; }
        [JsonProperty("embeds")]
        public List<string> Embeds { get; set; }

        public SavechatMessage(string author, string time, string msg, List<string> attachments, List<string> embeds) {
            Author = author;
            Time = time;
            Message = msg;
            Attachments = attachments;
            Embeds = embeds;
        }
    }
}