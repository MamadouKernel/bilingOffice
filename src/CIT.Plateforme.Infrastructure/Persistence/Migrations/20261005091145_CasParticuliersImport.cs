using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CasParticuliersImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CasParticuliers",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CasParticuliers",
                table: "Tickets");
        }
    }
}
