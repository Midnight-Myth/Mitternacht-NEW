using System.Runtime.CompilerServices;
using Discord.Commands;
using Mitternacht.Resources;

namespace Mitternacht.Common.Attributes {
	public class Description : SummaryAttribute {
		public Description([CallerMemberName] string memberName = "") : base(CommandStrings.GetCommandStringModel(memberName.ToLowerInvariant()).Description) {

		}
	}
}
