using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanterApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConsentAndAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "analytics_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Feature = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnonymousSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PropertiesJson = table.Column<string>(type: "text", nullable: true),
                    AppVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Environment = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analytics_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "consent_preferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnonymousUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConsentVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AnalyticsAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    MarketingAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consent_preferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_consent_preferences_anonymous_users_AnonymousUserId",
                        column: x => x.AnonymousUserId,
                        principalTable: "anonymous_users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_consent_preferences_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_analytics_events_EventName_OccurredAt",
                table: "analytics_events",
                columns: new[] { "EventName", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_analytics_events_OccurredAt",
                table: "analytics_events",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_consent_preferences_AnonymousUserId",
                table: "consent_preferences",
                column: "AnonymousUserId",
                unique: true,
                filter: "\"AnonymousUserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_consent_preferences_UserId",
                table: "consent_preferences",
                column: "UserId",
                unique: true,
                filter: "\"UserId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analytics_events");

            migrationBuilder.DropTable(
                name: "consent_preferences");
        }
    }
}
