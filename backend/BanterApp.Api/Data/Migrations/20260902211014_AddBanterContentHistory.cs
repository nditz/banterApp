using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanterApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBanterContentHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "banter_content_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    MatchId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TeamId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PredictionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScenarioType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProviderContentId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SearchPhrase = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    MemeTemplateId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CaptionHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SelectionScore = table.Column<decimal>(type: "numeric", nullable: true),
                    UsedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_banter_content_history", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_banter_content_history_MemeTemplateId_UsedAtUtc",
                table: "banter_content_history",
                columns: new[] { "MemeTemplateId", "UsedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_banter_content_history_Provider_ProviderContentId",
                table: "banter_content_history",
                columns: new[] { "Provider", "ProviderContentId" });

            migrationBuilder.CreateIndex(
                name: "IX_banter_content_history_ScenarioType_UsedAtUtc",
                table: "banter_content_history",
                columns: new[] { "ScenarioType", "UsedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_banter_content_history_TeamId_UsedAtUtc",
                table: "banter_content_history",
                columns: new[] { "TeamId", "UsedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_banter_content_history_UserId_UsedAtUtc",
                table: "banter_content_history",
                columns: new[] { "UserId", "UsedAtUtc" });

            migrationBuilder.Sql("ALTER TABLE public.banter_content_history ENABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "banter_content_history");
        }
    }
}
