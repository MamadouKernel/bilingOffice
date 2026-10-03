using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BadgesEnrolement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BadgesEnrolement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DateRemiseUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateExpirationUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgesEnrolement", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BadgesEnrolement_DateExpirationUtc",
                table: "BadgesEnrolement",
                column: "DateExpirationUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BadgesEnrolement_Numero",
                table: "BadgesEnrolement",
                column: "Numero");

            migrationBuilder.CreateIndex(
                name: "IX_BadgesEnrolement_TicketId",
                table: "BadgesEnrolement",
                column: "TicketId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BadgesEnrolement");
        }
    }
}
