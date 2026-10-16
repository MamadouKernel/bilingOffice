using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutMesuresActualisationBadt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MesuresActualisationBadt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LigneId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NumeroBl = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NumeroConteneur = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ActeurId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cause = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    NombreJoursAccordes = table.Column<int>(type: "int", nullable: true),
                    HorodatageUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MesuresActualisationBadt", x => x.Id);
                    table.CheckConstraint("CK_MesuresActualisationBadt_Cause", "[Cause] IN (N'Terminale', N'NonTerminale')");
                    table.CheckConstraint("CK_MesuresActualisationBadt_Type", "[Type] IN (N'Creation', N'Depliage', N'Qualification', N'RejetAgent', N'Accord', N'Refus', N'Renvoi')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MesuresActualisationBadt_HorodatageUtc",
                table: "MesuresActualisationBadt",
                column: "HorodatageUtc");

            migrationBuilder.CreateIndex(
                name: "IX_MesuresActualisationBadt_LigneId",
                table: "MesuresActualisationBadt",
                column: "LigneId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MesuresActualisationBadt");
        }
    }
}
