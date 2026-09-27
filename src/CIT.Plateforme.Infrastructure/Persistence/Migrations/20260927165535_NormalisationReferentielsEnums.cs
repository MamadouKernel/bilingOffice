using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalisationReferentielsEnums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MotifCloture",
                table: "Tickets",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Gravite",
                table: "Tickets",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(16)",
                oldMaxLength: 16,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TypeClient",
                table: "ParametresSla",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // DONNÉES — les deux colonnes jusque-là entières passent en code texte (0 → nom de la
            // valeur de l'énumération), comme toutes les autres colonnes d'énumération.
            migrationBuilder.Sql(@"
UPDATE [Tickets] SET [MotifCloture] = CASE [MotifCloture] WHEN N'0' THEN N'Normale' WHEN N'1' THEN N'AbsenceReponse' END
WHERE [MotifCloture] IS NOT NULL;
UPDATE [ParametresSla] SET [TypeClient] = CASE [TypeClient] WHEN N'0' THEN N'Normal' WHEN N'1' THEN N'EnCompte' WHEN N'2' THEN N'Deposit' WHEN N'3' THEN N'IES' END
WHERE [TypeClient] IS NOT NULL;");

            migrationBuilder.CreateTable(
                name: "CanauxNotification",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LibelleFr = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    LibelleEn = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanauxNotification", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "CategoriesDocument",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LibelleFr = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    LibelleEn = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriesDocument", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "GravitesIncident",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LibelleFr = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    LibelleEn = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GravitesIncident", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "MotifsCloture",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LibelleFr = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    LibelleEn = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MotifsCloture", x => x.Code);
                });

            // Valeurs des référentiels, insérées AVANT les clés étrangères qui les exigent. Libellés
            // par défaut, modifiables ensuite depuis l'administration.
            migrationBuilder.Sql(@"
INSERT INTO [GravitesIncident] ([Code],[LibelleFr],[LibelleEn],[Ordre],[Actif]) VALUES
 (N'Mineure',N'Mineure',N'Minor',10,1),(N'Majeure',N'Majeure',N'Major',20,1),(N'Critique',N'Critique',N'Critical',30,1);
INSERT INTO [MotifsCloture] ([Code],[LibelleFr],[LibelleEn],[Ordre],[Actif]) VALUES
 (N'Normale',N'Clôture normale',N'Normal closure',10,1),(N'AbsenceReponse',N'Absence de réponse du client',N'No response from customer',20,1);
INSERT INTO [CategoriesDocument] ([Code],[LibelleFr],[LibelleEn],[Ordre],[Actif]) VALUES
 (N'DeclarationDouane',N'Déclaration en douane',N'Customs declaration',10,1),(N'CopieDuBl',N'Copie du BL',N'Copy of the B/L',20,1),(N'AutresPieces',N'Autres pièces',N'Other documents',30,1);
INSERT INTO [CanauxNotification] ([Code],[LibelleFr],[LibelleEn],[Ordre],[Actif]) VALUES
 (N'InApp',N'Centre de notifications',N'Notification centre',10,1),(N'Email',N'E-mail',N'Email',20,1);
INSERT INTO [TypesClient] ([Code],[LibelleFr],[LibelleEn],[DescriptionFr],[DescriptionEn],[Ordre],[Actif])
SELECT v.[Code], v.[Fr], v.[En], N'', N'', v.[Ordre], 1
FROM (VALUES (N'Normal',N'Normal (paiement à l''acte)',N'Standard (pay per transaction)',10),
             (N'EnCompte',N'En compte (facturation différée)',N'Account (deferred billing)',20),
             (N'Deposit',N'Deposit (dépôt de garantie)',N'Deposit (guarantee deposit)',30),
             (N'IES',N'IES',N'IES',40)) AS v([Code],[Fr],[En],[Ordre])
WHERE NOT EXISTS (SELECT 1 FROM [TypesClient] t WHERE t.[Code] = v.[Code]);");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UtilisateursSociete_DroitConsultation",
                table: "UtilisateursSociete",
                sql: "[DroitConsultation] IN (N'PropresDemandesUniquement', N'ToutesDemandesSociete')");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Gravite",
                table: "Tickets",
                column: "Gravite");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_MotifCloture",
                table: "Tickets",
                column: "MotifCloture");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tickets_DecisionAvoirExo",
                table: "Tickets",
                sql: "[DecisionAvoirExo] IN (N'Accorde', N'Rejete')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tickets_ObjetEnrolement",
                table: "Tickets",
                sql: "[ObjetEnrolement] IN (N'Chauffeur', N'Camion')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tickets_SensEnrolement",
                table: "Tickets",
                sql: "[SensEnrolement] IN (N'Ajout', N'Retrait')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tickets_Statut",
                table: "Tickets",
                sql: "[Statut] IN (0, 1, 2, 3, 4)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tickets_TypeAnnulationExport",
                table: "Tickets",
                sql: "[TypeAnnulationExport] IN (N'RejetBackOffice')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tickets_TypeAnnulationImport",
                table: "Tickets",
                sql: "[TypeAnnulationImport] IN (N'RejetManifeste')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SuivisBranchesIes_Branche",
                table: "SuivisBranchesIes",
                sql: "[Branche] IN (N'Manifeste', N'ControleDocumentaire', N'BackOffice')");

            migrationBuilder.CreateIndex(
                name: "IX_Societes_Profil",
                table: "Societes",
                column: "Profil");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Societes_Etat",
                table: "Societes",
                sql: "[Etat] IN (N'EnAttenteValidation', N'Active', N'Refusee')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RendezVous_Statut",
                table: "RendezVous",
                sql: "[Statut] IN (N'Reserve', N'Honore', N'Annule', N'NonPresente')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ReglesEscalade_Action",
                table: "ReglesEscalade",
                sql: "[Action] IN (N'NotifierSuperviseur', N'RemonterPriorite', N'Reaffecter')");

            migrationBuilder.CreateIndex(
                name: "IX_ReglesDiffusion_Canal",
                table: "ReglesDiffusion",
                column: "Canal");

            migrationBuilder.CreateIndex(
                name: "IX_PreferencesCanalUtilisateur_Canal",
                table: "PreferencesCanalUtilisateur",
                column: "Canal");

            migrationBuilder.CreateIndex(
                name: "IX_ParametresSla_TypeClient",
                table: "ParametresSla",
                column: "TypeClient");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ParametresMajoration_Type",
                table: "ParametresMajoration",
                sql: "[Type] IN (N'Lourd', N'Dangereux', N'HorsGabarit')");

            migrationBuilder.CreateIndex(
                name: "IX_ModelesNotification_Canal",
                table: "ModelesNotification",
                column: "Canal");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MessagesAssistant_Auteur",
                table: "MessagesAssistant",
                sql: "[Auteur] IN (N'Client', N'Assistant')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Messages_Type",
                table: "Messages",
                sql: "[Type] IN (0, 1)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InvitationsCollaborateur_Etat",
                table: "InvitationsCollaborateur",
                sql: "[Etat] IN (N'EnAttente', N'Acceptee', N'Revoquee')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Files_StrategieActive",
                table: "Files",
                sql: "[StrategieActive] IN (0, 1, 2, 3, 4, 5)");

            migrationBuilder.CreateIndex(
                name: "IX_EnvoisNotification_Canal",
                table: "EnvoisNotification",
                column: "Canal");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EnvoisNotification_Statut",
                table: "EnvoisNotification",
                sql: "[Statut] IN (N'EnAttente', N'Envoye', N'Echec', N'NonDelivrable')");

            migrationBuilder.CreateIndex(
                name: "IX_Dossiers_TypeClient",
                table: "Dossiers",
                column: "TypeClient");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Categorie",
                table: "Documents",
                column: "Categorie");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Documents_StatutAntivirus",
                table: "Documents",
                sql: "[StatutAntivirus] IN (0, 1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Disponibilites_Statut",
                table: "Disponibilites",
                sql: "[Statut] IN (0, 1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChampsFormulaireEnrolement_TypeObjet",
                table: "ChampsFormulaireEnrolement",
                sql: "[TypeObjet] IN (N'Chauffeur', N'Camion')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AspNetUsers_Nature",
                table: "AspNetUsers",
                sql: "[Nature] IN (0, 1)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Alertes_Type",
                table: "Alertes",
                sql: "[Type] IN (0, 1, 2, 3, 4, 5, 6, 7, 8)");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_CategoriesDocument_Categorie",
                table: "Documents",
                column: "Categorie",
                principalTable: "CategoriesDocument",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Dossiers_TypesClient_TypeClient",
                table: "Dossiers",
                column: "TypeClient",
                principalTable: "TypesClient",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EnvoisNotification_CanauxNotification_Canal",
                table: "EnvoisNotification",
                column: "Canal",
                principalTable: "CanauxNotification",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ModelesNotification_CanauxNotification_Canal",
                table: "ModelesNotification",
                column: "Canal",
                principalTable: "CanauxNotification",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ParametresSla_TypesClient_TypeClient",
                table: "ParametresSla",
                column: "TypeClient",
                principalTable: "TypesClient",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PreferencesCanalUtilisateur_CanauxNotification_Canal",
                table: "PreferencesCanalUtilisateur",
                column: "Canal",
                principalTable: "CanauxNotification",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReglesDiffusion_CanauxNotification_Canal",
                table: "ReglesDiffusion",
                column: "Canal",
                principalTable: "CanauxNotification",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Societes_TypesClient_Profil",
                table: "Societes",
                column: "Profil",
                principalTable: "TypesClient",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_GravitesIncident_Gravite",
                table: "Tickets",
                column: "Gravite",
                principalTable: "GravitesIncident",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_MotifsCloture_MotifCloture",
                table: "Tickets",
                column: "MotifCloture",
                principalTable: "MotifsCloture",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_CategoriesDocument_Categorie",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Dossiers_TypesClient_TypeClient",
                table: "Dossiers");

            migrationBuilder.DropForeignKey(
                name: "FK_EnvoisNotification_CanauxNotification_Canal",
                table: "EnvoisNotification");

            migrationBuilder.DropForeignKey(
                name: "FK_ModelesNotification_CanauxNotification_Canal",
                table: "ModelesNotification");

            migrationBuilder.DropForeignKey(
                name: "FK_ParametresSla_TypesClient_TypeClient",
                table: "ParametresSla");

            migrationBuilder.DropForeignKey(
                name: "FK_PreferencesCanalUtilisateur_CanauxNotification_Canal",
                table: "PreferencesCanalUtilisateur");

            migrationBuilder.DropForeignKey(
                name: "FK_ReglesDiffusion_CanauxNotification_Canal",
                table: "ReglesDiffusion");

            migrationBuilder.DropForeignKey(
                name: "FK_Societes_TypesClient_Profil",
                table: "Societes");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_GravitesIncident_Gravite",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_MotifsCloture_MotifCloture",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "CanauxNotification");

            migrationBuilder.DropTable(
                name: "CategoriesDocument");

            migrationBuilder.DropTable(
                name: "GravitesIncident");

            migrationBuilder.DropTable(
                name: "MotifsCloture");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UtilisateursSociete_DroitConsultation",
                table: "UtilisateursSociete");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_Gravite",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_MotifCloture",
                table: "Tickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Tickets_DecisionAvoirExo",
                table: "Tickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Tickets_ObjetEnrolement",
                table: "Tickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Tickets_SensEnrolement",
                table: "Tickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Tickets_Statut",
                table: "Tickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Tickets_TypeAnnulationExport",
                table: "Tickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Tickets_TypeAnnulationImport",
                table: "Tickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SuivisBranchesIes_Branche",
                table: "SuivisBranchesIes");

            migrationBuilder.DropIndex(
                name: "IX_Societes_Profil",
                table: "Societes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Societes_Etat",
                table: "Societes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RendezVous_Statut",
                table: "RendezVous");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ReglesEscalade_Action",
                table: "ReglesEscalade");

            migrationBuilder.DropIndex(
                name: "IX_ReglesDiffusion_Canal",
                table: "ReglesDiffusion");

            migrationBuilder.DropIndex(
                name: "IX_PreferencesCanalUtilisateur_Canal",
                table: "PreferencesCanalUtilisateur");

            migrationBuilder.DropIndex(
                name: "IX_ParametresSla_TypeClient",
                table: "ParametresSla");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ParametresMajoration_Type",
                table: "ParametresMajoration");

            migrationBuilder.DropIndex(
                name: "IX_ModelesNotification_Canal",
                table: "ModelesNotification");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MessagesAssistant_Auteur",
                table: "MessagesAssistant");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Messages_Type",
                table: "Messages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InvitationsCollaborateur_Etat",
                table: "InvitationsCollaborateur");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Files_StrategieActive",
                table: "Files");

            migrationBuilder.DropIndex(
                name: "IX_EnvoisNotification_Canal",
                table: "EnvoisNotification");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EnvoisNotification_Statut",
                table: "EnvoisNotification");

            migrationBuilder.DropIndex(
                name: "IX_Dossiers_TypeClient",
                table: "Dossiers");

            migrationBuilder.DropIndex(
                name: "IX_Documents_Categorie",
                table: "Documents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Documents_StatutAntivirus",
                table: "Documents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Disponibilites_Statut",
                table: "Disponibilites");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChampsFormulaireEnrolement_TypeObjet",
                table: "ChampsFormulaireEnrolement");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AspNetUsers_Nature",
                table: "AspNetUsers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Alertes_Type",
                table: "Alertes");

            // DONNÉES — retour aux entiers avant de rétablir le type int.
            migrationBuilder.Sql(@"
UPDATE [Tickets] SET [MotifCloture] = CASE [MotifCloture] WHEN N'Normale' THEN N'0' WHEN N'AbsenceReponse' THEN N'1' END
WHERE [MotifCloture] IS NOT NULL;
UPDATE [ParametresSla] SET [TypeClient] = CASE [TypeClient] WHEN N'Normal' THEN N'0' WHEN N'EnCompte' THEN N'1' WHEN N'Deposit' THEN N'2' WHEN N'IES' THEN N'3' END
WHERE [TypeClient] IS NOT NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "MotifCloture",
                table: "Tickets",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Gravite",
                table: "Tickets",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TypeClient",
                table: "ParametresSla",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);
        }
    }
}
