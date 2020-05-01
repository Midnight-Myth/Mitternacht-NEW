using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Mitternacht;
using System.Threading.Tasks;

namespace MitternachtWeb {
	public class Program {
		public static MitternachtBot MitternachtBot;

		public static async Task Main(string[] args) {
			MitternachtBot = new MitternachtBot(0, 0);
			await MitternachtBot.RunAsync(args);
			
			await CreateHostBuilder(args).Build().RunAsync();
		}

		public static IHostBuilder CreateHostBuilder(string[] args)
			=> Host.CreateDefaultBuilder(args).ConfigureAppConfiguration((context, config) => {
				config.AddJsonFile("mitternachtweb.config");
			}).ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
	}
}
