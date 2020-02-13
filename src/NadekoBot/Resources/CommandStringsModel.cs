using YamlDotNet.Serialization;

namespace Mitternacht.Resources {
	public class CommandStringsModel {
		[YamlMember(Alias = "name")]
		public string Name { get; set; } = "";

		[YamlMember(Alias = "cmd")]
		public string Command { get; set; } = "";

		[YamlMember(Alias = "aliases")]
		public string[] Aliases { get; set; } = { };

		[YamlMember(Alias = "desc")]
		public string Description { get; set; } = "";

		[YamlMember(Alias = "usage")]
		public string Usage { get; set; } = "";
	}
}