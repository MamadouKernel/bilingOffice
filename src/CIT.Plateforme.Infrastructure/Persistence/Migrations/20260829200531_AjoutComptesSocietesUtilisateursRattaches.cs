using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutComptesSocietesUtilisateursRattaches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Societes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InformationsDeclarees = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Profil = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Etat = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NombreAdministrateursActifs = table.Column<int>(type: "int", nullable: false),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VersionConcurrence = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Societes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UtilisateursSociete",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SocieteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstAdministrateur = table.Column<bool>(type: "bit", nullable: false),
                    DroitConsultation = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DroitSoumission = table.Column<bool>(type: "bit", nullable: false),
                    EstActif = table.Column<bool>(type: "bit", nullable: false),
                    DateRattachementUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilisateursSociete", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Societes_Etat",
                table: "Societes",
                column: "Etat");

            migrationBuilder.CreateIndex(
                name: "IX_UtilisateursSociete_SocieteId",
                table: "UtilisateursSociete",
                column: "SocieteId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilisateursSociete_UtilisateurId",
                table: "UtilisateursSociete",
                column: "UtilisateurId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Societes");

            migrationBuilder.DropTable(
                name: "UtilisateursSociete");
        }
    }
}
