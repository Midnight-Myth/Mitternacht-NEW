using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Mitternacht.Migrations.Mitternacht
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BirthDates",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    UserId = table.Column<decimal>(nullable: false),
                    Day = table.Column<int>(nullable: false),
                    Month = table.Column<int>(nullable: false),
                    Year = table.Column<int>(nullable: true),
                    BirthdayMessageEnabled = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BirthDates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BotConfig",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    BufferSize = table.Column<decimal>(nullable: false),
                    ForwardMessages = table.Column<bool>(nullable: false),
                    ForwardToAllOwners = table.Column<bool>(nullable: false),
                    CurrencyGenerationChance = table.Column<float>(nullable: false),
                    CurrencyGenerationCooldown = table.Column<int>(nullable: false),
                    RotatingStatuses = table.Column<bool>(nullable: false),
                    RemindMessageFormat = table.Column<string>(nullable: true),
                    CurrencySign = table.Column<string>(nullable: true),
                    CurrencyName = table.Column<string>(nullable: true),
                    CurrencyPluralName = table.Column<string>(nullable: true),
                    TriviaCurrencyReward = table.Column<int>(nullable: false),
                    MinimumBetAmount = table.Column<int>(nullable: false),
                    BetflipMultiplier = table.Column<float>(nullable: false),
                    CurrencyDropAmount = table.Column<int>(nullable: false),
                    CurrencyDropAmountMax = table.Column<int>(nullable: true),
                    Betroll67Multiplier = table.Column<float>(nullable: false),
                    Betroll91Multiplier = table.Column<float>(nullable: false),
                    Betroll100Multiplier = table.Column<float>(nullable: false),
                    DMHelpString = table.Column<string>(nullable: true),
                    HelpString = table.Column<string>(nullable: true),
                    OkColor = table.Column<string>(nullable: true),
                    ErrorColor = table.Column<string>(nullable: true),
                    Locale = table.Column<string>(nullable: true),
                    DefaultPrefix = table.Column<string>(nullable: true),
                    CustomReactionsStartWith = table.Column<bool>(nullable: false),
                    LogUsernames = table.Column<bool>(nullable: false),
                    LastTimeBirthdaysChecked = table.Column<DateTime>(nullable: false),
                    FirstAprilHereChance = table.Column<double>(nullable: false),
                    DmCommandsOwnerOnly = table.Column<bool>(nullable: false),
                    PermissionVersion = table.Column<int>(nullable: false),
                    MigrationVersion = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotConfig", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currency",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    UserId = table.Column<decimal>(nullable: false),
                    Amount = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currency", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CurrencyTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    Amount = table.Column<long>(nullable: false),
                    Reason = table.Column<string>(nullable: true),
                    UserId = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomReactions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    GuildId = table.Column<decimal>(nullable: true),
                    Response = table.Column<string>(nullable: true),
                    Trigger = table.Column<string>(nullable: true),
                    IsRegex = table.Column<bool>(nullable: false),
                    OwnerOnly = table.Column<bool>(nullable: false),
                    AutoDeleteTrigger = table.Column<bool>(nullable: false),
                    DmResponse = table.Column<bool>(nullable: false),
                    ContainsAnywhere = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomReactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyMoney",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    UserId = table.Column<decimal>(nullable: false),
                    LastTimeGotten = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyMoney", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyMoneyStats",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    UserId = table.Column<decimal>(nullable: false),
                    TimeReceived = table.Column<DateTime>(nullable: false),
                    MoneyReceived = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyMoneyStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Donators",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    UserId = table.Column<decimal>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Amount = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Donators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LevelModel",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    GuildId = table.Column<decimal>(nullable: false),
                    UserId = table.Column<decimal>(nullable: false),
                    TotalXP = table.Column<int>(nullable: false),
                    timestamp = table.Column<DateTime>(nullable: false),
                    CurrentXP = table.Column<int>(nullable: false),
                    Level = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LevelModel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogSetting",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    LogOtherId = table.Column<decimal>(nullable: true),
                    MessageUpdatedId = table.Column<decimal>(nullable: true),
                    MessageDeletedId = table.Column<decimal>(nullable: true),
                    UserJoinedId = table.Column<decimal>(nullable: true),
                    UserLeftId = table.Column<decimal>(nullable: true),
                    UserBannedId = table.Column<decimal>(nullable: true),
                    UserUnbannedId = table.Column<decimal>(nullable: true),
                    UserUpdatedId = table.Column<decimal>(nullable: true),
                    ChannelCreatedId = table.Column<decimal>(nullable: true),
                    ChannelDestroyedId = table.Column<decimal>(nullable: true),
                    ChannelUpdatedId = table.Column<decimal>(nullable: true),
                    UserMutedId = table.Column<decimal>(nullable: true),
                    LogUserPresenceId = table.Column<decimal>(nullable: true),
                    LogVoicePresenceId = table.Column<decimal>(nullable: true),
                    LogVoicePresenceTTSId = table.Column<decimal>(nullable: true),
                    VerificationSteps = table.Column<decimal>(nullable: true),
                    VerificationMessages = table.Column<decimal>(nullable: true),
                    IsLogging = table.Column<bool>(nullable: false),
                    ChannelId = table.Column<decimal>(nullable: false),
                    MessageUpdated = table.Column<bool>(nullable: false),
                    MessageDeleted = table.Column<bool>(nullable: false),
                    UserJoined = table.Column<bool>(nullable: false),
                    UserLeft = table.Column<bool>(nullable: false),
                    UserBanned = table.Column<bool>(nullable: false),
                    UserUnbanned = table.Column<bool>(nullable: false),
                    UserUpdated = table.Column<bool>(nullable: false),
                    ChannelCreated = table.Column<bool>(nullable: false),
                    ChannelDestroyed = table.Column<bool>(nullable: false),
                    ChannelUpdated = table.Column<bool>(nullable: false),
                    LogUserPresence = table.Column<bool>(nullable: false),
                    UserPresenceChannelId = table.Column<decimal>(nullable: false),
                    LogVoicePresence = table.Column<bool>(nullable: false),
                    VoicePresenceChannelId = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogSetting", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessageXpRestrictions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    GuildId = table.Column<decimal>(nullable: false),
                    ChannelId = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageXpRestrictions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permission",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    NextId = table.Column<int>(nullable: true),
                    PrimaryTarget = table.Column<int>(nullable: false),
                    PrimaryTargetId = table.Column<decimal>(nullable: false),
                    SecondaryTarget = table.Column<int>(nullable: false),
                    SecondaryTargetName = table.Column<string>(nullable: true),
                    State = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permission_Permission_NextId",
                        column: x => x.NextId,
                        principalTable: "Permission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    GuildId = table.Column<decimal>(nullable: false),
                    Keyword = table.Column<string>(nullable: false),
                    AuthorName = table.Column<string>(nullable: false),
                    AuthorId = table.Column<decimal>(nullable: false),
                    Text = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reminders",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    When = table.Column<DateTime>(nullable: false),
                    ChannelId = table.Column<decimal>(nullable: false),
                    ServerId = table.Column<decimal>(nullable: false),
                    UserId = table.Column<decimal>(nullable: false),
                    Message = table.Column<string>(nullable: true),
                    IsPrivate = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reminders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RewardedUser",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    UserId = table.Column<decimal>(nullable: false),
                    PatreonUserId = table.Column<string>(nullable: true),
                    AmountRewardedThisMonth = table.Column<int>(nullable: false),
                    LastReward = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardedUser", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleLevelBinding",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    RoleId = table.Column<decimal>(nullable: false),
                    MinimumLevel = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleLevelBinding", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleMoney",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    RoleId = table.Column<decimal>(nullable: false),
                    Money = table.Column<long>(nullable: false),
                    Priority = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleMoney", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SelfAssignableRoles",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    GuildId = table.Column<decimal>(nullable: false),
                    RoleId = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelfAssignableRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeamUpdateRank",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    GuildId = table.Column<decimal>(nullable: false),
                    Rankname = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamUpdateRank", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsernameHistory",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    UserId = table.Column<decimal>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    DiscordDiscriminator = table.Column<int>(nullable: false),
                    DateSet = table.Column<DateTime>(nullable: false),
                    DateReplaced = table.Column<DateTime>(nullable: true),
                    Discriminator = table.Column<string>(nullable: false),
                    GuildId = table.Column<decimal>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsernameHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VerifiedUsers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    GuildId = table.Column<decimal>(nullable: false),
                    UserId = table.Column<decimal>(nullable: false),
                    ForumUserId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerifiedUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VoiceChannelStats",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    UserId = table.Column<decimal>(nullable: false),
                    GuildId = table.Column<decimal>(nullable: false),
                    TimeInVoiceChannel = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceChannelStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warnings",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    GuildId = table.Column<decimal>(nullable: false),
                    UserId = table.Column<decimal>(nullable: false),
                    Reason = table.Column<string>(nullable: true),
                    Forgiven = table.Column<bool>(nullable: false),
                    ForgivenBy = table.Column<string>(nullable: true),
                    Moderator = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warnings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BlacklistItem",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    ItemId = table.Column<decimal>(nullable: false),
                    Type = table.Column<int>(nullable: false),
                    BotConfigId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlacklistItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlacklistItem_BotConfig_BotConfigId",
                        column: x => x.BotConfigId,
                        principalTable: "BotConfig",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BlockedCmdOrMdl",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    BotConfigId = table.Column<int>(nullable: true),
                    BotConfigId1 = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockedCmdOrMdl", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlockedCmdOrMdl_BotConfig_BotConfigId",
                        column: x => x.BotConfigId,
                        principalTable: "BotConfig",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BlockedCmdOrMdl_BotConfig_BotConfigId1",
                        column: x => x.BotConfigId1,
                        principalTable: "BotConfig",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommandPrice",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    Price = table.Column<int>(nullable: false),
                    CommandName = table.Column<string>(nullable: true),
                    BotConfigId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandPrice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommandPrice_BotConfig_BotConfigId",
                        column: x => x.BotConfigId,
                        principalTable: "BotConfig",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EightBallResponse",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    Text = table.Column<string>(nullable: true),
                    BotConfigId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EightBallResponse", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EightBallResponse_BotConfig_BotConfigId",
                        column: x => x.BotConfigId,
                        principalTable: "BotConfig",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModulePrefix",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    ModuleName = table.Column<string>(nullable: true),
                    Prefix = table.Column<string>(nullable: true),
                    BotConfigId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModulePrefix", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModulePrefix_BotConfig_BotConfigId",
                        column: x => x.BotConfigId,
                        principalTable: "BotConfig",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayingStatus",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    Status = table.Column<string>(nullable: true),
                    BotConfigId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayingStatus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayingStatus_BotConfig_BotConfigId",
                        column: x => x.BotConfigId,
                        principalTable: "BotConfig",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RaceAnimal",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    Icon = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    BotConfigId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaceAnimal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaceAnimal_BotConfig_BotConfigId",
                        column: x => x.BotConfigId,
                        principalTable: "BotConfig",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StartupCommand",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    Index = table.Column<int>(nullable: false),
                    CommandText = table.Column<string>(nullable: true),
                    ChannelId = table.Column<decimal>(nullable: false),
                    ChannelName = table.Column<string>(nullable: true),
                    GuildId = table.Column<decimal>(nullable: true),
                    GuildName = table.Column<string>(nullable: true),
                    VoiceChannelId = table.Column<decimal>(nullable: true),
                    VoiceChannelName = table.Column<string>(nullable: true),
                    BotConfigId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StartupCommand", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StartupCommand_BotConfig_BotConfigId",
                        column: x => x.BotConfigId,
                        principalTable: "BotConfig",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IgnoredLogChannel",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    LogSettingId = table.Column<int>(nullable: true),
                    ChannelId = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IgnoredLogChannel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IgnoredLogChannel_LogSetting_LogSettingId",
                        column: x => x.LogSettingId,
                        principalTable: "LogSetting",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IgnoredVoicePresenceChannel",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    LogSettingId = table.Column<int>(nullable: true),
                    ChannelId = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IgnoredVoicePresenceChannel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IgnoredVoicePresenceChannel_LogSetting_LogSettingId",
                        column: x => x.LogSettingId,
                        principalTable: "LogSetting",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GuildConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    GuildId = table.Column<decimal>(nullable: false),
                    Prefix = table.Column<string>(nullable: true),
                    DeleteMessageOnCommand = table.Column<bool>(nullable: false),
                    AutoAssignRoleId = table.Column<decimal>(nullable: false),
                    AutoDeleteGreetMessagesTimer = table.Column<int>(nullable: false),
                    AutoDeleteByeMessagesTimer = table.Column<int>(nullable: false),
                    GreetMessageChannelId = table.Column<decimal>(nullable: false),
                    ByeMessageChannelId = table.Column<decimal>(nullable: false),
                    SendDmGreetMessage = table.Column<bool>(nullable: false),
                    DmGreetMessageText = table.Column<string>(nullable: true),
                    SendChannelGreetMessage = table.Column<bool>(nullable: false),
                    ChannelGreetMessageText = table.Column<string>(nullable: true),
                    SendChannelByeMessage = table.Column<bool>(nullable: false),
                    ChannelByeMessageText = table.Column<string>(nullable: true),
                    ExclusiveSelfAssignedRoles = table.Column<bool>(nullable: false),
                    AutoDeleteSelfAssignedRoleMessages = table.Column<bool>(nullable: false),
                    DefaultMusicVolume = table.Column<float>(nullable: false),
                    VoicePlusTextEnabled = table.Column<bool>(nullable: false),
                    CleverbotEnabled = table.Column<bool>(nullable: false),
                    MuteRoleName = table.Column<string>(nullable: true),
                    Locale = table.Column<string>(nullable: true),
                    TimeZoneId = table.Column<string>(nullable: true),
                    GameVoiceChannel = table.Column<decimal>(nullable: true),
                    VerboseErrors = table.Column<bool>(nullable: false),
                    VerifiedRoleId = table.Column<decimal>(nullable: true),
                    VerifyString = table.Column<string>(nullable: true),
                    VerificationTutorialText = table.Column<string>(nullable: true),
                    AdditionalVerificationUsers = table.Column<string>(nullable: true),
                    VerificationPasswordChannelId = table.Column<decimal>(nullable: true),
                    TurnToXpMultiplier = table.Column<double>(nullable: false),
                    MessageXpTimeDifference = table.Column<double>(nullable: false),
                    MessageXpCharCountMin = table.Column<int>(nullable: false),
                    MessageXpCharCountMax = table.Column<int>(nullable: false),
                    LogUsernameHistory = table.Column<bool>(nullable: true),
                    BirthdayRoleId = table.Column<decimal>(nullable: true),
                    BirthdayMessage = table.Column<string>(nullable: true),
                    BirthdayMessageChannelId = table.Column<decimal>(nullable: true),
                    BirthdaysEnabled = table.Column<bool>(nullable: false),
                    BirthdayMoney = table.Column<long>(nullable: true),
                    GommeTeamMemberRoleId = table.Column<decimal>(nullable: true),
                    VipRoleId = table.Column<decimal>(nullable: true),
                    TeamUpdateChannelId = table.Column<decimal>(nullable: true),
                    TeamUpdateMessagePrefix = table.Column<string>(nullable: true),
                    CountToNumberChannelId = table.Column<decimal>(nullable: true),
                    CountToNumberMessageChance = table.Column<double>(nullable: false),
                    ForumNotificationChannelId = table.Column<decimal>(nullable: true),
                    RootPermissionId = table.Column<int>(nullable: true),
                    VerbosePermissions = table.Column<bool>(nullable: false),
                    PermissionRole = table.Column<string>(nullable: true),
                    FilterInvites = table.Column<bool>(nullable: false),
                    FilterWords = table.Column<bool>(nullable: false),
                    FilterZalgo = table.Column<bool>(nullable: false),
                    LogSettingId = table.Column<int>(nullable: true),
                    WarningsInitialized = table.Column<bool>(nullable: false),
                    AutoDeleteGreetMessages = table.Column<bool>(nullable: false),
                    AutoDeleteByeMessages = table.Column<bool>(nullable: false),
                    SupportChannelId = table.Column<decimal>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildConfigs_LogSetting_LogSettingId",
                        column: x => x.LogSettingId,
                        principalTable: "LogSetting",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GuildConfigs_Permission_RootPermissionId",
                        column: x => x.RootPermissionId,
                        principalTable: "Permission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AntiRaidSetting",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    GuildConfigId = table.Column<int>(nullable: false),
                    UserThreshold = table.Column<int>(nullable: false),
                    Seconds = table.Column<int>(nullable: false),
                    Action = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AntiRaidSetting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AntiRaidSetting_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AntiSpamSetting",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    GuildConfigId = table.Column<int>(nullable: false),
                    Action = table.Column<int>(nullable: false),
                    MessageThreshold = table.Column<int>(nullable: false),
                    MuteTime = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AntiSpamSetting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AntiSpamSetting_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommandAlias",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    Trigger = table.Column<string>(nullable: true),
                    Mapping = table.Column<string>(nullable: true),
                    GuildConfigId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandAlias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommandAlias_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommandCooldown",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    Seconds = table.Column<int>(nullable: false),
                    CommandName = table.Column<string>(nullable: true),
                    GuildConfigId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandCooldown", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommandCooldown_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FilterChannelId",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    ChannelId = table.Column<decimal>(nullable: false),
                    GuildConfigId = table.Column<int>(nullable: true),
                    GuildConfigId1 = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilterChannelId", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FilterChannelId_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FilterChannelId_GuildConfigs_GuildConfigId1",
                        column: x => x.GuildConfigId1,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FilteredWord",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    Word = table.Column<string>(nullable: true),
                    GuildConfigId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilteredWord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FilteredWord_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FollowedStream",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    ChannelId = table.Column<decimal>(nullable: false),
                    Username = table.Column<string>(nullable: true),
                    Type = table.Column<int>(nullable: false),
                    GuildId = table.Column<decimal>(nullable: false),
                    GuildConfigId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowedStream", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FollowedStream_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GCChannelId",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    ChannelId = table.Column<decimal>(nullable: false),
                    GuildConfigId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GCChannelId", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GCChannelId_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GuildRepeater",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    GuildId = table.Column<decimal>(nullable: false),
                    ChannelId = table.Column<decimal>(nullable: false),
                    Message = table.Column<string>(nullable: true),
                    Interval = table.Column<TimeSpan>(nullable: false),
                    StartTimeOfDay = table.Column<TimeSpan>(nullable: true),
                    GuildConfigId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildRepeater", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildRepeater_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MutedUserId",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    UserId = table.Column<decimal>(nullable: false),
                    GuildConfigId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MutedUserId", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MutedUserId_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NsfwBlacklitedTag",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    Tag = table.Column<string>(nullable: true),
                    GuildConfigId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NsfwBlacklitedTag", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NsfwBlacklitedTag_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Permissionv2",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    GuildConfigId = table.Column<int>(nullable: true),
                    Index = table.Column<int>(nullable: false),
                    PrimaryTarget = table.Column<int>(nullable: false),
                    PrimaryTargetId = table.Column<decimal>(nullable: false),
                    SecondaryTarget = table.Column<int>(nullable: false),
                    SecondaryTargetName = table.Column<string>(nullable: true),
                    State = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissionv2", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permissionv2_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShopEntry",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    Index = table.Column<int>(nullable: false),
                    Price = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    AuthorId = table.Column<decimal>(nullable: false),
                    Type = table.Column<int>(nullable: false),
                    RoleName = table.Column<string>(nullable: true),
                    RoleId = table.Column<decimal>(nullable: false),
                    GuildConfigId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopEntry_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SlowmodeIgnoredRole",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    RoleId = table.Column<decimal>(nullable: false),
                    GuildConfigId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlowmodeIgnoredRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlowmodeIgnoredRole_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SlowmodeIgnoredUser",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    UserId = table.Column<decimal>(nullable: false),
                    GuildConfigId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlowmodeIgnoredUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlowmodeIgnoredUser_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StreamRoleSettings",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    GuildConfigId = table.Column<int>(nullable: false),
                    Enabled = table.Column<bool>(nullable: false),
                    AddRoleId = table.Column<decimal>(nullable: false),
                    FromRoleId = table.Column<decimal>(nullable: false),
                    Keyword = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamRoleSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StreamRoleSettings_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UnmuteTimer",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    UserId = table.Column<decimal>(nullable: false),
                    UnmuteAt = table.Column<DateTime>(nullable: false),
                    GuildConfigId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnmuteTimer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnmuteTimer_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VcRoleInfo",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    VoiceChannelId = table.Column<decimal>(nullable: false),
                    RoleId = table.Column<decimal>(nullable: false),
                    GuildConfigId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VcRoleInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VcRoleInfo_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarningPunishment",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    Count = table.Column<int>(nullable: false),
                    Time = table.Column<int>(nullable: false),
                    Punishment = table.Column<int>(nullable: false),
                    GuildConfigId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarningPunishment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarningPunishment_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ZalgoFilterChannel",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    ChannelId = table.Column<decimal>(nullable: false),
                    GuildConfigId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZalgoFilterChannel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZalgoFilterChannel_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AntiSpamIgnore",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    ChannelId = table.Column<decimal>(nullable: false),
                    AntiSpamSettingId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AntiSpamIgnore", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AntiSpamIgnore_AntiSpamSetting_AntiSpamSettingId",
                        column: x => x.AntiSpamSettingId,
                        principalTable: "AntiSpamSetting",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShopEntryItem",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    Text = table.Column<string>(nullable: true),
                    ShopEntryId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopEntryItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopEntryItem_ShopEntry_ShopEntryId",
                        column: x => x.ShopEntryId,
                        principalTable: "ShopEntry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StreamRoleBlacklistedUser",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    UserId = table.Column<decimal>(nullable: false),
                    Username = table.Column<string>(nullable: true),
                    StreamRoleSettingsId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamRoleBlacklistedUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StreamRoleBlacklistedUser_StreamRoleSettings_StreamRoleSett~",
                        column: x => x.StreamRoleSettingsId,
                        principalTable: "StreamRoleSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StreamRoleWhitelistedUser",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    UserId = table.Column<decimal>(nullable: false),
                    Username = table.Column<string>(nullable: true),
                    StreamRoleSettingsId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamRoleWhitelistedUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StreamRoleWhitelistedUser_StreamRoleSettings_StreamRoleSett~",
                        column: x => x.StreamRoleSettingsId,
                        principalTable: "StreamRoleSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AntiRaidSetting_GuildConfigId",
                table: "AntiRaidSetting",
                column: "GuildConfigId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AntiSpamIgnore_AntiSpamSettingId",
                table: "AntiSpamIgnore",
                column: "AntiSpamSettingId");

            migrationBuilder.CreateIndex(
                name: "IX_AntiSpamSetting_GuildConfigId",
                table: "AntiSpamSetting",
                column: "GuildConfigId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BirthDates_UserId",
                table: "BirthDates",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistItem_BotConfigId",
                table: "BlacklistItem",
                column: "BotConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockedCmdOrMdl_BotConfigId",
                table: "BlockedCmdOrMdl",
                column: "BotConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockedCmdOrMdl_BotConfigId1",
                table: "BlockedCmdOrMdl",
                column: "BotConfigId1");

            migrationBuilder.CreateIndex(
                name: "IX_CommandAlias_GuildConfigId",
                table: "CommandAlias",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandCooldown_GuildConfigId",
                table: "CommandCooldown",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandPrice_BotConfigId",
                table: "CommandPrice",
                column: "BotConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandPrice_Price",
                table: "CommandPrice",
                column: "Price",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Currency_UserId",
                table: "Currency",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyMoney_UserId",
                table: "DailyMoney",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Donators_UserId",
                table: "Donators",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EightBallResponse_BotConfigId",
                table: "EightBallResponse",
                column: "BotConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_FilterChannelId_GuildConfigId",
                table: "FilterChannelId",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_FilterChannelId_GuildConfigId1",
                table: "FilterChannelId",
                column: "GuildConfigId1");

            migrationBuilder.CreateIndex(
                name: "IX_FilteredWord_GuildConfigId",
                table: "FilteredWord",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_FollowedStream_GuildConfigId",
                table: "FollowedStream",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_GCChannelId_GuildConfigId",
                table: "GCChannelId",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildConfigs_GuildId",
                table: "GuildConfigs",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildConfigs_LogSettingId",
                table: "GuildConfigs",
                column: "LogSettingId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildConfigs_RootPermissionId",
                table: "GuildConfigs",
                column: "RootPermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildRepeater_GuildConfigId",
                table: "GuildRepeater",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_IgnoredLogChannel_LogSettingId",
                table: "IgnoredLogChannel",
                column: "LogSettingId");

            migrationBuilder.CreateIndex(
                name: "IX_IgnoredVoicePresenceChannel_LogSettingId",
                table: "IgnoredVoicePresenceChannel",
                column: "LogSettingId");

            migrationBuilder.CreateIndex(
                name: "IX_LevelModel_GuildId_UserId",
                table: "LevelModel",
                columns: new[] { "GuildId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageXpRestrictions_GuildId_ChannelId",
                table: "MessageXpRestrictions",
                columns: new[] { "GuildId", "ChannelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModulePrefix_BotConfigId",
                table: "ModulePrefix",
                column: "BotConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_MutedUserId_GuildConfigId",
                table: "MutedUserId",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_NsfwBlacklitedTag_GuildConfigId",
                table: "NsfwBlacklitedTag",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_Permission_NextId",
                table: "Permission",
                column: "NextId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissionv2_GuildConfigId",
                table: "Permissionv2",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayingStatus_BotConfigId",
                table: "PlayingStatus",
                column: "BotConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_RaceAnimal_BotConfigId",
                table: "RaceAnimal",
                column: "BotConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardedUser_UserId",
                table: "RewardedUser",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleLevelBinding_RoleId",
                table: "RoleLevelBinding",
                column: "RoleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleMoney_RoleId",
                table: "RoleMoney",
                column: "RoleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SelfAssignableRoles_GuildId_RoleId",
                table: "SelfAssignableRoles",
                columns: new[] { "GuildId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopEntry_GuildConfigId",
                table: "ShopEntry",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopEntryItem_ShopEntryId",
                table: "ShopEntryItem",
                column: "ShopEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_SlowmodeIgnoredRole_GuildConfigId",
                table: "SlowmodeIgnoredRole",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_SlowmodeIgnoredUser_GuildConfigId",
                table: "SlowmodeIgnoredUser",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_StartupCommand_BotConfigId",
                table: "StartupCommand",
                column: "BotConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_StreamRoleBlacklistedUser_StreamRoleSettingsId",
                table: "StreamRoleBlacklistedUser",
                column: "StreamRoleSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_StreamRoleSettings_GuildConfigId",
                table: "StreamRoleSettings",
                column: "GuildConfigId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StreamRoleWhitelistedUser_StreamRoleSettingsId",
                table: "StreamRoleWhitelistedUser",
                column: "StreamRoleSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamUpdateRank_GuildId_Rankname",
                table: "TeamUpdateRank",
                columns: new[] { "GuildId", "Rankname" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnmuteTimer_GuildConfigId",
                table: "UnmuteTimer",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_VcRoleInfo_GuildConfigId",
                table: "VcRoleInfo",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_VerifiedUsers_GuildId_UserId",
                table: "VerifiedUsers",
                columns: new[] { "GuildId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoiceChannelStats_UserId_GuildId",
                table: "VoiceChannelStats",
                columns: new[] { "UserId", "GuildId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarningPunishment_GuildConfigId",
                table: "WarningPunishment",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_ZalgoFilterChannel_GuildConfigId",
                table: "ZalgoFilterChannel",
                column: "GuildConfigId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AntiRaidSetting");

            migrationBuilder.DropTable(
                name: "AntiSpamIgnore");

            migrationBuilder.DropTable(
                name: "BirthDates");

            migrationBuilder.DropTable(
                name: "BlacklistItem");

            migrationBuilder.DropTable(
                name: "BlockedCmdOrMdl");

            migrationBuilder.DropTable(
                name: "CommandAlias");

            migrationBuilder.DropTable(
                name: "CommandCooldown");

            migrationBuilder.DropTable(
                name: "CommandPrice");

            migrationBuilder.DropTable(
                name: "Currency");

            migrationBuilder.DropTable(
                name: "CurrencyTransactions");

            migrationBuilder.DropTable(
                name: "CustomReactions");

            migrationBuilder.DropTable(
                name: "DailyMoney");

            migrationBuilder.DropTable(
                name: "DailyMoneyStats");

            migrationBuilder.DropTable(
                name: "Donators");

            migrationBuilder.DropTable(
                name: "EightBallResponse");

            migrationBuilder.DropTable(
                name: "FilterChannelId");

            migrationBuilder.DropTable(
                name: "FilteredWord");

            migrationBuilder.DropTable(
                name: "FollowedStream");

            migrationBuilder.DropTable(
                name: "GCChannelId");

            migrationBuilder.DropTable(
                name: "GuildRepeater");

            migrationBuilder.DropTable(
                name: "IgnoredLogChannel");

            migrationBuilder.DropTable(
                name: "IgnoredVoicePresenceChannel");

            migrationBuilder.DropTable(
                name: "LevelModel");

            migrationBuilder.DropTable(
                name: "MessageXpRestrictions");

            migrationBuilder.DropTable(
                name: "ModulePrefix");

            migrationBuilder.DropTable(
                name: "MutedUserId");

            migrationBuilder.DropTable(
                name: "NsfwBlacklitedTag");

            migrationBuilder.DropTable(
                name: "Permissionv2");

            migrationBuilder.DropTable(
                name: "PlayingStatus");

            migrationBuilder.DropTable(
                name: "Quotes");

            migrationBuilder.DropTable(
                name: "RaceAnimal");

            migrationBuilder.DropTable(
                name: "Reminders");

            migrationBuilder.DropTable(
                name: "RewardedUser");

            migrationBuilder.DropTable(
                name: "RoleLevelBinding");

            migrationBuilder.DropTable(
                name: "RoleMoney");

            migrationBuilder.DropTable(
                name: "SelfAssignableRoles");

            migrationBuilder.DropTable(
                name: "ShopEntryItem");

            migrationBuilder.DropTable(
                name: "SlowmodeIgnoredRole");

            migrationBuilder.DropTable(
                name: "SlowmodeIgnoredUser");

            migrationBuilder.DropTable(
                name: "StartupCommand");

            migrationBuilder.DropTable(
                name: "StreamRoleBlacklistedUser");

            migrationBuilder.DropTable(
                name: "StreamRoleWhitelistedUser");

            migrationBuilder.DropTable(
                name: "TeamUpdateRank");

            migrationBuilder.DropTable(
                name: "UnmuteTimer");

            migrationBuilder.DropTable(
                name: "UsernameHistory");

            migrationBuilder.DropTable(
                name: "VcRoleInfo");

            migrationBuilder.DropTable(
                name: "VerifiedUsers");

            migrationBuilder.DropTable(
                name: "VoiceChannelStats");

            migrationBuilder.DropTable(
                name: "WarningPunishment");

            migrationBuilder.DropTable(
                name: "Warnings");

            migrationBuilder.DropTable(
                name: "ZalgoFilterChannel");

            migrationBuilder.DropTable(
                name: "AntiSpamSetting");

            migrationBuilder.DropTable(
                name: "ShopEntry");

            migrationBuilder.DropTable(
                name: "BotConfig");

            migrationBuilder.DropTable(
                name: "StreamRoleSettings");

            migrationBuilder.DropTable(
                name: "GuildConfigs");

            migrationBuilder.DropTable(
                name: "LogSetting");

            migrationBuilder.DropTable(
                name: "Permission");
        }
    }
}
