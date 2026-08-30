using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutSupportBillingExport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NumeroBooking",
                table: "Tickets",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                collation: "Latin1_General_CI_AS");

            migrationBuilder.AddColumn<string>(
                name: "TypeAnnulationExport",
                table: "Tickets",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_NumeroBooking_Actif",
                table: "Tickets",
                column: "NumeroBooking",
                unique: true,
                filter: "[NumeroBooking] IS NOT NULL AND [Statut] IN (0, 1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_NumeroBooking_Actif",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "NumeroBooking",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "TypeAnnulationExport",
                table: "Tickets");
        }
    }
}
