using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanterApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFootballReferenceData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "countries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ExternalProvider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    FlagUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Continent = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    FifaRanking = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_countries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ExternalProvider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CountryId = table.Column<Guid>(type: "uuid", nullable: true),
                    FirstName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    LastName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    KnownName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Age = table.Column<int>(type: "integer", nullable: true),
                    Position = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    PhotoUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ClubName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    NationalTeamName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_players_countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "countries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "leaderboard_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaderboardType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CountryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Rank = table.Column<int>(type: "integer", nullable: true),
                    Value = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Competition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Season = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    SourceProvider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    SourceUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leaderboard_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_leaderboard_entries_countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "countries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_leaderboard_entries_players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "player_stats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CountryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Competition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Season = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    MatchesPlayed = table.Column<int>(type: "integer", nullable: false),
                    Goals = table.Column<int>(type: "integer", nullable: false),
                    Assists = table.Column<int>(type: "integer", nullable: false),
                    YellowCards = table.Column<int>(type: "integer", nullable: false),
                    RedCards = table.Column<int>(type: "integer", nullable: false),
                    MinutesPlayed = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    SourceProvider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    SourceUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_stats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_player_stats_countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "countries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_player_stats_players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_predictions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PredictionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CountryId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Competition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Season = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    PredictionValue = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Confidence = table.Column<int>(type: "integer", nullable: true),
                    LockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_predictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_predictions_countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "countries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_user_predictions_players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "players",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_user_predictions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_countries_Code",
                table: "countries",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_countries_ExternalProvider_ExternalId",
                table: "countries",
                columns: new[] { "ExternalProvider", "ExternalId" },
                unique: true,
                filter: "\"ExternalProvider\" IS NOT NULL AND \"ExternalId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_countries_IsActive",
                table: "countries",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_leaderboard_entries_CountryId",
                table: "leaderboard_entries",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_leaderboard_entries_LeaderboardType_Competition_Season_Rank",
                table: "leaderboard_entries",
                columns: new[] { "LeaderboardType", "Competition", "Season", "Rank" });

            migrationBuilder.CreateIndex(
                name: "IX_leaderboard_entries_LeaderboardType_PlayerId_Competition_Se~",
                table: "leaderboard_entries",
                columns: new[] { "LeaderboardType", "PlayerId", "Competition", "Season", "SourceProvider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_leaderboard_entries_PlayerId",
                table: "leaderboard_entries",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_player_stats_CountryId",
                table: "player_stats",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_player_stats_PlayerId_CountryId_Competition_Season_SourcePr~",
                table: "player_stats",
                columns: new[] { "PlayerId", "CountryId", "Competition", "Season", "SourceProvider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_players_CountryId",
                table: "players",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_players_DisplayName",
                table: "players",
                column: "DisplayName");

            migrationBuilder.CreateIndex(
                name: "IX_players_ExternalProvider_ExternalId",
                table: "players",
                columns: new[] { "ExternalProvider", "ExternalId" },
                unique: true,
                filter: "\"ExternalProvider\" IS NOT NULL AND \"ExternalId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_players_IsActive",
                table: "players",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_players_Position",
                table: "players",
                column: "Position");

            migrationBuilder.CreateIndex(
                name: "IX_user_predictions_CountryId",
                table: "user_predictions",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_user_predictions_PlayerId",
                table: "user_predictions",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_user_predictions_PredictionType",
                table: "user_predictions",
                column: "PredictionType");

            migrationBuilder.CreateIndex(
                name: "IX_user_predictions_UserId_PredictionType_Competition_Season",
                table: "user_predictions",
                columns: new[] { "UserId", "PredictionType", "Competition", "Season" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "leaderboard_entries");

            migrationBuilder.DropTable(
                name: "player_stats");

            migrationBuilder.DropTable(
                name: "user_predictions");

            migrationBuilder.DropTable(
                name: "players");

            migrationBuilder.DropTable(
                name: "countries");
        }
    }
}
