using System;

namespace Mitternacht.Modules.Games.Common.Hangman {
	[Flags]
	public enum TermType {
		Countries = 0,
		Movies    = 1,
		Animals   = 2,
		Things    = 4,
		Random    = 8,
	}
}