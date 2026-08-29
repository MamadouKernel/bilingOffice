using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemediationAuditSlaCalendrierCompetenceValueComparer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // REMÉDIATION AUDIT (post-implementation-audit.md §3.D) : SQL Server ne convertit pas
            // implicitement `time` -> `bigint` (types incompatibles, aucune conversion automatique
            // sensée entre une heure du jour et un nombre de ticks). Le socle n'étant pas encore en
            // recette/production (aucune valeur SLA réelle enregistrée — PC-12 toujours ouvert),
            // la colonne est supprimée puis recréée dans le nouveau type plutôt que convertie :
            // aucune perte de donnée réelle, uniquement des valeurs de test locales à chaque
            // environnement de développement.
            migrationBuilder.DropColumn(
                name: "DureeCible",
                table: "ParametresSla");

            migrationBuilder.AddColumn<long>(
                name: "DureeCible",
                table: "ParametresSla",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "HeureDebut",
                table: "ParametresSla",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "HeureFin",
                table: "ParametresSla",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<byte>(
                name: "JoursOuvresBitmask",
                table: "ParametresSla",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "CompetenceRequise",
                table: "Files",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompetencesOperateur",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetencesOperateur", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PeriodesAttenteClient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DebutUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodesAttenteClient", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompetencesOperateur_OperateurId_Code",
                table: "CompetencesOperateur",
                columns: new[] { "OperateurId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeriodesAttenteClient_TicketId",
                table: "PeriodesAttenteClient",
                column: "TicketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompetencesOperateur");

            migrationBuilder.DropTable(
                name: "PeriodesAttenteClient");

            migrationBuilder.DropColumn(
                name: "HeureDebut",
                table: "ParametresSla");

            migrationBuilder.DropColumn(
                name: "HeureFin",
                table: "ParametresSla");

            migrationBuilder.DropColumn(
                name: "JoursOuvresBitmask",
                table: "ParametresSla");

            migrationBuilder.DropColumn(
                name: "CompetenceRequise",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "DureeCible",
                table: "ParametresSla");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "DureeCible",
                table: "ParametresSla",
                type: "time",
                nullable: false,
                defaultValue: TimeSpan.Zero);
        }
    }
}
