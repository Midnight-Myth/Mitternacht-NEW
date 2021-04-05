using System.ComponentModel.DataAnnotations;

namespace MitternachtWeb.Areas.Guild.Models {
	public class EditQuote {
		[Required]
		public string Keyword { get; set; }
		[Required]
		public string Text { get; set; }
	}
}
