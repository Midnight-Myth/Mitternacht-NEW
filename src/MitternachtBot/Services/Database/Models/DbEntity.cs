using System;
using System.ComponentModel.DataAnnotations;

namespace Mitternacht.Services.Database.Models {
	public class DbEntity {
		[Key]
		public int       Id        { get; set; }
		public DateTime? DateAdded { get; set; } = DateTime.UtcNow;
	}
}
