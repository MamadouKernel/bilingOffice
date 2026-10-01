using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HistoriqueDisponibilite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistoriquesDisponibilite",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AncienStatut = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    NouveauStatut = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    HorodatageUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoriquesDisponibilite", x => x.Id);
                    table.CheckConstraint("CK_HistoriquesDisponibilite_AncienStatut", "[AncienStatut] IN (N'Disponible', N'Indisponible', N'Absent')");
                    table.CheckConstraint("CK_HistoriquesDisponibilite_NouveauStatut", "[NouveauStatut] IN (N'Disponible', N'Indisponible', N'Absent')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistoriquesDisponibilite_OperateurId_HorodatageUtc",
                table: "HistoriquesDisponibilite",
                columns: new[] { "OperateurId", "HorodatageUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoriquesDisponibilite");
        }
    }
}
