using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnumerationsEnTexte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_NumeroBl",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_NumeroBooking_Actif",
                table: "Tickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Tickets_Statut",
                table: "Tickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Messages_Type",
                table: "Messages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Files_StrategieActive",
                table: "Files");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Documents_StatutAntivirus",
                table: "Documents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Disponibilites_Statut",
                table: "Disponibilites");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AspNetUsers_Nature",
                table: "AspNetUsers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Alertes_Type",
                table: "Alertes");

            migrationBuilder.AlterColumn<string>(
                name: "Statut",
                table: "Tickets",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Messages",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "StrategieActive",
                table: "Files",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "StatutAntivirus",
                table: "Documents",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Statut",
                table: "Disponibilites",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Nature",
                table: "AspNetUsers",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Alertes",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            // DONNÉES — chaque valeur entière devient le nom de sa valeur d'énumération (0 → premier
            // nom, etc.), AVANT la recréation des index filtrés et des contraintes CHECK en texte.
            migrationBuilder.Sql(@"
UPDATE [Tickets] SET [Statut] = CASE [Statut] WHEN N'0' THEN N'EnAttenteDePriseEnCharge' WHEN N'1' THEN N'EnCours' WHEN N'2' THEN N'EnAttenteClient' WHEN N'3' THEN N'Cloture' WHEN N'4' THEN N'Annule' END;
UPDATE [Disponibilites] SET [Statut] = CASE [Statut] WHEN N'0' THEN N'Disponible' WHEN N'1' THEN N'Indisponible' WHEN N'2' THEN N'Absent' END;
UPDATE [Documents] SET [StatutAntivirus] = CASE [StatutAntivirus] WHEN N'0' THEN N'NonAnalyse' WHEN N'1' THEN N'Sain' WHEN N'2' THEN N'Infecte' END;
UPDATE [Files] SET [StrategieActive] = CASE [StrategieActive] WHEN N'0' THEN N'PriseLibre' WHEN N'1' THEN N'AffectationManuelle' WHEN N'2' THEN N'RoundRobin' WHEN N'3' THEN N'Charge' WHEN N'4' THEN N'Disponibilite' WHEN N'5' THEN N'Competence' END;
UPDATE [Messages] SET [Type] = CASE [Type] WHEN N'0' THEN N'Externe' WHEN N'1' THEN N'NoteInterne' END;
UPDATE [Alertes] SET [Type] = CASE [Type] WHEN N'0' THEN N'NonPriseEnCharge' WHEN N'1' THEN N'SansActivite' WHEN N'2' THEN N'SlaSeuilInformation' WHEN N'3' THEN N'SlaSeuilAlerte' WHEN N'4' THEN N'SlaDepasse' WHEN N'5' THEN N'SlaEscalade' WHEN N'6' THEN N'BacklogFile' WHEN N'7' THEN N'VolumeAnormal' WHEN N'8' THEN N'OperateurIndisponibleAvecTickets' END;
UPDATE [AspNetUsers] SET [Nature] = CASE [Nature] WHEN N'0' THEN N'Client' WHEN N'1' THEN N'Agent' END;");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_NumeroBl",
                table: "Tickets",
                column: "NumeroBl",
                unique: true,
                filter: "[NumeroBl] IS NOT NULL AND [Statut] IN (N'EnAttenteDePriseEnCharge', N'EnCours', N'EnAttenteClient')");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_NumeroBooking_Actif",
                table: "Tickets",
                column: "NumeroBooking",
                unique: true,
                filter: "[NumeroBooking] IS NOT NULL AND [Statut] IN (N'EnAttenteDePriseEnCharge', N'EnCours', N'EnAttenteClient')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tickets_Statut",
                table: "Tickets",
                sql: "[Statut] IN (N'EnAttenteDePriseEnCharge', N'EnCours', N'EnAttenteClient', N'Cloture', N'Annule')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Messages_Type",
                table: "Messages",
                sql: "[Type] IN (N'Externe', N'NoteInterne')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Files_StrategieActive",
                table: "Files",
                sql: "[StrategieActive] IN (N'PriseLibre', N'AffectationManuelle', N'RoundRobin', N'Charge', N'Disponibilite', N'Competence')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Documents_StatutAntivirus",
                table: "Documents",
                sql: "[StatutAntivirus] IN (N'NonAnalyse', N'Sain', N'Infecte')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Disponibilites_Statut",
                table: "Disponibilites",
                sql: "[Statut] IN (N'Disponible', N'Indisponible', N'Absent')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AspNetUsers_Nature",
                table: "AspNetUsers",
                sql: "[Nature] IN (N'Client', N'Agent')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Alertes_Type",
                table: "Alertes",
                sql: "[Type] IN (N'NonPriseEnCharge', N'SansActivite', N'SlaSeuilInformation', N'SlaSeuilAlerte', N'SlaDepasse', N'SlaEscalade', N'BacklogFile', N'VolumeAnormal', N'OperateurIndisponibleAvecTickets')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_NumeroBl",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_NumeroBooking_Actif",
                table: "Tickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Tickets_Statut",
                table: "Tickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Messages_Type",
                table: "Messages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Files_StrategieActive",
                table: "Files");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Documents_StatutAntivirus",
                table: "Documents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Disponibilites_Statut",
                table: "Disponibilites");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AspNetUsers_Nature",
                table: "AspNetUsers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Alertes_Type",
                table: "Alertes");

            // DONNÉES — retour aux entiers avant de rétablir le type int.
            migrationBuilder.Sql(@"
UPDATE [Tickets] SET [Statut] = CASE [Statut] WHEN N'EnAttenteDePriseEnCharge' THEN N'0' WHEN N'EnCours' THEN N'1' WHEN N'EnAttenteClient' THEN N'2' WHEN N'Cloture' THEN N'3' WHEN N'Annule' THEN N'4' END;
UPDATE [Disponibilites] SET [Statut] = CASE [Statut] WHEN N'Disponible' THEN N'0' WHEN N'Indisponible' THEN N'1' WHEN N'Absent' THEN N'2' END;
UPDATE [Documents] SET [StatutAntivirus] = CASE [StatutAntivirus] WHEN N'NonAnalyse' THEN N'0' WHEN N'Sain' THEN N'1' WHEN N'Infecte' THEN N'2' END;
UPDATE [Files] SET [StrategieActive] = CASE [StrategieActive] WHEN N'PriseLibre' THEN N'0' WHEN N'AffectationManuelle' THEN N'1' WHEN N'RoundRobin' THEN N'2' WHEN N'Charge' THEN N'3' WHEN N'Disponibilite' THEN N'4' WHEN N'Competence' THEN N'5' END;
UPDATE [Messages] SET [Type] = CASE [Type] WHEN N'Externe' THEN N'0' WHEN N'NoteInterne' THEN N'1' END;
UPDATE [Alertes] SET [Type] = CASE [Type] WHEN N'NonPriseEnCharge' THEN N'0' WHEN N'SansActivite' THEN N'1' WHEN N'SlaSeuilInformation' THEN N'2' WHEN N'SlaSeuilAlerte' THEN N'3' WHEN N'SlaDepasse' THEN N'4' WHEN N'SlaEscalade' THEN N'5' WHEN N'BacklogFile' THEN N'6' WHEN N'VolumeAnormal' THEN N'7' WHEN N'OperateurIndisponibleAvecTickets' THEN N'8' END;
UPDATE [AspNetUsers] SET [Nature] = CASE [Nature] WHEN N'Client' THEN N'0' WHEN N'Agent' THEN N'1' END;");

            migrationBuilder.AlterColumn<int>(
                name: "Statut",
                table: "Tickets",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Messages",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<int>(
                name: "StrategieActive",
                table: "Files",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<int>(
                name: "StatutAntivirus",
                table: "Documents",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<int>(
                name: "Statut",
                table: "Disponibilites",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<int>(
                name: "Nature",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Alertes",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_NumeroBl",
                table: "Tickets",
                column: "NumeroBl",
                unique: true,
                filter: "[NumeroBl] IS NOT NULL AND [Statut] IN (0, 1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_NumeroBooking_Actif",
                table: "Tickets",
                column: "NumeroBooking",
                unique: true,
                filter: "[NumeroBooking] IS NOT NULL AND [Statut] IN (0, 1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tickets_Statut",
                table: "Tickets",
                sql: "[Statut] IN (0, 1, 2, 3, 4)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Messages_Type",
                table: "Messages",
                sql: "[Type] IN (0, 1)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Files_StrategieActive",
                table: "Files",
                sql: "[StrategieActive] IN (0, 1, 2, 3, 4, 5)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Documents_StatutAntivirus",
                table: "Documents",
                sql: "[StatutAntivirus] IN (0, 1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Disponibilites_Statut",
                table: "Disponibilites",
                sql: "[Statut] IN (0, 1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AspNetUsers_Nature",
                table: "AspNetUsers",
                sql: "[Nature] IN (0, 1)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Alertes_Type",
                table: "Alertes",
                sql: "[Type] IN (0, 1, 2, 3, 4, 5, 6, 7, 8)");
        }
    }
}
