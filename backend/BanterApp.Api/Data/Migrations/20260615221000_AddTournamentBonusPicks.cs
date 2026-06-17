using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanterApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentBonusPicks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tournament_award_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    AnswerValue = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AnswerDisplay = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    AnnouncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_award_results", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tournament_bonus_picks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnonymousUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    PickValue = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PointsAwarded = table.Column<int>(type: "integer", nullable: false),
                    LockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_bonus_picks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tournament_bonus_picks_anonymous_users_AnonymousUserId",
                        column: x => x.AnonymousUserId,
                        principalTable: "anonymous_users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tournament_bonus_picks_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_tournament_award_results_Category",
                table: "tournament_award_results",
                column: "Category",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tournament_bonus_picks_AnonymousUserId_Category",
                table: "tournament_bonus_picks",
                columns: new[] { "AnonymousUserId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_tournament_bonus_picks_UserId_Category",
                table: "tournament_bonus_picks",
                columns: new[] { "UserId", "Category" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tournament_award_results");

            migrationBuilder.DropTable(
                name: "tournament_bonus_picks");
        }
    }
}
