using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjouterBrouillonsSaisie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BrouillonsSaisie",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SocieteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TypeFormulaire = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DonneesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateDerniereModificationUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrouillonsSaisie", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Brouillons_Societe",
                table: "BrouillonsSaisie",
                column: "SocieteId");

            migrationBuilder.CreateIndex(
                name: "IX_Brouillons_Utilisateur_Type",
                table: "BrouillonsSaisie",
                columns: new[] { "UtilisateurId", "TypeFormulaire" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrouillonsSaisie");
        }
    }
}
