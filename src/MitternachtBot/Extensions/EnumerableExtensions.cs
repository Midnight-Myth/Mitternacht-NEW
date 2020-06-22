using System;
using System.Collections.Generic;
using System.Linq;

namespace Mitternacht.Extensions {
	public static class EnumerableExtensions {
		public static IEnumerable<T> Distinct<T, U>(this IEnumerable<T> data, Func<T, U> getKey)
			=> data.GroupBy(getKey).Select(x => x.First());

		public static T MaxOr<T>(this IEnumerable<T> list, T defaultValue)
			=> list.Any() ? list.Max() : defaultValue;
	}
}
