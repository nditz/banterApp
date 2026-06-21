using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanterApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SecurityHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountStatus",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EmailConfirmedAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "auth_audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    Details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "provider_usage_daily",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UsageDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RequestCount = table.Column<int>(type: "integer", nullable: false),
                    FailureCount = table.Column<int>(type: "integer", nullable: false),
                    EstimatedUnits = table.Column<int>(type: "integer", nullable: false),
                    TotalLatencyMs = table.Column<long>(type: "bigint", nullable: false),
                    LatencySamples = table.Column<int>(type: "integer", nullable: false),
                    AverageLatencyMs = table.Column<double>(type: "double precision", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_usage_daily", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_AccountStatus",
                table: "users",
                column: "AccountStatus");

            migrationBuilder.CreateIndex(
                name: "IX_auth_audit_logs_Email",
                table: "auth_audit_logs",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_auth_audit_logs_EventType",
                table: "auth_audit_logs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_auth_audit_logs_OccurredAt",
                table: "auth_audit_logs",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_provider_usage_daily_Provider_UsageDate",
                table: "provider_usage_daily",
                columns: new[] { "Provider", "UsageDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auth_audit_logs");

            migrationBuilder.DropTable(
                name: "provider_usage_daily");

            migrationBuilder.DropIndex(
                name: "IX_users_AccountStatus",
                table: "users");

            migrationBuilder.DropColumn(
                name: "AccountStatus",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmailConfirmedAt",
                table: "users");
        }
    }
}
