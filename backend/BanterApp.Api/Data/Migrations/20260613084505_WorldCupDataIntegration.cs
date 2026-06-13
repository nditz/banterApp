using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanterApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class WorldCupDataIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pundit_predictions_matches_MatchId",
                table: "pundit_predictions");

            migrationBuilder.AlterColumn<string>(
                name: "MatchId",
                table: "pundit_predictions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)");

            migrationBuilder.AddColumn<string>(
                name: "Author",
                table: "pundit_predictions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Confidence",
                table: "pundit_predictions",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvidenceSnippet",
                table: "pundit_predictions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMatched",
                table: "pundit_predictions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PredictedScore",
                table: "pundit_predictions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PredictedTeam",
                table: "pundit_predictions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PredictionType",
                table: "pundit_predictions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "pundit_predictions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "pundit_predictions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Speaker",
                table: "pundit_predictions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "external_ids",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProviderExternalId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RawPayloadHash = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_ids", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "lineup_players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TeamCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    ShirtNumber = table.Column<int>(type: "integer", nullable: true),
                    PlayerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Position = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    IsSubstitute = table.Column<bool>(type: "boolean", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lineup_players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lineup_players_matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "match_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Minute = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TeamCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    PlayerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Detail = table.Column<string>(type: "text", nullable: true),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProviderEventId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_match_events_matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "media_sources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RssUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SiteUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CrawlAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    ExtractPredictions = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_sources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "standing_rows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupKey = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    TeamCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    TeamName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Played = table.Column<int>(type: "integer", nullable: false),
                    Won = table.Column<int>(type: "integer", nullable: false),
                    Drawn = table.Column<int>(type: "integer", nullable: false),
                    Lost = table.Column<int>(type: "integer", nullable: false),
                    GoalsFor = table.Column<int>(type: "integer", nullable: false),
                    GoalsAgainst = table.Column<int>(type: "integer", nullable: false),
                    GoalDiff = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_standing_rows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sync_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    JobName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    RecordsCreated = table.Column<int>(type: "integer", nullable: false),
                    RecordsUpdated = table.Column<int>(type: "integer", nullable: false),
                    RecordsFailed = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "media_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SourceUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    AudioUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TranscriptSnippet = table.Column<string>(type: "text", nullable: true),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_media_items_media_sources_MediaSourceId",
                        column: x => x.MediaSourceId,
                        principalTable: "media_sources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sync_errors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SyncRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    JobName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Message = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_errors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sync_errors_sync_runs_SyncRunId",
                        column: x => x.SyncRunId,
                        principalTable: "sync_runs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_external_ids_EntityType_EntityId",
                table: "external_ids",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_external_ids_Provider_ProviderExternalId_EntityType",
                table: "external_ids",
                columns: new[] { "Provider", "ProviderExternalId", "EntityType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lineup_players_MatchId_TeamCode_PlayerName_IsSubstitute",
                table: "lineup_players",
                columns: new[] { "MatchId", "TeamCode", "PlayerName", "IsSubstitute" });

            migrationBuilder.CreateIndex(
                name: "IX_match_events_MatchId_ProviderEventId",
                table: "match_events",
                columns: new[] { "MatchId", "ProviderEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_media_items_MediaSourceId_ExternalId",
                table: "media_items",
                columns: new[] { "MediaSourceId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_media_sources_Name",
                table: "media_sources",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_standing_rows_GroupKey_TeamCode_Provider",
                table: "standing_rows",
                columns: new[] { "GroupKey", "TeamCode", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sync_errors_OccurredAt",
                table: "sync_errors",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_sync_errors_SyncRunId",
                table: "sync_errors",
                column: "SyncRunId");

            migrationBuilder.CreateIndex(
                name: "IX_sync_runs_Provider_JobName",
                table: "sync_runs",
                columns: new[] { "Provider", "JobName" });

            migrationBuilder.CreateIndex(
                name: "IX_sync_runs_StartedAt",
                table: "sync_runs",
                column: "StartedAt");

            migrationBuilder.AddForeignKey(
                name: "FK_pundit_predictions_matches_MatchId",
                table: "pundit_predictions",
                column: "MatchId",
                principalTable: "matches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pundit_predictions_matches_MatchId",
                table: "pundit_predictions");

            migrationBuilder.DropTable(
                name: "external_ids");

            migrationBuilder.DropTable(
                name: "lineup_players");

            migrationBuilder.DropTable(
                name: "match_events");

            migrationBuilder.DropTable(
                name: "media_items");

            migrationBuilder.DropTable(
                name: "standing_rows");

            migrationBuilder.DropTable(
                name: "sync_errors");

            migrationBuilder.DropTable(
                name: "media_sources");

            migrationBuilder.DropTable(
                name: "sync_runs");

            migrationBuilder.DropColumn(
                name: "Author",
                table: "pundit_predictions");

            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "pundit_predictions");

            migrationBuilder.DropColumn(
                name: "EvidenceSnippet",
                table: "pundit_predictions");

            migrationBuilder.DropColumn(
                name: "IsMatched",
                table: "pundit_predictions");

            migrationBuilder.DropColumn(
                name: "PredictedScore",
                table: "pundit_predictions");

            migrationBuilder.DropColumn(
                name: "PredictedTeam",
                table: "pundit_predictions");

            migrationBuilder.DropColumn(
                name: "PredictionType",
                table: "pundit_predictions");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "pundit_predictions");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "pundit_predictions");

            migrationBuilder.DropColumn(
                name: "Speaker",
                table: "pundit_predictions");

            migrationBuilder.AlterColumn<string>(
                name: "MatchId",
                table: "pundit_predictions",
                type: "character varying(64)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_pundit_predictions_matches_MatchId",
                table: "pundit_predictions",
                column: "MatchId",
                principalTable: "matches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
