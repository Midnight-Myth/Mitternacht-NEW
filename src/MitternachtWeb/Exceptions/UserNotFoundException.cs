using System;

namespace MitternachtWeb.Exceptions {
	public class UserNotFoundException : Exception {
		public UserNotFoundException(ulong userId) : base(userId.ToString()) { }
	}
}
