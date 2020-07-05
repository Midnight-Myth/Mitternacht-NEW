using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;
using MitternachtWeb.Models;

namespace MitternachtWeb.Controllers {
	[Authorize("ReadBotConfig")]
	public class BotConfigController : DiscordUserController {
		private readonly DbService          _db;
		private readonly IBotCredentials    _creds;
		private readonly IBotConfigProvider _bcp;

		public BotConfigController(DbService db, IBotCredentials creds, IBotConfigProvider bcp) {
			_db    = db;
			_creds = creds;
			_bcp   = bcp;
		}

		// GET: Settings/BotConfig
		public IActionResult Index() {
			using var uow = _db.UnitOfWork;
			var botConfig = uow.BotConfig.GetOrCreate();

			return DiscordUser.BotPagePermissions.HasFlag(BotLevelPermission.WriteBotConfig) ? View("Edit", botConfig) : View(botConfig);
		}

		// POST: Settings/BotConfig
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize("WriteBotConfig")]
		public async Task<IActionResult> Index([Bind("ForwardMessages,ForwardToAllOwners,CurrencyGenerationChance,CurrencyGenerationCooldown,RotatingStatuses,RemindMessageFormat,CurrencySign,CurrencyName,CurrencyPluralName,MinimumBetAmount,BetflipMultiplier,CurrencyDropAmount,CurrencyDropAmountMax,Betroll67Multiplier,Betroll91Multiplier,Betroll100Multiplier,DMHelpString,HelpString,OkColor,ErrorColor,Locale,DefaultPrefix,CustomReactionsStartWith,LogUsernames,FirstAprilHereChance,DmCommandsOwnerOnly,Id")] BotConfig botConfig) {
			if(ModelState.IsValid) {
				using var uow = _db.UnitOfWork;
				if(uow.BotConfig.GetOrCreate().Id != botConfig.Id) {
					return NotFound();
				} else {
					var bc = uow.BotConfig.GetOrCreate();

					bc.ForwardMessages            = botConfig.ForwardMessages;
					bc.ForwardToAllOwners         = botConfig.ForwardToAllOwners;
					bc.CurrencyGenerationChance   = botConfig.CurrencyGenerationChance;
					bc.CurrencyGenerationCooldown = botConfig.CurrencyGenerationCooldown;
					bc.RotatingStatuses           = botConfig.RotatingStatuses;
					bc.RemindMessageFormat        = botConfig.RemindMessageFormat;
					bc.CurrencySign               = botConfig.CurrencySign;
					bc.CurrencyName               = botConfig.CurrencyName;
					bc.CurrencyPluralName         = botConfig.CurrencyPluralName;
					bc.MinimumBetAmount           = botConfig.MinimumBetAmount;
					bc.BetflipMultiplier          = botConfig.BetflipMultiplier;
					bc.CurrencyDropAmount         = botConfig.CurrencyDropAmount;
					bc.CurrencyDropAmountMax      = botConfig.CurrencyDropAmountMax;
					bc.Betroll67Multiplier        = botConfig.Betroll67Multiplier;
					bc.Betroll91Multiplier        = botConfig.Betroll91Multiplier;
					bc.Betroll100Multiplier       = botConfig.Betroll100Multiplier;
					bc.DMHelpString               = botConfig.DMHelpString;
					bc.HelpString                 = botConfig.HelpString;
					bc.OkColor                    = botConfig.OkColor;
					bc.ErrorColor                 = botConfig.ErrorColor;
					bc.Locale                     = botConfig.Locale;
					bc.DefaultPrefix              = botConfig.DefaultPrefix;
					bc.CustomReactionsStartWith   = botConfig.CustomReactionsStartWith;
					bc.LogUsernames               = botConfig.LogUsernames;
					bc.FirstAprilHereChance       = botConfig.FirstAprilHereChance;
					bc.DmCommandsOwnerOnly        = botConfig.DmCommandsOwnerOnly;

					uow.BotConfig.Update(bc);
					await uow.SaveChangesAsync();

					_bcp.Reload();

					return RedirectToAction(nameof(Index));
				}
			} else {
				return View(botConfig);
			}
		}
	}
}
