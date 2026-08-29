using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutLiaisonSlaAlertes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "DureeAttenteClienteCumulee",
                table: "Tickets",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.CreateTable(
                name: "Alertes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Traitee = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alertes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LiaisonsTicket",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketLieId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreeParId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiaisonsTicket", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParametresSla",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TypeDemande = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DureeCible = table.Column<TimeSpan>(type: "time", nullable: false),
                    SeuilInformation = table.Column<double>(type: "float", nullable: false),
                    SeuilAlerte = table.Column<double>(type: "float", nullable: false),
                    UtiliserCalendrierToujoursOuvert = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametresSla", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alertes_TicketId_Type",
                table: "Alertes",
                columns: new[] { "TicketId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_LiaisonsTicket_TicketId",
                table: "LiaisonsTicket",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_LiaisonsTicket_TicketLieId",
                table: "LiaisonsTicket",
                column: "TicketLieId");

            migrationBuilder.CreateIndex(
                name: "IX_ParametresSla_Module_TypeDemande",
                table: "ParametresSla",
                columns: new[] { "Module", "TypeDemande" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alertes");

            migrationBuilder.DropTable(
                name: "LiaisonsTicket");

            migrationBuilder.DropTable(
                name: "ParametresSla");

            migrationBuilder.DropColumn(
                name: "DureeAttenteClienteCumulee",
                table: "Tickets");
        }
    }
}
