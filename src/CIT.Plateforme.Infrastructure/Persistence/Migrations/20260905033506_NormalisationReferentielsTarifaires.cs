using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalisationReferentielsTarifaires : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LignesTarifaires_VersionGrilleTarifaireId_Sens_Taille_CategorieMarchandise",
                table: "LignesTarifaires");

            migrationBuilder.DropColumn(
                name: "Parametres_FranchiseStationnementImportLocalJours",
                table: "VersionsGrillesTarifaires");

            migrationBuilder.DropColumn(
                name: "Parametres_FranchiseStationnementImportTransitJours",
                table: "VersionsGrillesTarifaires");

            migrationBuilder.DropColumn(
                name: "Parametres_QuotePartCit",
                table: "VersionsGrillesTarifaires");

            migrationBuilder.DropColumn(
                name: "Parametres_QuotePartOic",
                table: "VersionsGrillesTarifaires");

            migrationBuilder.DropColumn(
                name: "Parametres_QuotePartPaa",
                table: "VersionsGrillesTarifaires");

            migrationBuilder.DropColumn(
                name: "Parametres_SeuilPoidsLourd20Pieds",
                table: "VersionsGrillesTarifaires");

            migrationBuilder.DropColumn(
                name: "Parametres_SeuilPoidsLourd40Pieds",
                table: "VersionsGrillesTarifaires");

            migrationBuilder.DropColumn(
                name: "Parametres_TarifStationnementTranche1Jour",
                table: "VersionsGrillesTarifaires");

            migrationBuilder.DropColumn(
                name: "Parametres_TarifStationnementTranche2Jour",
                table: "VersionsGrillesTarifaires");

            migrationBuilder.DropColumn(
                name: "Parametres_TarifStationnementTranche3Jour",
                table: "VersionsGrillesTarifaires");

            migrationBuilder.DropColumn(
                name: "Parametres_TauxMajorationDangereux",
                table: "VersionsGrillesTarifaires");

            migrationBuilder.DropColumn(
                name: "Parametres_TauxMajorationLourd",
                table: "VersionsGrillesTarifaires");

            migrationBuilder.DropColumn(
                name: "Parametres_TauxMajorationOog",
                table: "VersionsGrillesTarifaires");

            migrationBuilder.DropColumn(
                name: "Sens",
                table: "SimulationsProforma");

            migrationBuilder.DropColumn(
                name: "CategorieMarchandise",
                table: "LignesTarifaires");

            migrationBuilder.DropColumn(
                name: "Sens",
                table: "LignesTarifaires");

            migrationBuilder.DropColumn(
                name: "Taille",
                table: "LignesTarifaires");

            migrationBuilder.DropColumn(
                name: "CategorieMarchandise",
                table: "LignesResultatsSimulation");

            migrationBuilder.DropColumn(
                name: "PartCit",
                table: "LignesResultatsSimulation");

            migrationBuilder.DropColumn(
                name: "PartOic",
                table: "LignesResultatsSimulation");

            migrationBuilder.DropColumn(
                name: "PartPaa",
                table: "LignesResultatsSimulation");

            migrationBuilder.DropColumn(
                name: "Taille",
                table: "LignesResultatsSimulation");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "LignesResultatsSimulation");

            migrationBuilder.RenameColumn(
                name: "Parametres_FranchiseBranchementJours",
                table: "VersionsGrillesTarifaires",
                newName: "FranchiseBranchementJours");

            migrationBuilder.AddColumn<Guid>(
                name: "SensOperationId",
                table: "SimulationsProforma",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TicketId",
                table: "SimulationsProforma",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CategorieMarchandiseId",
                table: "LignesTarifaires",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SensOperationId",
                table: "LignesTarifaires",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TailleConteneurId",
                table: "LignesTarifaires",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CategorieMarchandiseId",
                table: "LignesResultatsSimulation",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TailleConteneurId",
                table: "LignesResultatsSimulation",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TypeConteneurId",
                table: "LignesResultatsSimulation",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ParametresMajoration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionGrilleTarifaireId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Taux = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametresMajoration", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParametresMajoration_VersionsGrillesTarifaires_VersionGrilleTarifaireId",
                        column: x => x.VersionGrilleTarifaireId,
                        principalTable: "VersionsGrillesTarifaires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartsStationnementSimulees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LigneResultatSimulationConteneurId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Beneficiaire = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartsStationnementSimulees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartsStationnementSimulees_LignesResultatsSimulation_LigneResultatSimulationConteneurId",
                        column: x => x.LigneResultatSimulationConteneurId,
                        principalTable: "LignesResultatsSimulation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuotesPartsBeneficiaire",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionGrilleTarifaireId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Beneficiaire = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Pourcentage = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotesPartsBeneficiaire", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotesPartsBeneficiaire_VersionsGrillesTarifaires_VersionGrilleTarifaireId",
                        column: x => x.VersionGrilleTarifaireId,
                        principalTable: "VersionsGrillesTarifaires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SensOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensOperations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaillesConteneur",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaillesConteneur", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TranchesStationnement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionGrilleTarifaireId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BorneMinJour = table.Column<int>(type: "int", nullable: false),
                    BorneMaxJour = table.Column<int>(type: "int", nullable: true),
                    TarifJournalier = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranchesStationnement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TranchesStationnement_VersionsGrillesTarifaires_VersionGrilleTarifaireId",
                        column: x => x.VersionGrilleTarifaireId,
                        principalTable: "VersionsGrillesTarifaires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TypesConteneur",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeclencheBranchementFrigorifique = table.Column<bool>(type: "bit", nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypesConteneur", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CategoriesMarchandise",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SensOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriesMarchandise", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoriesMarchandise_SensOperations_SensOperationId",
                        column: x => x.SensOperationId,
                        principalTable: "SensOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FranchisesStationnement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionGrilleTarifaireId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SensOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreJours = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FranchisesStationnement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FranchisesStationnement_SensOperations_SensOperationId",
                        column: x => x.SensOperationId,
                        principalTable: "SensOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FranchisesStationnement_VersionsGrillesTarifaires_VersionGrilleTarifaireId",
                        column: x => x.VersionGrilleTarifaireId,
                        principalTable: "VersionsGrillesTarifaires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeuilsPoidsMajoration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionGrilleTarifaireId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TailleConteneurId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeuilPoidsTonnes = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeuilsPoidsMajoration", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeuilsPoidsMajoration_TaillesConteneur_TailleConteneurId",
                        column: x => x.TailleConteneurId,
                        principalTable: "TaillesConteneur",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SeuilsPoidsMajoration_VersionsGrillesTarifaires_VersionGrilleTarifaireId",
                        column: x => x.VersionGrilleTarifaireId,
                        principalTable: "VersionsGrillesTarifaires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SimulationsProforma_SensOperationId",
                table: "SimulationsProforma",
                column: "SensOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationsProforma_TicketId",
                table: "SimulationsProforma",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_LignesTarifaires_CategorieMarchandiseId",
                table: "LignesTarifaires",
                column: "CategorieMarchandiseId");

            migrationBuilder.CreateIndex(
                name: "IX_LignesTarifaires_SensOperationId",
                table: "LignesTarifaires",
                column: "SensOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_LignesTarifaires_TailleConteneurId",
                table: "LignesTarifaires",
                column: "TailleConteneurId");

            migrationBuilder.CreateIndex(
                name: "IX_LignesTarifaires_VersionGrilleTarifaireId_SensOperationId_TailleConteneurId_CategorieMarchandiseId",
                table: "LignesTarifaires",
                columns: new[] { "VersionGrilleTarifaireId", "SensOperationId", "TailleConteneurId", "CategorieMarchandiseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LignesResultatsSimulation_CategorieMarchandiseId",
                table: "LignesResultatsSimulation",
                column: "CategorieMarchandiseId");

            migrationBuilder.CreateIndex(
                name: "IX_LignesResultatsSimulation_TailleConteneurId",
                table: "LignesResultatsSimulation",
                column: "TailleConteneurId");

            migrationBuilder.CreateIndex(
                name: "IX_LignesResultatsSimulation_TypeConteneurId",
                table: "LignesResultatsSimulation",
                column: "TypeConteneurId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriesMarchandise_Code_SensOperationId",
                table: "CategoriesMarchandise",
                columns: new[] { "Code", "SensOperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoriesMarchandise_SensOperationId",
                table: "CategoriesMarchandise",
                column: "SensOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_FranchisesStationnement_SensOperationId",
                table: "FranchisesStationnement",
                column: "SensOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_FranchisesStationnement_VersionGrilleTarifaireId_SensOperationId",
                table: "FranchisesStationnement",
                columns: new[] { "VersionGrilleTarifaireId", "SensOperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParametresMajoration_VersionGrilleTarifaireId_Type",
                table: "ParametresMajoration",
                columns: new[] { "VersionGrilleTarifaireId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartsStationnementSimulees_LigneResultatSimulationConteneurId_Beneficiaire",
                table: "PartsStationnementSimulees",
                columns: new[] { "LigneResultatSimulationConteneurId", "Beneficiaire" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuotesPartsBeneficiaire_VersionGrilleTarifaireId_Beneficiaire",
                table: "QuotesPartsBeneficiaire",
                columns: new[] { "VersionGrilleTarifaireId", "Beneficiaire" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SensOperations_Code",
                table: "SensOperations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeuilsPoidsMajoration_TailleConteneurId",
                table: "SeuilsPoidsMajoration",
                column: "TailleConteneurId");

            migrationBuilder.CreateIndex(
                name: "IX_SeuilsPoidsMajoration_VersionGrilleTarifaireId_TailleConteneurId",
                table: "SeuilsPoidsMajoration",
                columns: new[] { "VersionGrilleTarifaireId", "TailleConteneurId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaillesConteneur_Code",
                table: "TaillesConteneur",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TranchesStationnement_VersionGrilleTarifaireId_BorneMinJour",
                table: "TranchesStationnement",
                columns: new[] { "VersionGrilleTarifaireId", "BorneMinJour" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TypesConteneur_Code",
                table: "TypesConteneur",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LignesResultatsSimulation_CategoriesMarchandise_CategorieMarchandiseId",
                table: "LignesResultatsSimulation",
                column: "CategorieMarchandiseId",
                principalTable: "CategoriesMarchandise",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LignesResultatsSimulation_TaillesConteneur_TailleConteneurId",
                table: "LignesResultatsSimulation",
                column: "TailleConteneurId",
                principalTable: "TaillesConteneur",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LignesResultatsSimulation_TypesConteneur_TypeConteneurId",
                table: "LignesResultatsSimulation",
                column: "TypeConteneurId",
                principalTable: "TypesConteneur",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LignesTarifaires_CategoriesMarchandise_CategorieMarchandiseId",
                table: "LignesTarifaires",
                column: "CategorieMarchandiseId",
                principalTable: "CategoriesMarchandise",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LignesTarifaires_SensOperations_SensOperationId",
                table: "LignesTarifaires",
                column: "SensOperationId",
                principalTable: "SensOperations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LignesTarifaires_TaillesConteneur_TailleConteneurId",
                table: "LignesTarifaires",
                column: "TailleConteneurId",
                principalTable: "TaillesConteneur",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SimulationsProforma_SensOperations_SensOperationId",
                table: "SimulationsProforma",
                column: "SensOperationId",
                principalTable: "SensOperations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SimulationsProforma_Tickets_TicketId",
                table: "SimulationsProforma",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LignesResultatsSimulation_CategoriesMarchandise_CategorieMarchandiseId",
                table: "LignesResultatsSimulation");

            migrationBuilder.DropForeignKey(
                name: "FK_LignesResultatsSimulation_TaillesConteneur_TailleConteneurId",
                table: "LignesResultatsSimulation");

            migrationBuilder.DropForeignKey(
                name: "FK_LignesResultatsSimulation_TypesConteneur_TypeConteneurId",
                table: "LignesResultatsSimulation");

            migrationBuilder.DropForeignKey(
                name: "FK_LignesTarifaires_CategoriesMarchandise_CategorieMarchandiseId",
                table: "LignesTarifaires");

            migrationBuilder.DropForeignKey(
                name: "FK_LignesTarifaires_SensOperations_SensOperationId",
                table: "LignesTarifaires");

            migrationBuilder.DropForeignKey(
                name: "FK_LignesTarifaires_TaillesConteneur_TailleConteneurId",
                table: "LignesTarifaires");

            migrationBuilder.DropForeignKey(
                name: "FK_SimulationsProforma_SensOperations_SensOperationId",
                table: "SimulationsProforma");

            migrationBuilder.DropForeignKey(
                name: "FK_SimulationsProforma_Tickets_TicketId",
                table: "SimulationsProforma");

            migrationBuilder.DropTable(
                name: "CategoriesMarchandise");

            migrationBuilder.DropTable(
                name: "FranchisesStationnement");

            migrationBuilder.DropTable(
                name: "ParametresMajoration");

            migrationBuilder.DropTable(
                name: "PartsStationnementSimulees");

            migrationBuilder.DropTable(
                name: "QuotesPartsBeneficiaire");

            migrationBuilder.DropTable(
                name: "SeuilsPoidsMajoration");

            migrationBuilder.DropTable(
                name: "TranchesStationnement");

            migrationBuilder.DropTable(
                name: "TypesConteneur");

            migrationBuilder.DropTable(
                name: "SensOperations");

            migrationBuilder.DropTable(
                name: "TaillesConteneur");

            migrationBuilder.DropIndex(
                name: "IX_SimulationsProforma_SensOperationId",
                table: "SimulationsProforma");

            migrationBuilder.DropIndex(
                name: "IX_SimulationsProforma_TicketId",
                table: "SimulationsProforma");

            migrationBuilder.DropIndex(
                name: "IX_LignesTarifaires_CategorieMarchandiseId",
                table: "LignesTarifaires");

            migrationBuilder.DropIndex(
                name: "IX_LignesTarifaires_SensOperationId",
                table: "LignesTarifaires");

            migrationBuilder.DropIndex(
                name: "IX_LignesTarifaires_TailleConteneurId",
                table: "LignesTarifaires");

            migrationBuilder.DropIndex(
                name: "IX_LignesTarifaires_VersionGrilleTarifaireId_SensOperationId_TailleConteneurId_CategorieMarchandiseId",
                table: "LignesTarifaires");

            migrationBuilder.DropIndex(
                name: "IX_LignesResultatsSimulation_CategorieMarchandiseId",
                table: "LignesResultatsSimulation");

            migrationBuilder.DropIndex(
                name: "IX_LignesResultatsSimulation_TailleConteneurId",
                table: "LignesResultatsSimulation");

            migrationBuilder.DropIndex(
                name: "IX_LignesResultatsSimulation_TypeConteneurId",
                table: "LignesResultatsSimulation");

            migrationBuilder.DropColumn(
                name: "SensOperationId",
                table: "SimulationsProforma");

            migrationBuilder.DropColumn(
                name: "TicketId",
                table: "SimulationsProforma");

            migrationBuilder.DropColumn(
                name: "CategorieMarchandiseId",
                table: "LignesTarifaires");

            migrationBuilder.DropColumn(
                name: "SensOperationId",
                table: "LignesTarifaires");

            migrationBuilder.DropColumn(
                name: "TailleConteneurId",
                table: "LignesTarifaires");

            migrationBuilder.DropColumn(
                name: "CategorieMarchandiseId",
                table: "LignesResultatsSimulation");

            migrationBuilder.DropColumn(
                name: "TailleConteneurId",
                table: "LignesResultatsSimulation");

            migrationBuilder.DropColumn(
                name: "TypeConteneurId",
                table: "LignesResultatsSimulation");

            migrationBuilder.RenameColumn(
                name: "FranchiseBranchementJours",
                table: "VersionsGrillesTarifaires",
                newName: "Parametres_FranchiseBranchementJours");

            migrationBuilder.AddColumn<int>(
                name: "Parametres_FranchiseStationnementImportLocalJours",
                table: "VersionsGrillesTarifaires",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Parametres_FranchiseStationnementImportTransitJours",
                table: "VersionsGrillesTarifaires",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Parametres_QuotePartCit",
                table: "VersionsGrillesTarifaires",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Parametres_QuotePartOic",
                table: "VersionsGrillesTarifaires",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Parametres_QuotePartPaa",
                table: "VersionsGrillesTarifaires",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Parametres_SeuilPoidsLourd20Pieds",
                table: "VersionsGrillesTarifaires",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Parametres_SeuilPoidsLourd40Pieds",
                table: "VersionsGrillesTarifaires",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Parametres_TarifStationnementTranche1Jour",
                table: "VersionsGrillesTarifaires",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Parametres_TarifStationnementTranche2Jour",
                table: "VersionsGrillesTarifaires",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Parametres_TarifStationnementTranche3Jour",
                table: "VersionsGrillesTarifaires",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Parametres_TauxMajorationDangereux",
                table: "VersionsGrillesTarifaires",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Parametres_TauxMajorationLourd",
                table: "VersionsGrillesTarifaires",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Parametres_TauxMajorationOog",
                table: "VersionsGrillesTarifaires",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Sens",
                table: "SimulationsProforma",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CategorieMarchandise",
                table: "LignesTarifaires",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Sens",
                table: "LignesTarifaires",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Taille",
                table: "LignesTarifaires",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CategorieMarchandise",
                table: "LignesResultatsSimulation",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PartCit",
                table: "LignesResultatsSimulation",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PartOic",
                table: "LignesResultatsSimulation",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PartPaa",
                table: "LignesResultatsSimulation",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Taille",
                table: "LignesResultatsSimulation",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "LignesResultatsSimulation",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_LignesTarifaires_VersionGrilleTarifaireId_Sens_Taille_CategorieMarchandise",
                table: "LignesTarifaires",
                columns: new[] { "VersionGrilleTarifaireId", "Sens", "Taille", "CategorieMarchandise" },
                unique: true);
        }
    }
}
