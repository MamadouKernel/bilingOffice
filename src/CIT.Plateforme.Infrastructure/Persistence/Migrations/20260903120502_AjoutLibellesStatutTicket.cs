using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutLibellesStatutTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LibellesStatutTicket",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LibelleFr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LibelleEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    EstEngageant = table.Column<bool>(type: "bit", nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibellesStatutTicket", x => x.Code);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LibellesStatutTicket_Ordre",
                table: "LibellesStatutTicket",
                column: "Ordre");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LibellesStatutTicket");
        }
    }
}
