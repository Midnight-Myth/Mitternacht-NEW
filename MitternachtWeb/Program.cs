using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Mitternacht;
using System;
using System.IO;
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
				config.SetBasePath(Environment.CurrentDirectory);
				config.AddJsonFile("mitternachtweb.config");
			}).ConfigureWebHostDefaults(webBuilder => {
				webBuilder.UseStartup<Startup>();
				webBuilder.UseContentRoot(Path.GetDirectoryName(typeof(Program).Assembly.Location));
			});
	}
}
