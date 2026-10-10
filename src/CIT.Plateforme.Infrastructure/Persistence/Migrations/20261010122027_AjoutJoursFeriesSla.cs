using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutJoursFeriesSla : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JoursFeries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EstActif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JoursFeries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JoursFeries_Date",
                table: "JoursFeries",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JoursFeries_EstActif",
                table: "JoursFeries",
                column: "EstActif");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JoursFeries");
        }
    }
}
