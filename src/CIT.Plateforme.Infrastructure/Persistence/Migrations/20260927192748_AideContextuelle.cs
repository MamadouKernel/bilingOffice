using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AideContextuelle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArticlesAide",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Route = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TitreFr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TitreEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ContenuFr = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    ContenuEn = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticlesAide", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArticlesAide_Route",
                table: "ArticlesAide",
                column: "Route",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticlesAide");
        }
    }
}
