using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Mitternacht.Services.Impl;
using MitternachtWeb.Areas.Analysis.Services;
using MitternachtWeb.Authorization;
using MitternachtWeb.Models;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace MitternachtWeb {
	public class Startup {
		public Startup(IConfiguration configuration) {
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		public void ConfigureServices(IServiceCollection services) {
			services.AddRouting(o => {
				o.LowercaseUrls         = true;
				o.LowercaseQueryStrings = true;
			});
			services.AddControllersWithViews();
			services.AddHttpContextAccessor();
			services.Add(ServiceDescriptor.Singleton(Program.MitternachtBot));
			services.Add(Program.MitternachtBot.Services.Services.Select(s => ServiceDescriptor.Singleton(s.Key, s.Value)));
			services.AddSingleton(new UnknownKeyRequestsService(Program.MitternachtBot.Services.GetService<StringService>()));

			services.AddAuthentication(options => {
				options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
				options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

				options.DefaultChallengeScheme = "Discord";
			}).AddCookie().AddOAuth("Discord", options => {
				var discordApiUrl = "https://discordapp.com/api";

				options.AuthorizationEndpoint   = $"{discordApiUrl}/oauth2/authorize";
				options.TokenEndpoint           = $"{discordApiUrl}/oauth2/token";
				options.UserInformationEndpoint = $"{discordApiUrl}/users/@me";
				options.CallbackPath            = new PathString("/login/authenticate_discord");
				options.SaveTokens              = true;

				options.Scope.Add("identify");
				options.Scope.Add("guilds");
				
				options.ClientId     = Configuration.GetValue<string>("Discord:ClientId");
				options.ClientSecret = Configuration.GetValue<string>("Discord:ClientSecret");

				options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id", ClaimValueTypes.UInteger64);

				options.Events = new OAuthEvents {
					OnCreatingTicket = async context => {
						var request = new HttpRequestMessage(HttpMethod.Get, options.UserInformationEndpoint);
						request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
						request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

						var response = await context.Backchannel.SendAsync(request);
						response.EnsureSuccessStatusCode();

						var content = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
						context.RunClaimActions(content);
					}
				};
			});

			services.AddSingleton<IAuthorizationHandler, BotLevelPermissionHandler>();
			services.AddAuthorization(options => {
				options.AddPolicy("ReadBotConfig",  p => p.Requirements.Add(new BotLevelPermissionRequirement(BotLevelPermission. ReadBotConfig)));
				options.AddPolicy("WriteBotConfig", p => p.Requirements.Add(new BotLevelPermissionRequirement(BotLevelPermission.WriteBotConfig)));
			});
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
			app.UseForwardedHeaders(new ForwardedHeadersOptions {
				ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
			});

			if(env.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
			} else {
				app.UseExceptionHandler("/Home/Error");
				app.UseHsts();
			}
			app.UseHttpsRedirection();
			if(Configuration.GetValue("ServeStaticFiles", true)) {
				app.UseStaticFiles();
			}

			app.UseRouting();

			app.UseAuthentication();
			app.UseAuthorization();

			app.UseEndpoints(endpoints => {
				endpoints.MapAreaControllerRoute(
					name: "settings",
					areaName: "Settings",
					pattern: "settings/{controller=Settings}/{action=Index}/{id?}");
				endpoints.MapAreaControllerRoute(
					name: "analysis",
					areaName: "Analysis",
					pattern: "analysis/{controller=Analysis}/{action=Index}/{id?}");
				endpoints.MapAreaControllerRoute(
					name: "moderation",
					areaName: "Moderation",
					pattern: "moderation/{guildId}/{controller=Stats}/{action=Index}/{id?}");
				endpoints.MapControllerRoute(
					name: "default",
					pattern: "{controller=Home}/{action=Index}/{id?}");
			});
		}
	}
}
