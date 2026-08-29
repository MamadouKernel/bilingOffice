using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutDisponibilite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Disponibilites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    DateDerniereActiviteUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateDernierChangementUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Disponibilites", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Disponibilites_OperateurId",
                table: "Disponibilites",
                column: "OperateurId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Disponibilites");
        }
    }
}
