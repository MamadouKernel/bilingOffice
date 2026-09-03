using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutMotifsTransfert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MotifsTransfert",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EstActif = table.Column<bool>(type: "bit", nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MotifsTransfert", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MotifsTransfert_Code",
                table: "MotifsTransfert",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MotifsTransfert_EstActif",
                table: "MotifsTransfert",
                column: "EstActif");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MotifsTransfert");
        }
    }
}
