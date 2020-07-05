using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Discord;

namespace Mitternacht.Modules.Administration.Common {
	public class UserSpamStats : IDisposable {
		public int Count => _timers.Count;
		public string LastMessage { get; set; }

		private readonly ConcurrentQueue<Timer> _timers = new ConcurrentQueue<Timer>();

		public UserSpamStats(IUserMessage msg) {
			LastMessage = msg.Content;

			ApplyNextMessage(msg);
		}

		private readonly object applyLock = new object();

		public void ApplyNextMessage(IUserMessage message) {
			lock(applyLock) {
				if(!message.Content.Equals(LastMessage, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(message.Content) && message.Attachments.Any()) {
					LastMessage = message.Content;
					while(_timers.TryDequeue(out var old)) {
						StopAllTimers();
					}
				}

				var t = new Timer((_) => {
					if(_timers.TryDequeue(out var old))
						old.Dispose();
				}, null, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));

				_timers.Enqueue(t);
			}
		}

		private void StopAllTimers() {
			while(_timers.TryDequeue(out var old)) {
				old.Dispose();
			}
		}

		public void Dispose() {
			StopAllTimers();
		}
	}
}
