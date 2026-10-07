using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TypeChampEnrolement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Options",
                table: "ChampsFormulaireEnrolement",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "ChampsFormulaireEnrolement",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Texte");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChampsFormulaireEnrolement_Type",
                table: "ChampsFormulaireEnrolement",
                sql: "[Type] IN (N'Texte', N'Date', N'Liste')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ChampsFormulaireEnrolement_Type",
                table: "ChampsFormulaireEnrolement");

            migrationBuilder.DropColumn(
                name: "Options",
                table: "ChampsFormulaireEnrolement");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ChampsFormulaireEnrolement");
        }
    }
}
