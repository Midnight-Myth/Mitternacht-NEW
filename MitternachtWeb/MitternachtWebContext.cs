using Microsoft.EntityFrameworkCore;

namespace MitternachtWeb {
	public partial class MitternachtWebContext : DbContext {
		public MitternachtWebContext() { }

		public MitternachtWebContext(DbContextOptions<MitternachtWebContext> options) : base(options) {
			Database.Migrate();
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
			if(!optionsBuilder.IsConfigured) {
				optionsBuilder.UseSqlite("Filename=MitternachtWeb.db");
			}
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder) {
			OnModelCreatingPartial(modelBuilder);
		}

		partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
	}
}
