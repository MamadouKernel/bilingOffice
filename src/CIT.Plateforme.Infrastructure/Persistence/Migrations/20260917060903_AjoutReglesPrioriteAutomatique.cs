using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutReglesPrioriteAutomatique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReglesPrioriteAutomatique",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TypeDemande = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TypeClient = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PrioriteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstActif = table.Column<bool>(type: "bit", nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReglesPrioriteAutomatique", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReglesPrioriteAutomatique_EstActif",
                table: "ReglesPrioriteAutomatique",
                column: "EstActif");

            migrationBuilder.CreateIndex(
                name: "IX_ReglesPrioriteAutomatique_Ordre",
                table: "ReglesPrioriteAutomatique",
                column: "Ordre");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReglesPrioriteAutomatique");
        }
    }
}
