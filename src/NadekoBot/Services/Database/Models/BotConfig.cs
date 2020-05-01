using System;
using System.Collections.Generic;

namespace Mitternacht.Services.Database.Models {
	public class BotConfig : DbEntity {
		public ulong    BufferSize                 { get; set; } = 4000000;
		public bool     ForwardMessages            { get; set; } = true;
		public bool     ForwardToAllOwners         { get; set; } = true;
		public float    CurrencyGenerationChance   { get; set; } = 0.02f;
		public int      CurrencyGenerationCooldown { get; set; } = 10;
		public bool     RotatingStatuses           { get; set; } = false;
		public string   RemindMessageFormat        { get; set; } = "❗⏰**I've been told to remind you to '%message%' now by %user%.**⏰❗";
		public string   CurrencySign               { get; set; } = "💶";
		public string   CurrencyName               { get; set; } = "Money";
		public string   CurrencyPluralName         { get; set; } = "Money";
		public int      TriviaCurrencyReward       { get; set; } = 0;
		public int      MinimumBetAmount           { get; set; } = 2;
		public float    BetflipMultiplier          { get; set; } = 1.95f;
		public int      CurrencyDropAmount         { get; set; } = 1;
		public int?     CurrencyDropAmountMax      { get; set; } = null;
		public float    Betroll67Multiplier        { get; set; } = 2;
		public float    Betroll91Multiplier        { get; set; } = 4;
		public float    Betroll100Multiplier       { get; set; } = 10;
		public string   DMHelpString               { get; set; } = "Type `.h` for help.";
		public string   HelpString                 { get; set; } = @"You can use `{1}modules` command to see a list of all modules.
You can use `{1}commands ModuleName` (for example `{1}commands Administration`) to see a list of all of the commands in that module.
For a specific command help, use `{1}h CommandName` (for example {1}h {1}q).

**LIST OF COMMANDS:** <http://nadekobot.readthedocs.io/en/latest/Commands%20List/>";
		public string   OkColor                    { get; set; } = "71cd40";
		public string   ErrorColor                 { get; set; } = "ee281f";
		public string   Locale                     { get; set; } = null;
		public string   DefaultPrefix              { get; set; } = ".";
		public bool     CustomReactionsStartWith   { get; set; } = false;
		public bool     LogUsernames               { get; set; } = true;
		public DateTime LastTimeBirthdaysChecked   { get; set; } = DateTime.MinValue;
		public double   FirstAprilHereChance       { get; set; } = 0.005;
		public bool     DmCommandsOwnerOnly        { get; set; } = true;

		public HashSet<BlacklistItem>     Blacklist              { get; set; }
		public HashSet<ModulePrefix>      ModulePrefixes         { get; set; } = new HashSet<ModulePrefix>();
		public List<PlayingStatus>        RotatingStatusMessages { get; set; } = new List<PlayingStatus>();
		public HashSet<EightBallResponse> EightBallResponses     { get; set; } = new HashSet<EightBallResponse>();
		public HashSet<RaceAnimal>        RaceAnimals            { get; set; } = new HashSet<RaceAnimal>();
		public List<StartupCommand>       StartupCommands        { get; set; }
		public HashSet<BlockedCmdOrMdl>   BlockedCommands        { get; set; }
		public HashSet<BlockedCmdOrMdl>   BlockedModules         { get; set; }

		public int PermissionVersion { get; set; }
		public int MigrationVersion  { get; set; }


		[Obsolete]
		public HashSet<CommandPrice> CommandPrices { get; set; } = new HashSet<CommandPrice>();
	}

	public class BlockedCmdOrMdl : DbEntity {
		public string Name { get; set; }

		public override bool Equals(object obj)
			=> obj is BlockedCmdOrMdl bcm ? bcm.Name.Equals(Name, StringComparison.OrdinalIgnoreCase) : base.Equals(obj);

		public override int GetHashCode()
			=> HashCode.Combine(Name);
	}

	public class StartupCommand : DbEntity, IIndexed {
		public int    Index            { get; set; }
		public string CommandText      { get; set; }
		public ulong  ChannelId        { get; set; }
		public string ChannelName      { get; set; }
		public ulong? GuildId          { get; set; }
		public string GuildName        { get; set; }
		public ulong? VoiceChannelId   { get; set; }
		public string VoiceChannelName { get; set; }
	}

	public class PlayingStatus : DbEntity {
		public string Status { get; set; }
	}

	public class BlacklistItem : DbEntity {
		public ulong         ItemId { get; set; }
		public BlacklistType Type   { get; set; }
	}

	public enum BlacklistType {
		Server,
		Channel,
		User
	}

	public class EightBallResponse : DbEntity {
		public string Text { get; set; }

		public override int GetHashCode()
			=> HashCode.Combine(Text);

		public override bool Equals(object obj)
			=> obj is EightBallResponse er ? er.Text.Equals(Text) : base.Equals(obj);
	}

	public class RaceAnimal : DbEntity {
		public string Icon { get; set; }
		public string Name { get; set; }

		public override int GetHashCode()
			=> HashCode.Combine(Icon);

		public override bool Equals(object obj)
			=> obj is RaceAnimal ra ? ra.Icon.Equals(Icon) : base.Equals(obj);
	}

	public class ModulePrefix : DbEntity {
		public string ModuleName { get; set; }
		public string Prefix     { get; set; }

		public override int GetHashCode()
			=> HashCode.Combine(ModuleName);

		public override bool Equals(object obj)
			=> obj is ModulePrefix mp ? mp.ModuleName.Equals(ModuleName) : base.Equals(obj);
	}
}
