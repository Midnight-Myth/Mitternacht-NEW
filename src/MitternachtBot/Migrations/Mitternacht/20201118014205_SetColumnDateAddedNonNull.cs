using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MoreLinq.Extensions;

namespace Mitternacht.Migrations.Mitternacht {
	public partial class SetColumnDateAddedNonNull : Migration {
		protected override void Up(MigrationBuilder migrationBuilder) {
			new string[]{
				"ZalgoFilterChannel",
				"Warnings",
				"WarningPunishment",
				"VoiceChannelStats",
				"VerifiedUsers",
				"VcRoleInfo",
				"UserRoleColorBindings",
				"UsernameHistory",
				"UnmuteTimer",
				"TeamUpdateRank",
				"StartupCommand",
				"ShopEntryItem",
				"ShopEntry",
				"SelfAssignableRoles",
				"RoleMoney",
				"RoleLevelBinding",
				"RewardedUser",
				"Reminders",
				"Quotes",
				"PlayingStatus",
				"Permissionv2",
				"Permission",
				"NsfwBlacklitedTag",
				"MutedUserId",
				"MessageXpRestrictions",
				"LogSetting",
				"LevelModel",
				"IgnoredVoicePresenceChannel",
				"IgnoredLogChannel",
				"GuildRepeater",
				"GuildConfigs",
				"GCChannelId",
				"FilteredWord",
				"FilterChannelId",
				"EightBallResponse",
				"Donators",
				"DailyMoneyStats",
				"DailyMoney",
				"CustomReactions",
				"CurrencyTransactions",
				"Currency",
				"CommandCooldown",
				"CommandAlias",
				"BotConfig",
				"BlockedCmdOrMdl",
				"BlacklistItem",
				"BirthDates",
				"AntiSpamSetting",
				"AntiSpamIgnore",
				"AntiRaidSetting",
			}.ForEach(table => {
				migrationBuilder.Sql($"UPDATE \"{table}\" SET \"DateAdded\" = '0001-01-01 00:00:00.0' WHERE \"DateAdded\" IS NULL");
			});

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "ZalgoFilterChannel",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "Warnings",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "WarningPunishment",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "VoiceChannelStats",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "VerifiedUsers",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "VcRoleInfo",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "UserRoleColorBindings",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "UsernameHistory",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "UnmuteTimer",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "TeamUpdateRank",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "StartupCommand",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "ShopEntryItem",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "ShopEntry",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "SelfAssignableRoles",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "RoleMoney",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "RoleLevelBinding",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "RewardedUser",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "Reminders",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "Quotes",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "PlayingStatus",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "Permissionv2",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "Permission",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "NsfwBlacklitedTag",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "MutedUserId",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "MessageXpRestrictions",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "LogSetting",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "LevelModel",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "IgnoredVoicePresenceChannel",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "IgnoredLogChannel",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "GuildRepeater",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "GuildConfigs",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "GCChannelId",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "FilteredWord",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "FilterChannelId",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "EightBallResponse",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "Donators",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "DailyMoneyStats",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "DailyMoney",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "CustomReactions",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "CurrencyTransactions",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "Currency",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "CommandCooldown",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "CommandAlias",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "BotConfig",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "BlockedCmdOrMdl",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "BlacklistItem",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "BirthDates",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "AntiSpamSetting",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "AntiSpamIgnore",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "AntiRaidSetting",
				type: "timestamp without time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone",
				oldNullable: true);
		}

		protected override void Down(MigrationBuilder migrationBuilder) {
			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "ZalgoFilterChannel",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "Warnings",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "WarningPunishment",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "VoiceChannelStats",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "VerifiedUsers",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "VcRoleInfo",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "UserRoleColorBindings",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "UsernameHistory",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "UnmuteTimer",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "TeamUpdateRank",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "StartupCommand",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "ShopEntryItem",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "ShopEntry",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "SelfAssignableRoles",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "RoleMoney",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "RoleLevelBinding",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "RewardedUser",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "Reminders",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "Quotes",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "PlayingStatus",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "Permissionv2",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "Permission",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "NsfwBlacklitedTag",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "MutedUserId",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "MessageXpRestrictions",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "LogSetting",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "LevelModel",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "IgnoredVoicePresenceChannel",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "IgnoredLogChannel",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "GuildRepeater",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "GuildConfigs",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "GCChannelId",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "FilteredWord",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "FilterChannelId",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "EightBallResponse",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "Donators",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "DailyMoneyStats",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "DailyMoney",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "CustomReactions",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "CurrencyTransactions",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "Currency",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "CommandCooldown",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "CommandAlias",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "BotConfig",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "BlockedCmdOrMdl",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "BlacklistItem",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "BirthDates",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "AntiSpamSetting",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "AntiSpamIgnore",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "DateAdded",
				table: "AntiRaidSetting",
				type: "timestamp without time zone",
				nullable: true,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");
		}
	}
}
