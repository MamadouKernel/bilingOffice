using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutTypeObjetChampEnrolement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TypeObjet",
                table: "ChampsFormulaireEnrolement",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TypeObjet",
                table: "ChampsFormulaireEnrolement");
        }
    }
}
