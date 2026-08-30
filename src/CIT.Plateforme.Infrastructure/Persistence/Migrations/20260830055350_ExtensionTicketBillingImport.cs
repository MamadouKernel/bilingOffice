using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtensionTicketBillingImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DeclarationPresentielle",
                table: "Tickets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NumeroBl",
                table: "Tickets",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                collation: "Latin1_General_CI_AS");

            migrationBuilder.AddColumn<string>(
                name: "TypeAnnulationImport",
                table: "Tickets",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeclarationPresentielle",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "NumeroBl",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "TypeAnnulationImport",
                table: "Tickets");
        }
    }
}
