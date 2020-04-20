﻿using Mitternacht.Common;
using Mitternacht.Services.Database.Models;
using System;
using System.Globalization;

namespace Mitternacht.Services.Impl {
	public class BotConfigProvider : IBotConfigProvider {
		private readonly DbService _db;
		public BotConfig BotConfig { get; private set; }

		public event Action<BotConfig> BotConfigChanged;

		public BotConfigProvider(DbService db, BotConfig bc) {
			_db       = db;
			BotConfig = bc;
		}

		public void Reload() {
			using var uow = _db.UnitOfWork;
			BotConfig     = uow.BotConfig.GetOrCreate();

			BotConfigChanged?.Invoke(BotConfig);
		}

		public bool Edit(BotConfigEditType type, string newValue) {
			using var uow = _db.UnitOfWork;
			var bc = uow.BotConfig.GetOrCreate();
			switch(type) {
				case BotConfigEditType.CurrencyGenerationChance:
					if(float.TryParse(newValue, out var chance) && chance >= 0 && chance <= 1)
						bc.CurrencyGenerationChance = chance;
					else
						return false;
					break;
				case BotConfigEditType.CurrencyGenerationCooldown:
					if(int.TryParse(newValue, out var cd) && cd >= 1)
						bc.CurrencyGenerationCooldown = cd;
					else
						return false;
					break;
				case BotConfigEditType.CurrencyName:
					bc.CurrencyName = newValue ?? "-";
					break;
				case BotConfigEditType.CurrencyPluralName:
					bc.CurrencyPluralName = newValue ?? bc.CurrencyName + "s";
					break;
				case BotConfigEditType.CurrencySign:
					bc.CurrencySign = newValue ?? "-";
					break;
				case BotConfigEditType.DmHelpString:
					bc.DMHelpString = string.IsNullOrWhiteSpace(newValue) ? "-" : newValue;
					break;
				case BotConfigEditType.HelpString:
					bc.HelpString = string.IsNullOrWhiteSpace(newValue) ? "-" : newValue;
					break;
				case BotConfigEditType.CurrencyDropAmount:
					if(int.TryParse(newValue, out var amount) && amount > 0)
						bc.CurrencyDropAmount = amount;
					else
						return false;
					break;
				case BotConfigEditType.CurrencyDropAmountMax:
					if(newValue == null)
						bc.CurrencyDropAmountMax = null;
					else if(int.TryParse(newValue, out var maxAmount) && maxAmount > 0)
						bc.CurrencyDropAmountMax = maxAmount;
					else
						return false;
					break;
				case BotConfigEditType.MinimumBetAmount:
					if(int.TryParse(newValue, out var minBetAmount) && minBetAmount > 0)
						bc.MinimumBetAmount = minBetAmount;
					else
						return false;
					break;
				case BotConfigEditType.TriviaCurrencyReward:
					if(int.TryParse(newValue, out var triviaReward) && triviaReward > 0)
						bc.TriviaCurrencyReward = triviaReward;
					else
						return false;
					break;
				case BotConfigEditType.Betroll100Multiplier:
					if(float.TryParse(newValue, out var br100) && br100 > 0)
						bc.Betroll100Multiplier = br100;
					else
						return false;
					break;
				case BotConfigEditType.Betroll91Multiplier:
					if(int.TryParse(newValue, out var br91) && br91 > 0)
						bc.Betroll91Multiplier = br91;
					else
						return false;
					break;
				case BotConfigEditType.Betroll67Multiplier:
					if(int.TryParse(newValue, out var br67) && br67 > 0)
						bc.Betroll67Multiplier = br67;
					else
						return false;
					break;
				case BotConfigEditType.BetflipMultiplier:
					if(int.TryParse(newValue, out var bf) && bf > 0)
						bc.BetflipMultiplier = bf;
					else
						return false;
					break;
				case BotConfigEditType.HereChance:
					if(double.TryParse(newValue, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out var evchance) && evchance > 0)
						bc.FirstAprilHereChance = evchance;
					else
						return false;
					break;
				case BotConfigEditType.DmCommandsOwnerOnly:
					if(bool.TryParse(newValue, out var owneronly))
						bc.DmCommandsOwnerOnly = owneronly;
					else
						return false;
					break;
				default:
					return false;
			}

			BotConfig = bc;
			uow.Complete();

			BotConfigChanged?.Invoke(BotConfig);
			
			return true;
		}
	}
}