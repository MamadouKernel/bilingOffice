using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DistribueLeUtc",
                table: "JournalEvenements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EnvoisNotification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Canal = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NombreTentatives = table.Column<int>(type: "int", nullable: false),
                    DerniereTentativeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AboutiLeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotifEchec = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvoisNotification", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModelesNotification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TypeEvenement = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Canal = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    GabaritTitre = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    GabaritCorps = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstActif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelesNotification", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvenementSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinataireId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TypeEvenement = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ModeleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Titre = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Corps = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstLue = table.Column<bool>(type: "bit", nullable: false),
                    LueLeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreeeLeUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PreferencesCanalUtilisateur",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Canal = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EstActif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreferencesCanalUtilisateur", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReglesDiffusion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TypeEvenement = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Canal = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EstObligatoire = table.Column<bool>(type: "bit", nullable: false),
                    EstActif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReglesDiffusion", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEvenements_DistribueLeUtc",
                table: "JournalEvenements",
                column: "DistribueLeUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EnvoisNotification_NotificationId",
                table: "EnvoisNotification",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_EnvoisNotification_Statut_DerniereTentative",
                table: "EnvoisNotification",
                columns: new[] { "Statut", "DerniereTentativeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ModelesNotification_Evenement_Canal_Actif",
                table: "ModelesNotification",
                columns: new[] { "TypeEvenement", "Canal" },
                unique: true,
                filter: "[EstActif] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_DestinataireId",
                table: "Notifications",
                column: "DestinataireId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_EvenementSource_Destinataire",
                table: "Notifications",
                columns: new[] { "EvenementSourceId", "DestinataireId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreferencesCanal_Utilisateur_Canal",
                table: "PreferencesCanalUtilisateur",
                columns: new[] { "UtilisateurId", "Canal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReglesDiffusion_Evenement_Canal",
                table: "ReglesDiffusion",
                columns: new[] { "TypeEvenement", "Canal" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnvoisNotification");

            migrationBuilder.DropTable(
                name: "ModelesNotification");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PreferencesCanalUtilisateur");

            migrationBuilder.DropTable(
                name: "ReglesDiffusion");

            migrationBuilder.DropIndex(
                name: "IX_JournalEvenements_DistribueLeUtc",
                table: "JournalEvenements");

            migrationBuilder.DropColumn(
                name: "DistribueLeUtc",
                table: "JournalEvenements");
        }
    }
}
