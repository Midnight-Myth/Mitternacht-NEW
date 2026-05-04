using System.ComponentModel.DataAnnotations;

namespace MitternachtWeb.Areas.Guild.Models {
	public class CreateVerification {
		[Required]
		public ulong? UserId { get; set; }
		[Required]
		public long? ForumUserId { get; set; }
	}
}
