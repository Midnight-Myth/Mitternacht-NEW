using System.Collections.Generic;
using System.Linq;

namespace Mitternacht.Extensions {
	public static class EnumerableExtensions {
		public static T MaxOr<T>(this IEnumerable<T> list, T defaultValue)
			=> list.Any() ? list.Max() : defaultValue;
	}
}
