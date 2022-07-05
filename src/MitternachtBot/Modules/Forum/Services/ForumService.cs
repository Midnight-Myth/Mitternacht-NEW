using System;
using System.Threading.Tasks;
using Mitternacht.Services;
using NLog;

namespace Mitternacht.Modules.Forum.Services {
	public class ForumService : IMService {
		private readonly IBotCredentials _creds;
		private readonly Logger _log;

		public GommeHDnetForumAPI.Forum Forum { get; private set; }
		public bool HasForumInstance => Forum != null;
		public bool LoggedIn => Forum?.LoggedIn ?? false;
		private Task _loginTask;

		public ForumService(IBotCredentials creds) {
			_creds = creds;
			_log = LogManager.GetCurrentClassLogger();
			InitForumInstance();
		}

		public void InitForumInstance() {
			_loginTask?.Dispose();
			_loginTask = Task.Run(() => {
				try {
					Forum = new GommeHDnetForumAPI.Forum(_creds.ForumUsername, _creds.ForumPassword);

					_log.Info($"Initialized new Forum instance.");
				} catch(Exception e) {
					_log.Warn(e, $"Initializing new Forum instance failed: {e}");
				}
			});
		}
	}
}