using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutSupervisionPriorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModificationsPriorite",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AncienNiveauId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NouveauNiveauId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuteurId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Motif = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HorodatageUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModificationsPriorite", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NiveauxPriorite",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rang = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NiveauxPriorite", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerimetresSupervision",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SuperviseurId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    filesCouvertes = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerimetresSupervision", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModificationsPriorite_TicketId",
                table: "ModificationsPriorite",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_PerimetresSupervision_SuperviseurId",
                table: "PerimetresSupervision",
                column: "SuperviseurId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModificationsPriorite");

            migrationBuilder.DropTable(
                name: "NiveauxPriorite");

            migrationBuilder.DropTable(
                name: "PerimetresSupervision");
        }
    }
}
