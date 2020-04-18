namespace Mitternacht.Common {
	public class DbConfig {
		public string Type { get; }
		public string ConnectionString { get; }

		public DbConfig(string type, string connectionString) {
			Type             = type;
			ConnectionString = connectionString;
		}
	}
}
