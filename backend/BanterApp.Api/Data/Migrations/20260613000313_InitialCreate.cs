using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanterApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "anonymous_users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecoveryCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CookieId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CountryCode = table.Column<string>(type: "text", nullable: true),
                    DeviceFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AiGenerationsUsed = table.Column<int>(type: "integer", nullable: false),
                    TermsAcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_anonymous_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "matches",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TeamA = table.Column<string>(type: "text", nullable: false),
                    TeamB = table.Column<string>(type: "text", nullable: false),
                    TeamACode = table.Column<string>(type: "text", nullable: false),
                    TeamBCode = table.Column<string>(type: "text", nullable: false),
                    KickoffTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Stage = table.Column<string>(type: "text", nullable: false),
                    Group = table.Column<string>(type: "text", nullable: false),
                    Venue = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    HomeScore = table.Column<int>(type: "integer", nullable: true),
                    AwayScore = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "news_feed_items",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Author = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
                    ParentItemId = table.Column<string>(type: "text", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_news_feed_items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pundits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Organization = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pundits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CountryCode = table.Column<string>(type: "text", nullable: true),
                    Avatar = table.Column<string>(type: "text", nullable: true),
                    IsAdultVerified = table.Column<bool>(type: "boolean", nullable: false),
                    TermsAcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pundit_predictions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PunditId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<string>(type: "character varying(64)", nullable: false),
                    Prediction = table.Column<string>(type: "text", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pundit_predictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pundit_predictions_matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pundit_predictions_pundits_PunditId",
                        column: x => x.PunditId,
                        principalTable: "pundits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bracket_picks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnonymousUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SlotId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MatchId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WinnerTeamCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "generated_content",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnonymousUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Prompt = table.Column<string>(type: "text", nullable: false),
                    Output = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generated_content", x => x.Id);
                    table.ForeignKey(
                        name: "FK_generated_content_anonymous_users_AnonymousUserId",
                        column: x => x.AnonymousUserId,
                        principalTable: "anonymous_users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_generated_content_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "leagues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    InviteCode = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByAnonymousUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaxMembers = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leagues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_leagues_anonymous_users_CreatedByAnonymousUserId",
                        column: x => x.CreatedByAnonymousUserId,
                        principalTable: "anonymous_users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_leagues_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "predictions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnonymousUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    MatchId = table.Column<string>(type: "character varying(64)", nullable: false),
                    PredictionType = table.Column<int>(type: "integer", nullable: false),
                    PredictionValue = table.Column<string>(type: "text", nullable: false),
                    PointsAwarded = table.Column<int>(type: "integer", nullable: false),
                    LockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_predictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_predictions_anonymous_users_AnonymousUserId",
                        column: x => x.AnonymousUserId,
                        principalTable: "anonymous_users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_predictions_matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_predictions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "league_members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeagueId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnonymousUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_league_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_league_members_anonymous_users_AnonymousUserId",
                        column: x => x.AnonymousUserId,
                        principalTable: "anonymous_users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_league_members_leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_league_members_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_anonymous_users_CookieId",
                table: "anonymous_users",
                column: "CookieId",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_generated_content_AnonymousUserId",
                table: "generated_content",
                column: "AnonymousUserId");

            migrationBuilder.CreateIndex(
                name: "IX_generated_content_UserId",
                table: "generated_content",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_league_members_AnonymousUserId",
                table: "league_members",
                column: "AnonymousUserId");

            migrationBuilder.CreateIndex(
                name: "IX_league_members_LeagueId_AnonymousUserId",
                table: "league_members",
                columns: new[] { "LeagueId", "AnonymousUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_league_members_LeagueId_UserId",
                table: "league_members",
                columns: new[] { "LeagueId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_league_members_UserId",
                table: "league_members",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_leagues_CreatedByAnonymousUserId",
                table: "leagues",
                column: "CreatedByAnonymousUserId");

            migrationBuilder.CreateIndex(
                name: "IX_leagues_CreatedByUserId",
                table: "leagues",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_leagues_InviteCode",
                table: "leagues",
                column: "InviteCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_leagues_Kind_CountryCode",
                table: "leagues",
                columns: new[] { "Kind", "CountryCode" });

            migrationBuilder.CreateIndex(
                name: "IX_predictions_AnonymousUserId_MatchId_PredictionType",
                table: "predictions",
                columns: new[] { "AnonymousUserId", "MatchId", "PredictionType" });

            migrationBuilder.CreateIndex(
                name: "IX_predictions_MatchId",
                table: "predictions",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_predictions_UserId_MatchId_PredictionType",
                table: "predictions",
                columns: new[] { "UserId", "MatchId", "PredictionType" });

            migrationBuilder.CreateIndex(
                name: "IX_pundit_predictions_MatchId",
                table: "pundit_predictions",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_pundit_predictions_PunditId",
                table: "pundit_predictions",
                column: "PunditId");

            migrationBuilder.CreateIndex(
                name: "IX_matches_KickoffTime",
                table: "matches",
                column: "KickoffTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bracket_picks");

            migrationBuilder.DropTable(
                name: "generated_content");

            migrationBuilder.DropTable(
                name: "league_members");

            migrationBuilder.DropTable(
                name: "news_feed_items");

            migrationBuilder.DropTable(
                name: "predictions");

            migrationBuilder.DropTable(
                name: "pundit_predictions");

            migrationBuilder.DropTable(
                name: "leagues");

            migrationBuilder.DropTable(
                name: "matches");

            migrationBuilder.DropTable(
                name: "pundits");

            migrationBuilder.DropTable(
                name: "anonymous_users");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
