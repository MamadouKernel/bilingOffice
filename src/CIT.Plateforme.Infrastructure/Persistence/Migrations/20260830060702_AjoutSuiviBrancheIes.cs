using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutSuiviBrancheIes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SuivisBranchesIes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Branche = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EstTerminee = table.Column<bool>(type: "bit", nullable: false),
                    AuteurId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DateAchevementUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuivisBranchesIes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SuivisBranchesIes_TicketId",
                table: "SuivisBranchesIes",
                column: "TicketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SuivisBranchesIes");
        }
    }
}
