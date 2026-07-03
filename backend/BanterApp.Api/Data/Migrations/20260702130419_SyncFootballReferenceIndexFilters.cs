using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanterApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncFootballReferenceIndexFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_players_ExternalProvider_ExternalId",
                table: "players");

            migrationBuilder.DropIndex(
                name: "IX_countries_ExternalProvider_ExternalId",
                table: "countries");

            migrationBuilder.CreateIndex(
                name: "IX_players_ExternalProvider_ExternalId",
                table: "players",
                columns: new[] { "ExternalProvider", "ExternalId" },
                unique: true,
                filter: "\"ExternalProvider\" IS NOT NULL AND \"ExternalId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_countries_ExternalProvider_ExternalId",
                table: "countries",
                columns: new[] { "ExternalProvider", "ExternalId" },
                unique: true,
                filter: "\"ExternalProvider\" IS NOT NULL AND \"ExternalId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_players_ExternalProvider_ExternalId",
                table: "players");

            migrationBuilder.DropIndex(
                name: "IX_countries_ExternalProvider_ExternalId",
                table: "countries");

            migrationBuilder.CreateIndex(
                name: "IX_players_ExternalProvider_ExternalId",
                table: "players",
                columns: new[] { "ExternalProvider", "ExternalId" },
                unique: true,
                filter: "\"external_provider\" IS NOT NULL AND \"external_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_countries_ExternalProvider_ExternalId",
                table: "countries",
                columns: new[] { "ExternalProvider", "ExternalId" },
                unique: true,
                filter: "\"external_provider\" IS NOT NULL AND \"external_id\" IS NOT NULL");
        }
    }
}
