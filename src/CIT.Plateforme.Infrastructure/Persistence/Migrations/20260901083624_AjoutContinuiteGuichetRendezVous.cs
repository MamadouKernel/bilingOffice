using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutContinuiteGuichetRendezVous : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RendezVous",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DossierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DemandeurId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    HeureDebut = table.Column<TimeSpan>(type: "time(7)", nullable: false),
                    HeureFin = table.Column<TimeSpan>(type: "time(7)", nullable: false),
                    Motif = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModificationUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CommentaireAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VersionConcurrence = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RendezVous", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RendezVous_Dossiers_DossierId",
                        column: x => x.DossierId,
                        principalTable: "Dossiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RendezVous_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RendezVous_Date_HeureDebut",
                table: "RendezVous",
                columns: new[] { "Date", "HeureDebut", "Statut" });

            migrationBuilder.CreateIndex(
                name: "IX_RendezVous_DemandeurId",
                table: "RendezVous",
                columns: new[] { "DemandeurId", "DateCreationUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RendezVous_DossierId",
                table: "RendezVous",
                column: "DossierId");

            migrationBuilder.CreateIndex(
                name: "IX_RendezVous_TicketId_ActifUnique",
                table: "RendezVous",
                column: "TicketId",
                unique: true,
                filter: "[Statut] = 'Reserve'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RendezVous");
        }
    }
}
