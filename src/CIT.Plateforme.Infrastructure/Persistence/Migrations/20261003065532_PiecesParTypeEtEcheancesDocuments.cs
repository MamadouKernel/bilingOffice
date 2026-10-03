using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PiecesParTypeEtEcheancesDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TypeClientCible",
                table: "PiecesExigeesCompte",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EcheancesDocumentEnrolement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Document = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DateExpiration = table.Column<DateOnly>(type: "date", nullable: false),
                    RappelEmisUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EcheancesDocumentEnrolement", x => x.Id);
                    table.CheckConstraint("CK_EcheancesDocumentEnrolement_Document", "[Document] IN (N'PermisConduire', N'AssuranceVehicule', N'VisiteTechnique')");
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_PiecesExigeesCompte_TypeClientCible",
                table: "PiecesExigeesCompte",
                sql: "[TypeClientCible] IN (N'Normal', N'EnCompte', N'Deposit', N'IES', N'Transporteur')");

            migrationBuilder.CreateIndex(
                name: "IX_EcheancesDocumentEnrolement_RappelEmisUtc_DateExpiration",
                table: "EcheancesDocumentEnrolement",
                columns: new[] { "RappelEmisUtc", "DateExpiration" });

            migrationBuilder.CreateIndex(
                name: "IX_EcheancesDocumentEnrolement_TicketId",
                table: "EcheancesDocumentEnrolement",
                column: "TicketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EcheancesDocumentEnrolement");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PiecesExigeesCompte_TypeClientCible",
                table: "PiecesExigeesCompte");

            migrationBuilder.DropColumn(
                name: "TypeClientCible",
                table: "PiecesExigeesCompte");
        }
    }
}
