using System.Runtime.CompilerServices;
using Discord.Commands;
using Mitternacht.Resources;

namespace Mitternacht.Common.Attributes {
	public class MitternachtCommand : CommandAttribute {
		public MitternachtCommand([CallerMemberName] string memberName = "") : base(CommandStrings.GetCommandStringModel(memberName).Command) {

		}
	}
}
