using System.Runtime.CompilerServices;
using Discord.Commands;
using Mitternacht.Resources;

namespace Mitternacht.Common.Attributes {
	public class Usage : RemarksAttribute {
		public Usage([CallerMemberName] string memberName = "") : base(CommandStrings.GetCommandStringModel(memberName).Usage) {

		}
	}
}
