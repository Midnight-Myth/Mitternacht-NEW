using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Mitternacht;
using System.Threading.Tasks;

namespace MitternachtWeb {
	public class Program {
		public static async Task Main(string[] args) {
			await new MitternachtBot(0, 0).RunAsync(args);
			
			await CreateHostBuilder(args).Build().RunAsync();
		}

		public static IHostBuilder CreateHostBuilder(string[] args)
			=> Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
	}
}
