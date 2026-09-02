using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutPiecesExigeesEtInvitationsCollaborateur : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvitationsCollaborateur",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SocieteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Jeton = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreeParId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Etat = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateExpirationUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvitationsCollaborateur", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvitationsCollaborateur_Societes_SocieteId",
                        column: x => x.SocieteId,
                        principalTable: "Societes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JustificatifsDemandeCompte",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SocieteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PieceCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NomFichier = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    TypeContenu = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TailleOctets = table.Column<long>(type: "bigint", nullable: false),
                    CleStockage = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    DateDepotUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JustificatifsDemandeCompte", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JustificatifsDemandeCompte_Societes_SocieteId",
                        column: x => x.SocieteId,
                        principalTable: "Societes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PiecesExigeesCompte",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Obligatoire = table.Column<bool>(type: "bit", nullable: false),
                    EstActif = table.Column<bool>(type: "bit", nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PiecesExigeesCompte", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvitationsCollaborateur_Jeton",
                table: "InvitationsCollaborateur",
                column: "Jeton",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvitationsCollaborateur_SocieteId",
                table: "InvitationsCollaborateur",
                column: "SocieteId");

            migrationBuilder.CreateIndex(
                name: "IX_JustificatifsDemandeCompte_SocieteId",
                table: "JustificatifsDemandeCompte",
                column: "SocieteId");

            migrationBuilder.CreateIndex(
                name: "IX_JustificatifsDemandeCompte_SocieteId_PieceCode",
                table: "JustificatifsDemandeCompte",
                columns: new[] { "SocieteId", "PieceCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PiecesExigeesCompte_Code",
                table: "PiecesExigeesCompte",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PiecesExigeesCompte_EstActif",
                table: "PiecesExigeesCompte",
                column: "EstActif");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvitationsCollaborateur");

            migrationBuilder.DropTable(
                name: "JustificatifsDemandeCompte");

            migrationBuilder.DropTable(
                name: "PiecesExigeesCompte");
        }
    }
}
