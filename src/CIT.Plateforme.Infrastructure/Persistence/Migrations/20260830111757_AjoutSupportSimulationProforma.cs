using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutSupportSimulationProforma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferenceSimulationProforma",
                table: "Dossiers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SimulationsProforma",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SocieteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuteurId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VersionGrilleTarifaireId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionGrilleLibelle = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Sens = table.Column<int>(type: "int", nullable: false),
                    DateArrivee = table.Column<DateOnly>(type: "date", nullable: false),
                    DateEnlevementPrevisionnel = table.Column<DateOnly>(type: "date", nullable: false),
                    DureeSejourJours = table.Column<int>(type: "int", nullable: false),
                    NomNavire = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    NumeroVoyage = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    NumeroBlOuBooking = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    MontantTotalXof = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationsProforma", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VersionsGrillesTarifaires",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DateEffetUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AuteurId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstActive = table.Column<bool>(type: "bit", nullable: false),
                    Parametres_TauxMajorationLourd = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Parametres_TauxMajorationDangereux = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Parametres_TauxMajorationOog = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Parametres_SeuilPoidsLourd20Pieds = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    Parametres_SeuilPoidsLourd40Pieds = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    Parametres_FranchiseBranchementJours = table.Column<int>(type: "int", nullable: false),
                    Parametres_FranchiseStationnementImportLocalJours = table.Column<int>(type: "int", nullable: false),
                    Parametres_FranchiseStationnementImportTransitJours = table.Column<int>(type: "int", nullable: false),
                    Parametres_TarifStationnementTranche1Jour = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Parametres_TarifStationnementTranche2Jour = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Parametres_TarifStationnementTranche3Jour = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Parametres_QuotePartCit = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Parametres_QuotePartOic = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Parametres_QuotePartPaa = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VersionsGrillesTarifaires", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LignesResultatsSimulation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SimulationProformaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroConteneur = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Taille = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    PoidsTonnes = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    EstDangereux = table.Column<bool>(type: "bit", nullable: false),
                    EstHorsGabarit = table.Column<bool>(type: "bit", nullable: false),
                    CategorieMarchandise = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MontantAcconage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontantRelevage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontantBranchement = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontantMajorationLourd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontantMajorationDangereux = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontantMajorationOog = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontantStationnement = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PartCit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PartOic = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PartPaa = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalLigneXof = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LignesResultatsSimulation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LignesResultatsSimulation_SimulationsProforma_SimulationProformaId",
                        column: x => x.SimulationProformaId,
                        principalTable: "SimulationsProforma",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LignesTarifaires",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionGrilleTarifaireId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sens = table.Column<int>(type: "int", nullable: false),
                    Taille = table.Column<int>(type: "int", nullable: false),
                    CategorieMarchandise = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Acconage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Relevage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TarifJourBranchement = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LignesTarifaires", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LignesTarifaires_VersionsGrillesTarifaires_VersionGrilleTarifaireId",
                        column: x => x.VersionGrilleTarifaireId,
                        principalTable: "VersionsGrillesTarifaires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LignesResultatsSimulation_SimulationProformaId",
                table: "LignesResultatsSimulation",
                column: "SimulationProformaId");

            migrationBuilder.CreateIndex(
                name: "IX_LignesTarifaires_VersionGrilleTarifaireId_Sens_Taille_CategorieMarchandise",
                table: "LignesTarifaires",
                columns: new[] { "VersionGrilleTarifaireId", "Sens", "Taille", "CategorieMarchandise" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SimulationsProforma_Reference",
                table: "SimulationsProforma",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SimulationsProforma_SocieteId_DateCreationUtc",
                table: "SimulationsProforma",
                columns: new[] { "SocieteId", "DateCreationUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VersionsGrillesTarifaires_DateEffetUtc",
                table: "VersionsGrillesTarifaires",
                column: "DateEffetUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LignesResultatsSimulation");

            migrationBuilder.DropTable(
                name: "LignesTarifaires");

            migrationBuilder.DropTable(
                name: "SimulationsProforma");

            migrationBuilder.DropTable(
                name: "VersionsGrillesTarifaires");

            migrationBuilder.DropColumn(
                name: "ReferenceSimulationProforma",
                table: "Dossiers");
        }
    }
}
