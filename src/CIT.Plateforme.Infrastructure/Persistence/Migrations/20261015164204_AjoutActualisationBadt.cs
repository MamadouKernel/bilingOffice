using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutActualisationBadt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LignesActualisationBadt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroBl = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NumeroConteneur = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Cause = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    MotifCause = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Decision = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NombreJoursAccordes = table.Column<int>(type: "int", nullable: true),
                    AncienneDateExpiration = table.Column<DateOnly>(type: "date", nullable: true),
                    NouvelleDateValidite = table.Column<DateOnly>(type: "date", nullable: true),
                    MotifRefus = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LignesActualisationBadt", x => x.Id);
                    table.CheckConstraint("CK_LignesActualisationBadt_Cause", "[Cause] IN (N'Terminale', N'NonTerminale')");
                    table.CheckConstraint("CK_LignesActualisationBadt_Decision", "[Decision] IN (N'EnAttente', N'Accordee', N'Refusee')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LignesActualisationBadt_TicketId",
                table: "LignesActualisationBadt",
                column: "TicketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LignesActualisationBadt");
        }
    }
}
