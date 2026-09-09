using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanterApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGifLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gif_assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Title = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Mood = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Tags = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gif_assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gif_search_queries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Phrase = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsFootballRelated = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastSeenAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gif_search_queries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gif_assets_IsActive_Mood",
                table: "gif_assets",
                columns: new[] { "IsActive", "Mood" });

            migrationBuilder.CreateIndex(
                name: "IX_gif_assets_Url",
                table: "gif_assets",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gif_search_queries_IsActive_IsFootballRelated_LastSeenAtUtc",
                table: "gif_search_queries",
                columns: new[] { "IsActive", "IsFootballRelated", "LastSeenAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_gif_search_queries_Phrase",
                table: "gif_search_queries",
                column: "Phrase",
                unique: true);

            migrationBuilder.Sql("ALTER TABLE public.gif_assets ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE public.gif_search_queries ENABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gif_assets");

            migrationBuilder.DropTable(
                name: "gif_search_queries");
        }
    }
}
