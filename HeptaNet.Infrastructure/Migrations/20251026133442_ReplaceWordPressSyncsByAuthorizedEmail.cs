using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HeptaNet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceWordPressSyncsByAuthorizedEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WordPressSyncs");

            migrationBuilder.CreateTable(
                name: "AuthorizedEmails",
                columns: table => new
                {
                    SyncId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsImported = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SyncDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorizedEmails", x => x.SyncId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizedEmails_Email",
                table: "AuthorizedEmails",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthorizedEmails");

            migrationBuilder.CreateTable(
                name: "WordPressSyncs",
                columns: table => new
                {
                    SyncId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsImported = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SyncDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordPressSyncs", x => x.SyncId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WordPressSyncs_Email",
                table: "WordPressSyncs",
                column: "Email",
                unique: true);
        }
    }
}
