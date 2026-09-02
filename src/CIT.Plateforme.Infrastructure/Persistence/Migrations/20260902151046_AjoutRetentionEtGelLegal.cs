using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutRetentionEtGelLegal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateArchivageUtc",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EstAnonymise",
                table: "Tickets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstSousGelLegal",
                table: "Tickets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateScellementUtc",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmpreinteScellementSha256",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EstScelle",
                table: "Documents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "LotsPurgeRetention",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateExecutionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Categorie = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NombreElementsTraites = table.Column<int>(type: "int", nullable: false),
                    EstSucces = table.Column<bool>(type: "bit", nullable: false),
                    MessageErreur = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotsPurgeRetention", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LotsPurgeRetention");

            migrationBuilder.DropColumn(
                name: "DateArchivageUtc",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "EstAnonymise",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "EstSousGelLegal",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "DateScellementUtc",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "EmpreinteScellementSha256",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "EstScelle",
                table: "Documents");
        }
    }
}
