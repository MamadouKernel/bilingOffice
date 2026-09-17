using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutChampsFormulaireEnrolement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChampsFormulaireEnrolement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EstObligatoire = table.Column<bool>(type: "bit", nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    EstActif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChampsFormulaireEnrolement", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ValeursChampEnrolement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChampCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Valeur = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValeursChampEnrolement", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChampsFormulaireEnrolement_Code",
                table: "ChampsFormulaireEnrolement",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChampsFormulaireEnrolement_EstActif",
                table: "ChampsFormulaireEnrolement",
                column: "EstActif");

            migrationBuilder.CreateIndex(
                name: "IX_ValeursChampEnrolement_TicketId",
                table: "ValeursChampEnrolement",
                column: "TicketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChampsFormulaireEnrolement");

            migrationBuilder.DropTable(
                name: "ValeursChampEnrolement");
        }
    }
}
