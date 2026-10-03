using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class JustificatifsMultiplesParPiece : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JustificatifsDemandeCompte_SocieteId_PieceCode",
                table: "JustificatifsDemandeCompte");

            migrationBuilder.CreateIndex(
                name: "IX_JustificatifsDemandeCompte_SocieteId_PieceCode",
                table: "JustificatifsDemandeCompte",
                columns: new[] { "SocieteId", "PieceCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JustificatifsDemandeCompte_SocieteId_PieceCode",
                table: "JustificatifsDemandeCompte");

            migrationBuilder.CreateIndex(
                name: "IX_JustificatifsDemandeCompte_SocieteId_PieceCode",
                table: "JustificatifsDemandeCompte",
                columns: new[] { "SocieteId", "PieceCode" },
                unique: true);
        }
    }
}
