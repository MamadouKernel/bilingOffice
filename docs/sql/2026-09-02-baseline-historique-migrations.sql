-- Baseline de l'historique de migrations EF Core.
-- Contexte : la base BillingOffice a ete creee HORS migrations (table __EFMigrationsHistory
-- absente), alors que son schema correspond exactement au modele courant (44 tables = 44).
-- Sans baseline, EF considere TOUTES les migrations comme non appliquees et echoue en
-- tentant de recreer des objets existants (erreur 2714).
-- Ce script est IDEMPOTENT : il ne cree ni ne reinsere ce qui existe deja.

IF OBJECT_ID('__EFMigrationsHistory') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260829061622_SocleInitial')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (N'20260829061622_SocleInitial', N'10.0.11');
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260829063737_AjoutMatriceTransitions')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (N'20260829063737_AjoutMatriceTransitions', N'10.0.11');
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260829064258_AjoutCycleDeVieTicket')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (N'20260829064258_AjoutCycleDeVieTicket', N'10.0.11');
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260829105657_AjoutMessagerieDocuments')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (N'20260829105657_AjoutMessagerieDocuments', N'10.0.11');
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260829110446_AjoutLiaisonSlaAlertes')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (N'20260829110446_AjoutLiaisonSlaAlertes', N'10.0.11');
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260829111035_AjoutDisponibilite')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (N'20260829111035_AjoutDisponibilite', N'10.0.11');
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260829111428_AjoutSupervisionPriorites')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (N'20260829111428_AjoutSupervisionPriorites', N'10.0.11');
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260829164513_RemediationAuditSlaCalendrierCompetenceValueComparer')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (N'20260829164513_RemediationAuditSlaCalendrierCompetenceValueComparer', N'10.0.11');
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260829200147_AjoutAuteurIdDossier')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (N'20260829200147_AjoutAuteurIdDossier', N'10.0.11');
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260829200531_AjoutComptesSocietesUtilisateursRattaches')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (N'20260829200531_AjoutComptesSocietesUtilisateursRattaches', N'10.0.11');
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260829202640_AjoutDemandeurIdSocieteCliente')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (N'20260829202640_AjoutDemandeurIdSocieteCliente', N'10.0.11');
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260829213603_RemediationPostAuditFeature002')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (N'20260829213603_RemediationPostAuditFeature002', N'10.0.11');
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260830055350_ExtensionTicketBillingImport')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (N'20260830055350_ExtensionTicketBillingImport', N'10.0.11');
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260830060458_IndexUniqueFiltreNumeroBl')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (N'20260830060458_IndexUniqueFiltreNumeroBl', N'10.0.11');
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260830060702_AjoutSuiviBrancheIes')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (N'20260830060702_AjoutSuiviBrancheIes', N'10.0.11');
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260830101722_AjoutSupportBillingExport')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (N'20260830101722_AjoutSupportBillingExport', N'10.0.11');
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260830111757_AjoutSupportSimulationProforma')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (N'20260830111757_AjoutSupportSimulationProforma', N'10.0.11');
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260830180721_AjoutSupportCustomerComplaint')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (N'20260830180721_AjoutSupportCustomerComplaint', N'10.0.11');
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260830185846_RemediationCustomerComplaintContent')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (N'20260830185846_RemediationCustomerComplaintContent', N'10.0.11');
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260901053222_AjoutNotifications')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (N'20260901053222_AjoutNotifications', N'10.0.11');
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260901083624_AjoutContinuiteGuichetRendezVous')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (N'20260901083624_AjoutContinuiteGuichetRendezVous', N'10.0.11');
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260901103016_AjoutModuleAvoirExoneration')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (N'20260901103016_AjoutModuleAvoirExoneration', N'10.0.11');
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260901212846_AjouterBrouillonsSaisie')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (N'20260901212846_AjouterBrouillonsSaisie', N'10.0.11');
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260902094053_AjoutPiecesExigeesEtInvitationsCollaborateur')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (N'20260902094053_AjoutPiecesExigeesEtInvitationsCollaborateur', N'10.0.11');
