using System;

namespace Mitternacht.Modules.Games.Common.Hangman.Exceptions {
	public class TermNotFoundException : Exception {
		public TermNotFoundException(TermType type) : base($"TermType {type} could not be found.") {
		}
	}
}