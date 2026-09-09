using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanterApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPunditFollows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pundit_follows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnonymousUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PunditId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pundit_follows", x => x.Id);
                    table.CheckConstraint("CK_pundit_follows_owner", "(\"UserId\" IS NOT NULL AND \"AnonymousUserId\" IS NULL) OR (\"UserId\" IS NULL AND \"AnonymousUserId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_pundit_follows_anonymous_users_AnonymousUserId",
                        column: x => x.AnonymousUserId,
                        principalTable: "anonymous_users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_pundit_follows_pundits_PunditId",
                        column: x => x.PunditId,
                        principalTable: "pundits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pundit_follows_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_pundit_follows_AnonymousUserId_PunditId",
                table: "pundit_follows",
                columns: new[] { "AnonymousUserId", "PunditId" },
                unique: true,
                filter: "\"AnonymousUserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_pundit_follows_PunditId",
                table: "pundit_follows",
                column: "PunditId");

            migrationBuilder.CreateIndex(
                name: "IX_pundit_follows_UserId_PunditId",
                table: "pundit_follows",
                columns: new[] { "UserId", "PunditId" },
                unique: true,
                filter: "\"UserId\" IS NOT NULL");

            migrationBuilder.Sql("ALTER TABLE public.pundit_follows ENABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pundit_follows");
        }
    }
}
