using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanterApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RssFeeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rss_feeds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    RssUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ApplePodcastId = table.Column<long>(type: "bigint", nullable: true),
                    SiteUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    StyleSlug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    ExtractPredictions = table.Column<bool>(type: "boolean", nullable: false),
                    UseForMediaIngest = table.Column<bool>(type: "boolean", nullable: false),
                    UseForNews = table.Column<bool>(type: "boolean", nullable: false),
                    UseForPundit = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastCheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastHttpStatus = table.Column<int>(type: "integer", nullable: true),
                    ConsecutiveFailures = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rss_feeds", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rss_feeds_ApplePodcastId",
                table: "rss_feeds",
                column: "ApplePodcastId",
                unique: true,
                filter: "\"ApplePodcastId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_rss_feeds_IsActive_Priority",
                table: "rss_feeds",
                columns: new[] { "IsActive", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_rss_feeds_Slug",
                table: "rss_feeds",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rss_feeds");
        }
    }
}
