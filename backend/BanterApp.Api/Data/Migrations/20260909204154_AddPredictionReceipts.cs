using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanterApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPredictionReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "prediction_receipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PredictionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnonymousUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResultHash = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PredictionType = table.Column<int>(type: "integer", nullable: false),
                    PredictionValue = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PointsAwarded = table.Column<int>(type: "integer", nullable: false),
                    AuraDelta = table.Column<int>(type: "integer", nullable: false),
                    HomeScore = table.Column<int>(type: "integer", nullable: true),
                    AwayScore = table.Column<int>(type: "integer", nullable: true),
                    MatchStatus = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    StoryType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StoryTypesJson = table.Column<string>(type: "text", nullable: false),
                    PunditTakesJson = table.Column<string>(type: "text", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    SettledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prediction_receipts", x => x.Id);
                    table.CheckConstraint("CK_prediction_receipts_owner", "(\"UserId\" IS NOT NULL AND \"AnonymousUserId\" IS NULL) OR (\"UserId\" IS NULL AND \"AnonymousUserId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_prediction_receipts_anonymous_users_AnonymousUserId",
                        column: x => x.AnonymousUserId,
                        principalTable: "anonymous_users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_prediction_receipts_matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_prediction_receipts_predictions_PredictionId",
                        column: x => x.PredictionId,
                        principalTable: "predictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_prediction_receipts_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "receipt_story_candidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoryType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    Summary = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receipt_story_candidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_receipt_story_candidates_prediction_receipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "prediction_receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_prediction_receipts_AnonymousUserId",
                table: "prediction_receipts",
                column: "AnonymousUserId");

            migrationBuilder.CreateIndex(
                name: "IX_prediction_receipts_MatchId",
                table: "prediction_receipts",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_prediction_receipts_PredictionId_ResultHash",
                table: "prediction_receipts",
                columns: new[] { "PredictionId", "ResultHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prediction_receipts_SettledAt",
                table: "prediction_receipts",
                column: "SettledAt");

            migrationBuilder.CreateIndex(
                name: "IX_prediction_receipts_UserId",
                table: "prediction_receipts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_receipt_story_candidates_ReceiptId_Rank",
                table: "receipt_story_candidates",
                columns: new[] { "ReceiptId", "Rank" });

            migrationBuilder.Sql("ALTER TABLE public.prediction_receipts ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE public.receipt_story_candidates ENABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "receipt_story_candidates");

            migrationBuilder.DropTable(
                name: "prediction_receipts");
        }
    }
}
