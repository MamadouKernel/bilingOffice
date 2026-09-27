using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IdentiteStructureeSociete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RaisonSociale",
                table: "Societes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rccm",
                table: "Societes",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Societes_Rccm",
                table: "Societes",
                column: "Rccm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Societes_Rccm",
                table: "Societes");

            migrationBuilder.DropColumn(
                name: "RaisonSociale",
                table: "Societes");

            migrationBuilder.DropColumn(
                name: "Rccm",
                table: "Societes");
        }
    }
}
