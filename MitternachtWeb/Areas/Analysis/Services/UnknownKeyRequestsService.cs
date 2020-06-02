using Mitternacht.Services.Impl;
using System.Collections.Concurrent;
using System.Globalization;

namespace MitternachtWeb.Areas.Analysis.Services {
	public class UnknownKeyRequestsService {
		public ConcurrentDictionary<(string moduleName, string key, string cultureName), ulong> UnknownKeyRequests = new ConcurrentDictionary<(string moduleName, string key, string cultureName), ulong>();

		public UnknownKeyRequestsService(StringService ss) {
			ss.UnknownKeyRequested += UnknownKeyRequested;
		}

		private void UnknownKeyRequested(string moduleName, string key, CultureInfo cultureInfo) {
			UnknownKeyRequests.AddOrUpdate((moduleName, key, cultureInfo.Name), 1, (_, occurences) => occurences + 1);
		}
	}
}
