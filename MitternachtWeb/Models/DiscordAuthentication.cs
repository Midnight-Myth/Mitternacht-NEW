using System;

namespace MitternachtWeb.Models {
	public class DiscordAuthentication : DbEntity {
		public string AccessToken  { get; set; }
		public string RefreshToken { get; set; }
		public DateTime ExpiryTime { get; set; }
	}
}
