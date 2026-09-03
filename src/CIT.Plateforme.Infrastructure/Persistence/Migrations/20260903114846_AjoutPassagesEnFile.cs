using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutPassagesEnFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PassagesEnFile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rang = table.Column<int>(type: "int", nullable: false),
                    DateArriveeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DatePriseEnChargeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OperateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DateSortieUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PassagesEnFile", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PassagesEnFile_FileId_DateArriveeUtc",
                table: "PassagesEnFile",
                columns: new[] { "FileId", "DateArriveeUtc" },
                filter: "[DateSortieUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PassagesEnFile_TicketId_Rang",
                table: "PassagesEnFile",
                columns: new[] { "TicketId", "Rang" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PassagesEnFile");
        }
    }
}
