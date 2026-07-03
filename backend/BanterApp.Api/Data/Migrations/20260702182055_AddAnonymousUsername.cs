using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanterApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnonymousUsername : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "anonymous_users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_anonymous_users_Username",
                table: "anonymous_users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_anonymous_users_Username",
                table: "anonymous_users");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "anonymous_users");
        }
    }
}
