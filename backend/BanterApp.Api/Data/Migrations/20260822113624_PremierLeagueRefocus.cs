using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanterApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PremierLeagueRefocus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bracket_picks");

            migrationBuilder.Sql("""
                DELETE FROM predictions;
                DELETE FROM match_events;
                DELETE FROM lineup_players;
                DELETE FROM pundit_predictions;
                DELETE FROM pundit_opinions;
                DELETE FROM tournament_bonus_picks;
                DELETE FROM tournament_award_results;
                DELETE FROM user_predictions;
                DELETE FROM standing_rows;
                DELETE FROM news_feed_items;
                DELETE FROM matches;
                """);

            migrationBuilder.DropIndex(
                name: "IX_tournament_bonus_picks_AnonymousUserId_Category",
                table: "tournament_bonus_picks");

            migrationBuilder.DropIndex(
                name: "IX_tournament_bonus_picks_UserId_Category",
                table: "tournament_bonus_picks");

            migrationBuilder.AddColumn<int>(
                name: "SlotIndex",
                table: "tournament_bonus_picks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "CompetitionSeasonId",
                table: "standing_rows",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "standing_rows",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AwayLogoUrl",
                table: "matches",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompetitionSeasonId",
                table: "matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeLogoUrl",
                table: "matches",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MatchweekId",
                table: "matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MatchweekNumber",
                table: "matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PredictionLockAtUtc",
                table: "matches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "club_teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    LogoUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ProviderTeamId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_club_teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "competitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    LogoUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProviderCompetitionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsAvailableForPrediction = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "matchweek_bonuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnonymousUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompetitionSeasonId = table.Column<Guid>(type: "uuid", nullable: true),
                    MatchweekNumber = table.Column<int>(type: "integer", nullable: false),
                    PointsAwarded = table.Column<int>(type: "integer", nullable: false),
                    AwardedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matchweek_bonuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_matchweek_bonuses_anonymous_users_AnonymousUserId",
                        column: x => x.AnonymousUserId,
                        principalTable: "anonymous_users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_matchweek_bonuses_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "competition_seasons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartYear = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProviderSeasonId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competition_seasons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_competition_seasons_competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "matchweeks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetitionSeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matchweeks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_matchweeks_competition_seasons_CompetitionSeasonId",
                        column: x => x.CompetitionSeasonId,
                        principalTable: "competition_seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "season_teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetitionSeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_season_teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_season_teams_club_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "club_teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_season_teams_competition_seasons_CompetitionSeasonId",
                        column: x => x.CompetitionSeasonId,
                        principalTable: "competition_seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tournament_bonus_picks_AnonymousUserId_Category_SlotIndex",
                table: "tournament_bonus_picks",
                columns: new[] { "AnonymousUserId", "Category", "SlotIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_tournament_bonus_picks_UserId_Category_SlotIndex",
                table: "tournament_bonus_picks",
                columns: new[] { "UserId", "Category", "SlotIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_standing_rows_CompetitionSeasonId",
                table: "standing_rows",
                column: "CompetitionSeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_matches_CompetitionSeasonId",
                table: "matches",
                column: "CompetitionSeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_matches_MatchweekId",
                table: "matches",
                column: "MatchweekId");

            migrationBuilder.CreateIndex(
                name: "IX_matches_MatchweekNumber",
                table: "matches",
                column: "MatchweekNumber");

            migrationBuilder.CreateIndex(
                name: "IX_club_teams_Code",
                table: "club_teams",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_club_teams_Provider_ProviderTeamId",
                table: "club_teams",
                columns: new[] { "Provider", "ProviderTeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_club_teams_Slug",
                table: "club_teams",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_competition_seasons_CompetitionId_StartYear",
                table: "competition_seasons",
                columns: new[] { "CompetitionId", "StartYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_competition_seasons_IsCurrent",
                table: "competition_seasons",
                column: "IsCurrent");

            migrationBuilder.CreateIndex(
                name: "IX_competitions_Code",
                table: "competitions",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_competitions_Slug",
                table: "competitions",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_matchweek_bonuses_AnonymousUserId_CompetitionSeasonId_Match~",
                table: "matchweek_bonuses",
                columns: new[] { "AnonymousUserId", "CompetitionSeasonId", "MatchweekNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_matchweek_bonuses_UserId_CompetitionSeasonId_MatchweekNumber",
                table: "matchweek_bonuses",
                columns: new[] { "UserId", "CompetitionSeasonId", "MatchweekNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_matchweeks_CompetitionSeasonId_Number",
                table: "matchweeks",
                columns: new[] { "CompetitionSeasonId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_season_teams_CompetitionSeasonId_TeamId",
                table: "season_teams",
                columns: new[] { "CompetitionSeasonId", "TeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_season_teams_TeamId",
                table: "season_teams",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_matches_competition_seasons_CompetitionSeasonId",
                table: "matches",
                column: "CompetitionSeasonId",
                principalTable: "competition_seasons",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_matches_matchweeks_MatchweekId",
                table: "matches",
                column: "MatchweekId",
                principalTable: "matchweeks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_standing_rows_competition_seasons_CompetitionSeasonId",
                table: "standing_rows",
                column: "CompetitionSeasonId",
                principalTable: "competition_seasons",
                principalColumn: "Id");

            migrationBuilder.Sql("""
                INSERT INTO competitions ("Id", "Name", "Slug", "Code", "CountryCode", "Provider", "ProviderCompetitionId", "IsActive", "IsAvailableForPrediction", "DisplayOrder", "CreatedAt", "UpdatedAt")
                VALUES ('00000000-0000-0000-0000-000000000039', 'Premier League', 'premier-league', 'PL', 'GB', 'api_football', '39', TRUE, TRUE, 1, NOW(), NOW())
                ON CONFLICT ("Id") DO NOTHING;

                INSERT INTO competition_seasons ("Id", "CompetitionId", "Name", "StartYear", "ProviderSeasonId", "Status", "IsCurrent", "CreatedAt", "UpdatedAt")
                VALUES ('00000000-0000-0000-2026-000000000039', '00000000-0000-0000-0000-000000000039', '2026/27', 2026, '2026', 'current', TRUE, NOW(), NOW())
                ON CONFLICT ("Id") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_matches_competition_seasons_CompetitionSeasonId",
                table: "matches");

            migrationBuilder.DropForeignKey(
                name: "FK_matches_matchweeks_MatchweekId",
                table: "matches");

            migrationBuilder.DropForeignKey(
                name: "FK_standing_rows_competition_seasons_CompetitionSeasonId",
                table: "standing_rows");

            migrationBuilder.DropTable(
                name: "matchweek_bonuses");

            migrationBuilder.DropTable(
                name: "matchweeks");

            migrationBuilder.DropTable(
                name: "season_teams");

            migrationBuilder.DropTable(
                name: "club_teams");

            migrationBuilder.DropTable(
                name: "competition_seasons");

            migrationBuilder.DropTable(
                name: "competitions");

            migrationBuilder.DropIndex(
                name: "IX_tournament_bonus_picks_AnonymousUserId_Category_SlotIndex",
                table: "tournament_bonus_picks");

            migrationBuilder.DropIndex(
                name: "IX_tournament_bonus_picks_UserId_Category_SlotIndex",
                table: "tournament_bonus_picks");

            migrationBuilder.DropIndex(
                name: "IX_standing_rows_CompetitionSeasonId",
                table: "standing_rows");

            migrationBuilder.DropIndex(
                name: "IX_matches_CompetitionSeasonId",
                table: "matches");

            migrationBuilder.DropIndex(
                name: "IX_matches_MatchweekId",
                table: "matches");

            migrationBuilder.DropIndex(
                name: "IX_matches_MatchweekNumber",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "SlotIndex",
                table: "tournament_bonus_picks");

            migrationBuilder.DropColumn(
                name: "CompetitionSeasonId",
                table: "standing_rows");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "standing_rows");

            migrationBuilder.DropColumn(
                name: "AwayLogoUrl",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "CompetitionSeasonId",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "HomeLogoUrl",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "MatchweekId",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "MatchweekNumber",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "PredictionLockAtUtc",
                table: "matches");

            migrationBuilder.CreateTable(
                name: "bracket_picks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnonymousUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    MatchId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SlotId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    WinnerTeamCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bracket_picks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bracket_picks_anonymous_users_AnonymousUserId",
                        column: x => x.AnonymousUserId,
                        principalTable: "anonymous_users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_bracket_picks_matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bracket_picks_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_tournament_bonus_picks_AnonymousUserId_Category",
                table: "tournament_bonus_picks",
                columns: new[] { "AnonymousUserId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_tournament_bonus_picks_UserId_Category",
                table: "tournament_bonus_picks",
                columns: new[] { "UserId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_bracket_picks_AnonymousUserId_SlotId",
                table: "bracket_picks",
                columns: new[] { "AnonymousUserId", "SlotId" });

            migrationBuilder.CreateIndex(
                name: "IX_bracket_picks_MatchId",
                table: "bracket_picks",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_bracket_picks_UserId_SlotId",
                table: "bracket_picks",
                columns: new[] { "UserId", "SlotId" });
        }
    }
}
