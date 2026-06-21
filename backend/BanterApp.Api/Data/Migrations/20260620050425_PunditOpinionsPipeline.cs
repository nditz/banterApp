using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanterApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PunditOpinionsPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "pundits",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "pundits",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "pundits",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "pundits",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "pundits",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfigJson",
                table: "media_sources",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "media_sources",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Author",
                table: "media_items",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "media_items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProcessedAt",
                table: "media_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingError",
                table: "media_items",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingStatus",
                table: "media_items",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Publication",
                table: "media_items",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawPayloadJson",
                table: "media_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawSummary",
                table: "media_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawText",
                table: "media_items",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "prediction_aggregates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EntityName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PredictionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ConsensusSummary = table.Column<string>(type: "text", nullable: true),
                    PositiveCount = table.Column<int>(type: "integer", nullable: false),
                    NegativeCount = table.Column<int>(type: "integer", nullable: false),
                    NeutralCount = table.Column<int>(type: "integer", nullable: false),
                    SourceCount = table.Column<int>(type: "integer", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "double precision", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prediction_aggregates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pundit_opinions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    PunditId = table.Column<Guid>(type: "uuid", nullable: false),
                    Topic = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Team = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Player = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    MatchName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Opinion = table.Column<string>(type: "text", nullable: false),
                    Prediction = table.Column<string>(type: "text", nullable: true),
                    PredictionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Confidence = table.Column<double>(type: "double precision", nullable: true),
                    EvidenceQuote = table.Column<string>(type: "text", nullable: true),
                    QuoteContext = table.Column<string>(type: "text", nullable: true),
                    IsDirectQuote = table.Column<bool>(type: "boolean", nullable: false),
                    NeedsHumanReview = table.Column<bool>(type: "boolean", nullable: false),
                    ExtractedJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pundit_opinions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pundit_opinions_media_items_SourceItemId",
                        column: x => x.SourceItemId,
                        principalTable: "media_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pundit_opinions_pundits_PunditId",
                        column: x => x.PunditId,
                        principalTable: "pundits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pundits_Kind",
                table: "pundits",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_pundits_NormalizedName",
                table: "pundits",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "IX_media_items_ContentHash",
                table: "media_items",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_media_items_ProcessingStatus_ProcessedAt",
                table: "media_items",
                columns: new[] { "ProcessingStatus", "ProcessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_prediction_aggregates_EntityType_EntityName_PredictionType",
                table: "prediction_aggregates",
                columns: new[] { "EntityType", "EntityName", "PredictionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pundit_opinions_NeedsHumanReview",
                table: "pundit_opinions",
                column: "NeedsHumanReview");

            migrationBuilder.CreateIndex(
                name: "IX_pundit_opinions_PunditId",
                table: "pundit_opinions",
                column: "PunditId");

            migrationBuilder.CreateIndex(
                name: "IX_pundit_opinions_SourceItemId",
                table: "pundit_opinions",
                column: "SourceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_pundit_opinions_Team",
                table: "pundit_opinions",
                column: "Team");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "prediction_aggregates");

            migrationBuilder.DropTable(
                name: "pundit_opinions");

            migrationBuilder.DropIndex(
                name: "IX_pundits_Kind",
                table: "pundits");

            migrationBuilder.DropIndex(
                name: "IX_pundits_NormalizedName",
                table: "pundits");

            migrationBuilder.DropIndex(
                name: "IX_media_items_ContentHash",
                table: "media_items");

            migrationBuilder.DropIndex(
                name: "IX_media_items_ProcessingStatus_ProcessedAt",
                table: "media_items");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "pundits");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "pundits");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "pundits");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "pundits");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "pundits");

            migrationBuilder.DropColumn(
                name: "ConfigJson",
                table: "media_sources");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "media_sources");

            migrationBuilder.DropColumn(
                name: "Author",
                table: "media_items");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "media_items");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "media_items");

            migrationBuilder.DropColumn(
                name: "ProcessingError",
                table: "media_items");

            migrationBuilder.DropColumn(
                name: "ProcessingStatus",
                table: "media_items");

            migrationBuilder.DropColumn(
                name: "Publication",
                table: "media_items");

            migrationBuilder.DropColumn(
                name: "RawPayloadJson",
                table: "media_items");

            migrationBuilder.DropColumn(
                name: "RawSummary",
                table: "media_items");

            migrationBuilder.DropColumn(
                name: "RawText",
                table: "media_items");
        }
    }
}
