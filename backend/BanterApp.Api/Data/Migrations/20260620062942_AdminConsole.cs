using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanterApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdminConsole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPlatformAdmin",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "DurationMs",
                table: "sync_runs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItemsProcessed",
                table: "sync_runs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ItemsSkipped",
                table: "sync_runs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "sync_runs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNotes",
                table: "pundit_opinions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewStatus",
                table: "pundit_opinions",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                table: "pundit_opinions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByUserId",
                table: "pundit_opinions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "admin_audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TargetType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TargetId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "app_metrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MetricKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    MetricValue = table.Column<double>(type: "double precision", nullable: false),
                    DimensionsJson = table.Column<string>(type: "text", nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_metrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ingestion_errors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    JobKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Severity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    StackTrace = table.Column<string>(type: "text", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SyncRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    MediaItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingestion_errors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "job_registry_state",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    Paused = table.Column<bool>(type: "boolean", nullable: false),
                    Schedule = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_registry_state", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_IsPlatformAdmin",
                table: "users",
                column: "IsPlatformAdmin");

            migrationBuilder.CreateIndex(
                name: "IX_pundit_opinions_ReviewStatus",
                table: "pundit_opinions",
                column: "ReviewStatus");

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_logs_AdminUserId",
                table: "admin_audit_logs",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_logs_CreatedAt",
                table: "admin_audit_logs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_app_metrics_MetricKey_RecordedAt",
                table: "app_metrics",
                columns: new[] { "MetricKey", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ingestion_errors_LastSeenAt",
                table: "ingestion_errors",
                column: "LastSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_ingestion_errors_Source_JobKey_Message",
                table: "ingestion_errors",
                columns: new[] { "Source", "JobKey", "Message" });

            migrationBuilder.CreateIndex(
                name: "IX_ingestion_errors_Status",
                table: "ingestion_errors",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_job_registry_state_JobKey",
                table: "job_registry_state",
                column: "JobKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_audit_logs");

            migrationBuilder.DropTable(
                name: "app_metrics");

            migrationBuilder.DropTable(
                name: "ingestion_errors");

            migrationBuilder.DropTable(
                name: "job_registry_state");

            migrationBuilder.DropIndex(
                name: "IX_users_IsPlatformAdmin",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_pundit_opinions_ReviewStatus",
                table: "pundit_opinions");

            migrationBuilder.DropColumn(
                name: "IsPlatformAdmin",
                table: "users");

            migrationBuilder.DropColumn(
                name: "DurationMs",
                table: "sync_runs");

            migrationBuilder.DropColumn(
                name: "ItemsProcessed",
                table: "sync_runs");

            migrationBuilder.DropColumn(
                name: "ItemsSkipped",
                table: "sync_runs");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "sync_runs");

            migrationBuilder.DropColumn(
                name: "ReviewNotes",
                table: "pundit_opinions");

            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "pundit_opinions");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "pundit_opinions");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "pundit_opinions");
        }
    }
}
