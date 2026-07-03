using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanterApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPunditMatchLinkAndFeedQuality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MatchId",
                table: "pundit_opinions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchId",
                table: "news_feed_items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PredictionSummary",
                table: "news_feed_items",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QualityScore",
                table: "news_feed_items",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_pundit_opinions_MatchId",
                table: "pundit_opinions",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_news_feed_items_MatchId",
                table: "news_feed_items",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_news_feed_items_QualityScore",
                table: "news_feed_items",
                column: "QualityScore");

            migrationBuilder.AddForeignKey(
                name: "FK_pundit_opinions_matches_MatchId",
                table: "pundit_opinions",
                column: "MatchId",
                principalTable: "matches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pundit_opinions_matches_MatchId",
                table: "pundit_opinions");

            migrationBuilder.DropIndex(
                name: "IX_pundit_opinions_MatchId",
                table: "pundit_opinions");

            migrationBuilder.DropIndex(
                name: "IX_news_feed_items_MatchId",
                table: "news_feed_items");

            migrationBuilder.DropIndex(
                name: "IX_news_feed_items_QualityScore",
                table: "news_feed_items");

            migrationBuilder.DropColumn(
                name: "MatchId",
                table: "pundit_opinions");

            migrationBuilder.DropColumn(
                name: "MatchId",
                table: "news_feed_items");

            migrationBuilder.DropColumn(
                name: "PredictionSummary",
                table: "news_feed_items");

            migrationBuilder.DropColumn(
                name: "QualityScore",
                table: "news_feed_items");
        }
    }
}
