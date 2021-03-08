using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MinecraftQuery;
using Mitternacht.Common;
using Mitternacht.Common.ShardCom;
using Mitternacht.Common.TypeReaders;
using Mitternacht.Common.TypeReaders.Models;
using Mitternacht.Modules.Birthday.Models;
using Mitternacht.Services;
using Mitternacht.Database.Models;
using Mitternacht.Services.Impl;
using NLog;

namespace Mitternacht {
	public class MitternachtBot {
		private readonly Logger _log;

		public BotCredentials Credentials { get; }

		public DiscordSocketClient Client         { get; }
		public CommandService      CommandService { get; }

		private readonly DbService _db;

		public static Color OkColor    { get; private set; }
		public static Color ErrorColor { get; private set; }

		public TaskCompletionSource<bool> Ready { get; } = new TaskCompletionSource<bool>();

		public INServiceProvider Services { get; private set; }

		public ShardsCoordinator ShardCoord { get; private set; }

		private readonly ShardComClient _comClient;

		public MitternachtBot(int shardId, int parentProcessId, int? port = null) {
			if(shardId < 0)
				throw new ArgumentOutOfRangeException(nameof(shardId));

			LogSetup.SetupLogger();
			_log = LogManager.GetCurrentClassLogger();
			TerribleElevatedPermissionCheck();

			Credentials = BotCredentials.Load();
			_db         = new DbService(Credentials);
			Client = new DiscordSocketClient(new DiscordSocketConfig {
				MessageCacheSize    = 10,
				LogLevel            = LogSeverity.Warning,
				ConnectionTimeout   = int.MaxValue,
				TotalShards         = Credentials.TotalShards,
				ShardId             = shardId,
				AlwaysDownloadUsers = false
			});
			CommandService = new CommandService(new CommandServiceConfig {CaseSensitiveCommands = false, DefaultRunMode = RunMode.Sync});

			port       ??=Credentials.ShardRunPort;
			_comClient =  new ShardComClient(port.Value);

			using var uow = _db.UnitOfWork;
			uow.Context.EnsureCorrectDatabaseState();
			OnBotConfigChanged(uow.BotConfig.GetOrCreate());

			SetupShard(parentProcessId, port.Value);

			Client.Log += Client_Log;
		}

		private void StartSendingData() {
			Task.Run(async () => {
				while(true) {
					await _comClient.Send(new ShardComMessage {ConnectionState = Client.ConnectionState, Guilds = Client.ConnectionState == ConnectionState.Connected ? Client.Guilds.Count : 0, ShardId = Client.ShardId, Time = DateTime.UtcNow});
					await Task.Delay(5000);
				}
			});
		}

		private void StayConnected() {
			Task.Run(async () => {
				var counter = 0;
				while(true) {
					if(Client.ConnectionState == ConnectionState.Disconnected || Client.ConnectionState == ConnectionState.Disconnecting) {
						counter++;
						//shutdown Bot after unsuccessfully trying to reconnect 3 times.
						if(counter > 3) Environment.Exit(0);

						_log.Warn($"Shard {Client.ShardId} is not connected, trying to reconnect!");
						try {
							await Task.WhenAny(Task.Delay(10000), new Task(async () => {
								await Client.StopAsync();
								await Task.Delay(1000);
								await Client.StartAsync();
							}));
						} catch {
							_log.Warn($"Shard {Client.ShardId} failed to reconnect, trying again in 10s.");
						}
					}

					await Task.Delay(10000);
				}
			});
		}

		private void AddServices() {
			// This UnitOfWork will be used for building Modules in Discord.Commands.CommandService. Do not use it in anything else.
			using var uow = _db.UnitOfWork;

			IBotConfigProvider botConfigProvider = new BotConfigProvider(_db);
			botConfigProvider.BotConfigChanged  += OnBotConfigChanged;

			Services = new NServiceProvider.ServiceProviderBuilder()
						.AddManual<IBotCredentials>(Credentials)
						.AddManual(_db)
						.AddManual(Client)
						.AddManual(CommandService)
						.AddManual(botConfigProvider)
						.AddManual(this)
						.AddManual(uow)
						.AddManual(new MojangApi())
						.LoadFrom(typeof(MitternachtBot).Assembly)
						.Build();

			var commandHandler = Services.GetService<CommandHandler>();
			commandHandler.AddServices(Services);

			//setup typereaders
			CommandService.AddTypeReader<PermissionAction>(new PermissionActionTypeReader());
			CommandService.AddTypeReader<CommandInfo>(new CommandTypeReader());
			CommandService.AddTypeReader<CommandOrCrInfo>(new CommandOrCrTypeReader());
			CommandService.AddTypeReader<ModuleInfo>(new ModuleTypeReader(CommandService));
			CommandService.AddTypeReader<ModuleOrCrInfo>(new ModuleOrCrTypeReader(CommandService));
			CommandService.AddTypeReader<IGuild>(new GuildTypeReader(Client));
			CommandService.AddTypeReader<GuildDateTime>(new GuildDateTimeTypeReader());
			CommandService.AddTypeReader<IBirthDate>(new BirthDateTypeReader());
			CommandService.AddTypeReader<HexColor>(new HexColorTypeReader());
			CommandService.AddTypeReader<ModerationPoints>(new ModerationPointsTypeReader());
		}

		private async Task LoginAsync(string token) {
			var clientReady = new TaskCompletionSource<bool>();

			Task SetClientReady() {
				var _ = Task.Run(async () => {
					clientReady.TrySetResult(true);
					try {
						foreach(var chan in await Client.GetDMChannelsAsync()) {
							await chan.CloseAsync().ConfigureAwait(false);
						}
					} catch { }
				});
				return Task.CompletedTask;
			}

			_log.Info("Shard {0} logging in ...", Client.ShardId);
			await Client.LoginAsync(TokenType.Bot, token).ConfigureAwait(false);
			await Client.StartAsync().ConfigureAwait(false);
			Client.Ready += SetClientReady;
			await clientReady.Task.ConfigureAwait(false);
			Client.Ready       -= SetClientReady;
			Client.JoinedGuild += Client_JoinedGuild;
			Client.LeftGuild   += Client_LeftGuild;
			_log.Info("Shard {0} logged in.", Client.ShardId);
		}

		private Task Client_LeftGuild(SocketGuild arg) {
			_log.Info("Left server: {0} [{1}]", arg?.Name, arg?.Id);
			return Task.CompletedTask;
		}

		private Task Client_JoinedGuild(SocketGuild arg) {
			_log.Info("Joined server: {0} [{1}]", arg?.Name, arg?.Id);
			return Task.CompletedTask;
		}

		public async Task RunAsync(params string[] args) {
			if(Client.ShardId == 0)
				_log.Info($"Starting MitternachtBot v{StatsService.BotVersion} (based on NadekoBot v1.7)");

			var sw = Stopwatch.StartNew();

			await LoginAsync(Credentials.Token).ConfigureAwait(false);

			_log.Info($"Shard {Client.ShardId} loading services...");
			AddServices();

			sw.Stop();
			_log.Info($"Shard {Client.ShardId} connected in {sw.Elapsed.TotalSeconds:F2}s");

			var stats = Services.GetService<IStatsService>();
			stats.Initialize();
			var commandHandler = Services.GetService<CommandHandler>();
			var commandService = Services.GetService<CommandService>();

			commandHandler.StartHandling();

			var _ = await commandService.AddModulesAsync(GetType().GetTypeInfo().Assembly, Services);

			Ready.TrySetResult(true);
			_log.Info($"Shard {Client.ShardId} ready.");

			StartSendingData();
			StayConnected();
		}

		private Task Client_Log(LogMessage arg) {
			_log.Warn($"{arg.Source} | {arg.Message}");
			if(arg.Exception != null) _log.Warn(arg.Exception);
			return Task.CompletedTask;
		}

		public async Task RunAndBlockAsync(params string[] args) {
			await RunAsync(args).ConfigureAwait(false);

			if(ShardCoord != null)
				await ShardCoord.RunAndBlockAsync();
			else {
				await Task.Delay(-1).ConfigureAwait(false);
			}
		}

		private void TerribleElevatedPermissionCheck() {
			try {
				File.WriteAllText("test", "test");
				File.Delete("test");
			} catch {
				_log.Error("Not enough filesystem permissions!");
				Console.ReadKey();
				Environment.Exit(2);
			}
		}

		private void SetupShard(int parentProcessId, int port) {
			if(Client.ShardId == 0) {
				ShardCoord = new ShardsCoordinator(port);
				return;
			}

			new Thread(() => {
				try {
					Process.GetProcessById(parentProcessId).WaitForExit();
				} finally {
					Environment.Exit(10);
				}
			}).Start();
		}

		private void OnBotConfigChanged(BotConfig bc) {
			OkColor    = new Color(Convert.ToUInt32(bc.OkColor, 16));
			ErrorColor = new Color(Convert.ToUInt32(bc.ErrorColor, 16));
		}
	}
}