using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReferentielTypesClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TypesClient",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LibelleFr = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    LibelleEn = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DescriptionFr = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    DescriptionEn = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypesClient", x => x.Code);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TypesClient");
        }
    }
}
