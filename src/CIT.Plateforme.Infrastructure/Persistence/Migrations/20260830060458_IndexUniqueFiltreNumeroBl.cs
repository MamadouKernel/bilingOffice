using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IndexUniqueFiltreNumeroBl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Tickets_NumeroBl",
                table: "Tickets",
                column: "NumeroBl",
                unique: true,
                filter: "[NumeroBl] IS NOT NULL AND [Statut] IN (0, 1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_NumeroBl",
                table: "Tickets");
        }
    }
}
