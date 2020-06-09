using System.Runtime.CompilerServices;
using Discord.Commands;
using Mitternacht.Resources;

namespace Mitternacht.Common.Attributes {
	public class Aliases : AliasAttribute {
		public Aliases([CallerMemberName] string memberName = "") : base(CommandStrings.GetCommandStringModel(memberName.ToLowerInvariant()).Aliases) {

		}
	}
}
