using Mitternacht.Database.Models;
using System.ComponentModel.DataAnnotations;

namespace MitternachtWeb.Areas.Guild.Models {
	public class CreateWatchedForumAccount {
		[Required]
		public long ForumUserId { get; set; }
		[Required]
		public WatchAction WatchAction { get; set; }
	}
}
