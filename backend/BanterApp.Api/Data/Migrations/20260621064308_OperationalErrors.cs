using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanterApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class OperationalErrors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "errors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequestId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Environment = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Severity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ErrorCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ErrorType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    MessageSafe = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    MessageInternal = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    StackTrace = table.Column<string>(type: "text", nullable: true),
                    Route = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    StatusCode = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    JobKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    JobRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ProviderRequestId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OccurrenceCount = table.Column<int>(type: "integer", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_errors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_errors_Fingerprint",
                table: "errors",
                column: "Fingerprint");

            migrationBuilder.CreateIndex(
                name: "IX_errors_Fingerprint_Status",
                table: "errors",
                columns: new[] { "Fingerprint", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_errors_LastSeenAt",
                table: "errors",
                column: "LastSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_errors_RequestId",
                table: "errors",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_errors_Severity_Status",
                table: "errors",
                columns: new[] { "Severity", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_errors_Source_Provider",
                table: "errors",
                columns: new[] { "Source", "Provider" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "errors");
        }
    }
}
