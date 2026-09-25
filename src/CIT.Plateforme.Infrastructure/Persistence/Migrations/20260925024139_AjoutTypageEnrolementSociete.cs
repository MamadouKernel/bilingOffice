using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutTypageEnrolementSociete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ObjetEnrolement",
                table: "Tickets",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SensEnrolement",
                table: "Tickets",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ObjetEnrolement",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "SensEnrolement",
                table: "Tickets");
        }
    }
}
