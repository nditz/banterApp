using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanterApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReactionGifUses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reaction_gif_uses",
                columns: table => new
                {
                    WindowId = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    GifId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Seed = table.Column<int>(type: "integer", nullable: true),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reaction_gif_uses", x => new { x.WindowId, x.GifId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_news_feed_items_PublishedAt",
                table: "news_feed_items",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_reaction_gif_uses_WindowId_Seed",
                table: "reaction_gif_uses",
                columns: new[] { "WindowId", "Seed" },
                unique: true,
                filter: "\"Seed\" IS NOT NULL");

            migrationBuilder.Sql("ALTER TABLE public.reaction_gif_uses ENABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reaction_gif_uses");

            migrationBuilder.DropIndex(
                name: "IX_news_feed_items_PublishedAt",
                table: "news_feed_items");
        }
    }
}
