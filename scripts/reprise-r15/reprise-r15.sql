/* =============================================================================================
   R-15 — REPRISE DE DONNÉES POUR LA NORMALISATION 3FN DES RÉFÉRENTIELS TARIFAIRES
   Migration cible : 20260905033506_NormalisationReferentielsTarifaires

   POURQUOI CE SCRIPT EXISTE
   -------------------------
   La migration EF, telle qu'écrite, N'EST PAS APPLICABLE sur une base contenant des données :

     1. elle SUPPRIME les colonnes texte (`Sens`, `Taille`, `Type`, `CategorieMarchandise`,
        `PartCit/Oic/Paa`, et les scalaires `Parametres_*`) ;
     2. elle AJOUTE les clés étrangères correspondantes en NOT NULL, avec pour valeur par défaut
        `00000000-0000-0000-0000-000000000000` ;
     3. elle CRÉE ensuite les contraintes de clé étrangère.

   Sur une base peuplée, l'étape 3 échoue : aucune ligne de référentiel ne porte l'identifiant
   nul. La transaction est annulée et la base revient à son état antérieur.

   LE RISQUE EST UN ÉCHEC DE MIGRATION, PAS UNE PERTE DE DONNÉES. Rien n'est corrompu ; le
   déploiement est simplement impossible. C'est cette impossibilité que ce script lève.

   CE QUE FAIT CE SCRIPT, ET DANS CET ORDRE
   ----------------------------------------
     A. contrôles préalables — refuse de s'exécuter sur un schéma inattendu ;
     B. crée les tables de référentiel et de paramètres ;
     C. approvisionne les référentiels (Annexe A.6), par CODE et sans jamais réécrire l'existant ;
     D. ajoute les colonnes de clé étrangère en NULLABLE ;
     E. reporte les anciennes valeurs (ordinaux d'énumération et chaînes) vers les clés étrangères ;
     F. VÉRIFIE qu'aucune ligne n'est restée orpheline — et interrompt tout si c'est le cas ;
     G. passe les colonnes en NOT NULL, crée les clés étrangères ET les index ;
     H. supprime les colonnes texte devenues redondantes ;
     I. inscrit la migration comme appliquée dans `__EFMigrationsHistory`.

   L'ORDRE EST LA CORRECTION. La migration d'origine faisait H avant E ; ce script fait E avant H,
   et n'exécute G qu'après avoir prouvé en F que la reprise est complète.

   PROPRIÉTÉS
   ----------
     • TRANSACTIONNEL — une seule transaction ; toute erreur annule l'intégralité.
     • IDEMPOTENT — réexécutable sans effet de bord : chaque étape teste son propre état.
     • JOURNALISÉ — chaque étape écrit dans #JournalReprise, restitué en fin d'exécution.
     • VÉRIFIÉ — comptages avant/après, et arrêt explicite sur toute valeur non résoluble.

   MISE EN GARDE
   -------------
   NE PAS EXÉCUTER DIRECTEMENT EN PRODUCTION. Séquence imposée : restaurer une COPIE de la base de
   production, y exécuter ce script, contrôler le rapport final, puis seulement planifier
   l'exécution réelle avec une sauvegarde immédiatement antérieure.

   RÉPÉTITION EFFECTUÉE. Ce script a été exécuté sur une base LocalDB montée au schéma exact
   d'avant la migration (20260904053850_AjoutPiecesExigeesEtape) et peuplée de données
   représentatives. C'est cette répétition qui a révélé que `Sens`, `Taille` et `Type` étaient des
   ORDINAUX et non du texte — une erreur qu'aucune relecture n'avait vue.

   Il n'a PAS été exécuté sur une copie des données de PRODUCTION : les valeurs réellement
   présentes en base restent à confronter aux correspondances de la section E.
   ============================================================================================= */

SET NOCOUNT ON;
SET XACT_ABORT ON;   -- toute erreur annule la transaction, sans exception silencieuse

DECLARE @MigrationId  nvarchar(150) = N'20260905033506_NormalisationReferentielsTarifaires';
DECLARE @ProduitVersion nvarchar(32) = N'10.0.0';

CREATE TABLE #JournalReprise
(
    Ordre     int IDENTITY(1,1) PRIMARY KEY,
    HorodateUtc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    Etape     nvarchar(16)  NOT NULL,
    Message   nvarchar(400) NOT NULL,
    Lignes    int           NULL
);

BEGIN TRY
    BEGIN TRANSACTION RepriseR15;

    /* ==========================================================================================
       A. CONTRÔLES PRÉALABLES
       Le script refuse de s'exécuter sur un schéma qu'il ne reconnaît pas : mieux vaut ne rien
       faire que reprendre des données dans une forme inattendue.
       ========================================================================================== */

    IF OBJECT_ID(N'dbo.LignesTarifaires', N'U') IS NULL
        THROW 50001, N'R-15 : table LignesTarifaires absente — ce script vise le schéma tarifaire.', 1;

    DECLARE @DejaAppliquee bit =
        CASE WHEN EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = @MigrationId)
             THEN 1 ELSE 0 END;

    INSERT INTO #JournalReprise (Etape, Message, Lignes)
    VALUES (N'A', N'Migration déjà inscrite dans __EFMigrationsHistory : ' +
                  CASE WHEN @DejaAppliquee = 1 THEN N'OUI' ELSE N'NON' END, NULL);

    /* Comptages AVANT — ils servent de témoin au rapport final. */
    DECLARE @Reprises int;
    DECLARE @NbLignesTarifairesAvant int = (SELECT COUNT(*) FROM dbo.LignesTarifaires);
    DECLARE @NbLignesResultatAvant   int =
        CASE WHEN OBJECT_ID(N'dbo.LignesResultatsSimulation', N'U') IS NULL THEN 0
             ELSE (SELECT COUNT(*) FROM dbo.LignesResultatsSimulation) END;
    DECLARE @NbSimulationsAvant      int =
        CASE WHEN OBJECT_ID(N'dbo.SimulationsProforma', N'U') IS NULL THEN 0
             ELSE (SELECT COUNT(*) FROM dbo.SimulationsProforma) END;

    INSERT INTO #JournalReprise (Etape, Message, Lignes)
    VALUES (N'A', N'LignesTarifaires avant reprise',        @NbLignesTarifairesAvant),
           (N'A', N'LignesResultatsSimulation avant reprise', @NbLignesResultatAvant),
           (N'A', N'SimulationsProforma avant reprise',      @NbSimulationsAvant);

    /* ==========================================================================================
       B. TABLES DE RÉFÉRENTIEL
       Créées seulement si absentes : le script doit pouvoir reprendre après une exécution
       partielle, sans jamais écraser une table déjà en place.
       ========================================================================================== */

    IF OBJECT_ID(N'dbo.SensOperations', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.SensOperations
        (
            Id      uniqueidentifier NOT NULL CONSTRAINT PK_SensOperations PRIMARY KEY,
            Code    nvarchar(32)  NOT NULL,
            Libelle nvarchar(100) NOT NULL,
            Ordre   int           NOT NULL,
            Actif   bit           NOT NULL
        );
        INSERT INTO #JournalReprise (Etape, Message) VALUES (N'B', N'Table SensOperations créée.');
    END;

    IF OBJECT_ID(N'dbo.TaillesConteneur', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.TaillesConteneur
        (
            Id      uniqueidentifier NOT NULL CONSTRAINT PK_TaillesConteneur PRIMARY KEY,
            Code    nvarchar(8)  NOT NULL,
            Libelle nvarchar(50) NOT NULL,
            Ordre   int          NOT NULL,
            Actif   bit          NOT NULL
        );
        INSERT INTO #JournalReprise (Etape, Message) VALUES (N'B', N'Table TaillesConteneur créée.');
    END;

    IF OBJECT_ID(N'dbo.TypesConteneur', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.TypesConteneur
        (
            Id      uniqueidentifier NOT NULL CONSTRAINT PK_TypesConteneur PRIMARY KEY,
            Code    nvarchar(16) NOT NULL,
            Libelle nvarchar(50) NOT NULL,
            DeclencheBranchementFrigorifique bit NOT NULL,
            Ordre   int          NOT NULL,
            Actif   bit          NOT NULL
        );
        INSERT INTO #JournalReprise (Etape, Message) VALUES (N'B', N'Table TypesConteneur créée.');
    END;

    IF OBJECT_ID(N'dbo.CategoriesMarchandise', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.CategoriesMarchandise
        (
            Id             uniqueidentifier NOT NULL CONSTRAINT PK_CategoriesMarchandise PRIMARY KEY,
            Code           nvarchar(16)  NOT NULL,
            Libelle        nvarchar(150) NOT NULL,
            SensOperationId uniqueidentifier NOT NULL,
            Actif          bit           NOT NULL,
            CONSTRAINT FK_CategoriesMarchandise_SensOperations_SensOperationId
                FOREIGN KEY (SensOperationId) REFERENCES dbo.SensOperations (Id)
        );
        INSERT INTO #JournalReprise (Etape, Message) VALUES (N'B', N'Table CategoriesMarchandise créée.');
    END;

    /* ==========================================================================================
       C. APPROVISIONNEMENT DES RÉFÉRENTIELS — Annexe A.6 du CDC (SIM-06)
       STRICTEMENT ADDITIF, exactement comme ProvisionnementReferentielsTarifaires : un code déjà
       présent n'est jamais réécrit. Un administrateur ayant renommé « Import local » ou désactivé
       une catégorie conserve son paramétrage.
       Les identifiants sont générés ici ; l'application résout les référentiels PAR CODE, jamais
       par identifiant, de sorte que leur valeur n'a pas à être partagée avec le code applicatif.
       ========================================================================================== */

    MERGE dbo.SensOperations AS cible
    USING (VALUES
        (N'IMPORT_LOCAL',   N'Import local',   10),
        (N'IMPORT_TRANSIT', N'Import transit', 20),
        (N'EXPORT_LOCAL',   N'Export local',   30),
        (N'EXPORT_TRANSIT', N'Export transit', 40)
    ) AS source (Code, Libelle, Ordre)
    ON cible.Code = source.Code
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (Id, Code, Libelle, Ordre, Actif)
        VALUES (NEWID(), source.Code, source.Libelle, source.Ordre, 1);

    INSERT INTO #JournalReprise (Etape, Message, Lignes)
    VALUES (N'C', N'SensOperations approvisionnés', (SELECT COUNT(*) FROM dbo.SensOperations));

    MERGE dbo.TaillesConteneur AS cible
    USING (VALUES
        (N'20', N'20 pieds', 10),
        (N'40', N'40 pieds', 20)
    ) AS source (Code, Libelle, Ordre)
    ON cible.Code = source.Code
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (Id, Code, Libelle, Ordre, Actif)
        VALUES (NEWID(), source.Code, source.Libelle, source.Ordre, 1);

    INSERT INTO #JournalReprise (Etape, Message, Lignes)
    VALUES (N'C', N'TaillesConteneur approvisionnées', (SELECT COUNT(*) FROM dbo.TaillesConteneur));

    MERGE dbo.TypesConteneur AS cible
    USING (VALUES
        (N'DRY',    N'Dry',    CAST(0 AS bit), 10),
        (N'REEFER', N'Reefer', CAST(1 AS bit), 20)
    ) AS source (Code, Libelle, Frigorifique, Ordre)
    ON cible.Code = source.Code
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (Id, Code, Libelle, DeclencheBranchementFrigorifique, Ordre, Actif)
        VALUES (NEWID(), source.Code, source.Libelle, source.Frigorifique, source.Ordre, 1);

    INSERT INTO #JournalReprise (Etape, Message, Lignes)
    VALUES (N'C', N'TypesConteneur approvisionnés', (SELECT COUNT(*) FROM dbo.TypesConteneur));

    /* Les catégories sont scindées par sens : « C1 import » et « C1 export » ne désignent pas le
       même tarif (Annexe A.6). La clé fonctionnelle est donc (SensOperationId, Code). */
    MERGE dbo.CategoriesMarchandise AS cible
    USING (
        SELECT s.Id AS SensOperationId, v.Code, v.Libelle
        FROM (VALUES
            (N'IMPORT_LOCAL',   N'C1', N'Catégorie 1 — import local'),
            (N'IMPORT_LOCAL',   N'C2', N'Catégorie 2 — import local'),
            (N'IMPORT_LOCAL',   N'C3', N'Catégorie 3 — import local'),
            (N'IMPORT_TRANSIT', N'C1', N'Catégorie 1 — import transit'),
            (N'IMPORT_TRANSIT', N'C2', N'Catégorie 2 — import transit'),
            (N'IMPORT_TRANSIT', N'C3', N'Catégorie 3 — import transit'),
            (N'IMPORT_TRANSIT', N'C4', N'Catégorie 4 — import transit'),
            (N'IMPORT_TRANSIT', N'C5', N'Catégorie 5 — import transit'),
            (N'EXPORT_LOCAL',   N'C1', N'Catégorie 1 — export local'),
            (N'EXPORT_LOCAL',   N'C2', N'Catégorie 2 — export local'),
            (N'EXPORT_LOCAL',   N'C3', N'Catégorie 3 — export local'),
            (N'EXPORT_TRANSIT', N'C1', N'Catégorie 1 — export transit'),
            (N'EXPORT_TRANSIT', N'C2', N'Catégorie 2 — export transit'),
            (N'EXPORT_TRANSIT', N'C3', N'Catégorie 3 — export transit'),
            (N'EXPORT_TRANSIT', N'C4', N'Catégorie 4 — export transit'),
            (N'EXPORT_TRANSIT', N'C5', N'Catégorie 5 — export transit')
        ) AS v (SensCode, Code, Libelle)
        INNER JOIN dbo.SensOperations s ON s.Code = v.SensCode
    ) AS source
    ON cible.Code = source.Code AND cible.SensOperationId = source.SensOperationId
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (Id, Code, Libelle, SensOperationId, Actif)
        VALUES (NEWID(), source.Code, source.Libelle, source.SensOperationId, 1);

    INSERT INTO #JournalReprise (Etape, Message, Lignes)
    VALUES (N'C', N'CategoriesMarchandise approvisionnées', (SELECT COUNT(*) FROM dbo.CategoriesMarchandise));

    /* ==========================================================================================
       B2. TABLES DE PARAMÈTRES
       Elles remplacent des colonnes SCALAIRES portées par la grille (`Parametres_*`) et par la
       ligne de résultat (`PartCit/Oic/Paa`). Le passage en tables est ce qui rend le paramétrage
       administrable sans développement (RG-SIM-11) : trois colonnes figées ne se paramètrent pas.
       ========================================================================================== */

    IF OBJECT_ID(N'dbo.ParametresMajoration', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ParametresMajoration
        (
            Id uniqueidentifier NOT NULL CONSTRAINT PK_ParametresMajoration PRIMARY KEY,
            VersionGrilleTarifaireId uniqueidentifier NOT NULL,
            Type  nvarchar(20)  NOT NULL,
            Taux  decimal(5,4)  NOT NULL,
            CONSTRAINT FK_ParametresMajoration_VersionsGrillesTarifaires_VersionGrilleTarifaireId
                FOREIGN KEY (VersionGrilleTarifaireId) REFERENCES dbo.VersionsGrillesTarifaires (Id) ON DELETE CASCADE
        );
        INSERT INTO #JournalReprise (Etape, Message) VALUES (N'B2', N'Table ParametresMajoration creee.');
    END;

    IF OBJECT_ID(N'dbo.QuotesPartsBeneficiaire', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.QuotesPartsBeneficiaire
        (
            Id uniqueidentifier NOT NULL CONSTRAINT PK_QuotesPartsBeneficiaire PRIMARY KEY,
            VersionGrilleTarifaireId uniqueidentifier NOT NULL,
            Beneficiaire nvarchar(16) NOT NULL,
            Pourcentage  decimal(5,4) NOT NULL,
            CONSTRAINT FK_QuotesPartsBeneficiaire_VersionsGrillesTarifaires_VersionGrilleTarifaireId
                FOREIGN KEY (VersionGrilleTarifaireId) REFERENCES dbo.VersionsGrillesTarifaires (Id) ON DELETE CASCADE
        );
        INSERT INTO #JournalReprise (Etape, Message) VALUES (N'B2', N'Table QuotesPartsBeneficiaire creee.');
    END;

    IF OBJECT_ID(N'dbo.TranchesStationnement', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.TranchesStationnement
        (
            Id uniqueidentifier NOT NULL CONSTRAINT PK_TranchesStationnement PRIMARY KEY,
            VersionGrilleTarifaireId uniqueidentifier NOT NULL,
            BorneMinJour int NOT NULL,
            BorneMaxJour int NULL,
            TarifJournalier decimal(18,2) NOT NULL,
            CONSTRAINT FK_TranchesStationnement_VersionsGrillesTarifaires_VersionGrilleTarifaireId
                FOREIGN KEY (VersionGrilleTarifaireId) REFERENCES dbo.VersionsGrillesTarifaires (Id) ON DELETE CASCADE
        );
        INSERT INTO #JournalReprise (Etape, Message) VALUES (N'B2', N'Table TranchesStationnement creee.');
    END;

    IF OBJECT_ID(N'dbo.SeuilsPoidsMajoration', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.SeuilsPoidsMajoration
        (
            Id uniqueidentifier NOT NULL CONSTRAINT PK_SeuilsPoidsMajoration PRIMARY KEY,
            VersionGrilleTarifaireId uniqueidentifier NOT NULL,
            TailleConteneurId uniqueidentifier NOT NULL,
            SeuilPoidsTonnes decimal(18,2) NOT NULL,
            CONSTRAINT FK_SeuilsPoidsMajoration_VersionsGrillesTarifaires_VersionGrilleTarifaireId
                FOREIGN KEY (VersionGrilleTarifaireId) REFERENCES dbo.VersionsGrillesTarifaires (Id) ON DELETE CASCADE,
            CONSTRAINT FK_SeuilsPoidsMajoration_TaillesConteneur_TailleConteneurId
                FOREIGN KEY (TailleConteneurId) REFERENCES dbo.TaillesConteneur (Id)
        );
        INSERT INTO #JournalReprise (Etape, Message) VALUES (N'B2', N'Table SeuilsPoidsMajoration creee.');
    END;

    IF OBJECT_ID(N'dbo.FranchisesStationnement', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.FranchisesStationnement
        (
            Id uniqueidentifier NOT NULL CONSTRAINT PK_FranchisesStationnement PRIMARY KEY,
            VersionGrilleTarifaireId uniqueidentifier NOT NULL,
            SensOperationId uniqueidentifier NOT NULL,
            NombreJours int NOT NULL,
            CONSTRAINT FK_FranchisesStationnement_VersionsGrillesTarifaires_VersionGrilleTarifaireId
                FOREIGN KEY (VersionGrilleTarifaireId) REFERENCES dbo.VersionsGrillesTarifaires (Id) ON DELETE CASCADE,
            CONSTRAINT FK_FranchisesStationnement_SensOperations_SensOperationId
                FOREIGN KEY (SensOperationId) REFERENCES dbo.SensOperations (Id)
        );
        INSERT INTO #JournalReprise (Etape, Message) VALUES (N'B2', N'Table FranchisesStationnement creee.');
    END;

    IF OBJECT_ID(N'dbo.PartsStationnementSimulees', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PartsStationnementSimulees
        (
            Id uniqueidentifier NOT NULL CONSTRAINT PK_PartsStationnementSimulees PRIMARY KEY,
            LigneResultatSimulationConteneurId uniqueidentifier NOT NULL,
            Beneficiaire nvarchar(16) NOT NULL,
            Montant decimal(18,2) NOT NULL,
            CONSTRAINT FK_PartsStationnementSimulees_LignesResultatsSimulation_LigneResultatSimulationConteneurId
                FOREIGN KEY (LigneResultatSimulationConteneurId) REFERENCES dbo.LignesResultatsSimulation (Id) ON DELETE CASCADE
        );
        INSERT INTO #JournalReprise (Etape, Message) VALUES (N'B2', N'Table PartsStationnementSimulees creee.');
    END;

    /* ==========================================================================================
       C2. REPRISE DES PARAMÈTRES DE GRILLE
       Chaque insertion est conditionnée à l'absence de la ligne correspondante : le script reste
       réexécutable. Les bornes de tranche (1-5, 6-15, 16+) sont celles de l'Annexe A.8 — elles
       n'existaient nulle part en base, les trois colonnes ne portant que les tarifs.
       ========================================================================================== */

    IF COL_LENGTH(N'dbo.VersionsGrillesTarifaires', N'Parametres_TauxMajorationLourd') IS NOT NULL
    BEGIN
        EXEC sp_executesql N'
            INSERT INTO dbo.ParametresMajoration (Id, VersionGrilleTarifaireId, Type, Taux)
            SELECT NEWID(), v.Id, m.Type, m.Taux
            FROM dbo.VersionsGrillesTarifaires v
            CROSS APPLY (VALUES
                (N''Lourd'',       v.Parametres_TauxMajorationLourd),
                (N''Dangereux'',   v.Parametres_TauxMajorationDangereux),
                (N''HorsGabarit'', v.Parametres_TauxMajorationOog)
            ) AS m (Type, Taux)
            WHERE NOT EXISTS (SELECT 1 FROM dbo.ParametresMajoration p
                              WHERE p.VersionGrilleTarifaireId = v.Id AND p.Type = m.Type);';
        SET @Reprises = @@ROWCOUNT;
        INSERT INTO #JournalReprise (Etape, Message, Lignes) VALUES (N'C2', N'Taux de majoration repris', @Reprises);

        EXEC sp_executesql N'
            INSERT INTO dbo.QuotesPartsBeneficiaire (Id, VersionGrilleTarifaireId, Beneficiaire, Pourcentage)
            SELECT NEWID(), v.Id, q.Beneficiaire, q.Pourcentage
            FROM dbo.VersionsGrillesTarifaires v
            CROSS APPLY (VALUES
                (N''CIT'', v.Parametres_QuotePartCit),
                (N''OIC'', v.Parametres_QuotePartOic),
                (N''PAA'', v.Parametres_QuotePartPaa)
            ) AS q (Beneficiaire, Pourcentage)
            WHERE NOT EXISTS (SELECT 1 FROM dbo.QuotesPartsBeneficiaire x
                              WHERE x.VersionGrilleTarifaireId = v.Id AND x.Beneficiaire = q.Beneficiaire);';
        SET @Reprises = @@ROWCOUNT;
        INSERT INTO #JournalReprise (Etape, Message, Lignes) VALUES (N'C2', N'Quotes-parts beneficiaires reprises', @Reprises);

        EXEC sp_executesql N'
            INSERT INTO dbo.TranchesStationnement (Id, VersionGrilleTarifaireId, BorneMinJour, BorneMaxJour, TarifJournalier)
            SELECT NEWID(), v.Id, t.BorneMin, t.BorneMax, t.Tarif
            FROM dbo.VersionsGrillesTarifaires v
            CROSS APPLY (VALUES
                (1,  5,                  v.Parametres_TarifStationnementTranche1Jour),
                (6,  15,                 v.Parametres_TarifStationnementTranche2Jour),
                (16, CAST(NULL AS int),  v.Parametres_TarifStationnementTranche3Jour)
            ) AS t (BorneMin, BorneMax, Tarif)
            WHERE NOT EXISTS (SELECT 1 FROM dbo.TranchesStationnement x
                              WHERE x.VersionGrilleTarifaireId = v.Id AND x.BorneMinJour = t.BorneMin);';
        SET @Reprises = @@ROWCOUNT;
        INSERT INTO #JournalReprise (Etape, Message, Lignes) VALUES (N'C2', N'Tranches de stationnement reprises', @Reprises);

        EXEC sp_executesql N'
            INSERT INTO dbo.SeuilsPoidsMajoration (Id, VersionGrilleTarifaireId, TailleConteneurId, SeuilPoidsTonnes)
            SELECT NEWID(), v.Id, tc.Id, s.Seuil
            FROM dbo.VersionsGrillesTarifaires v
            CROSS APPLY (VALUES
                (N''20'', v.Parametres_SeuilPoidsLourd20Pieds),
                (N''40'', v.Parametres_SeuilPoidsLourd40Pieds)
            ) AS s (CodeTaille, Seuil)
            INNER JOIN dbo.TaillesConteneur tc ON tc.Code = s.CodeTaille
            WHERE NOT EXISTS (SELECT 1 FROM dbo.SeuilsPoidsMajoration x
                              WHERE x.VersionGrilleTarifaireId = v.Id AND x.TailleConteneurId = tc.Id);';
        SET @Reprises = @@ROWCOUNT;
        INSERT INTO #JournalReprise (Etape, Message, Lignes) VALUES (N'C2', N'Seuils de poids repris', @Reprises);

        /* Seuls IMPORT_LOCAL et IMPORT_TRANSIT portaient une franchise : les colonnes d'origine
           n'en prevoyaient aucune a l'export. Ne rien inventer pour les deux autres sens. */
        EXEC sp_executesql N'
            INSERT INTO dbo.FranchisesStationnement (Id, VersionGrilleTarifaireId, SensOperationId, NombreJours)
            SELECT NEWID(), v.Id, so.Id, f.Jours
            FROM dbo.VersionsGrillesTarifaires v
            CROSS APPLY (VALUES
                (N''IMPORT_LOCAL'',   v.Parametres_FranchiseStationnementImportLocalJours),
                (N''IMPORT_TRANSIT'', v.Parametres_FranchiseStationnementImportTransitJours)
            ) AS f (CodeSens, Jours)
            INNER JOIN dbo.SensOperations so ON so.Code = f.CodeSens
            WHERE NOT EXISTS (SELECT 1 FROM dbo.FranchisesStationnement x
                              WHERE x.VersionGrilleTarifaireId = v.Id AND x.SensOperationId = so.Id);';
        SET @Reprises = @@ROWCOUNT;
        INSERT INTO #JournalReprise (Etape, Message, Lignes) VALUES (N'C2', N'Franchises de stationnement reprises', @Reprises);
    END;

    IF COL_LENGTH(N'dbo.LignesResultatsSimulation', N'PartCit') IS NOT NULL
    BEGIN
        EXEC sp_executesql N'
            INSERT INTO dbo.PartsStationnementSimulees (Id, LigneResultatSimulationConteneurId, Beneficiaire, Montant)
            SELECT NEWID(), lr.Id, p.Beneficiaire, p.Montant
            FROM dbo.LignesResultatsSimulation lr
            CROSS APPLY (VALUES
                (N''CIT'', lr.PartCit),
                (N''OIC'', lr.PartOic),
                (N''PAA'', lr.PartPaa)
            ) AS p (Beneficiaire, Montant)
            WHERE NOT EXISTS (SELECT 1 FROM dbo.PartsStationnementSimulees x
                              WHERE x.LigneResultatSimulationConteneurId = lr.Id AND x.Beneficiaire = p.Beneficiaire);';
        SET @Reprises = @@ROWCOUNT;
        INSERT INTO #JournalReprise (Etape, Message, Lignes) VALUES (N'C2', N'Parts de stationnement simulees reprises', @Reprises);
    END;

    /* ==========================================================================================
       D. COLONNES DE CLÉ ÉTRANGÈRE — AJOUTÉES EN **NULLABLE**
       C'est le point où la migration d'origine se rend impossible : elle les ajoute en NOT NULL
       avec une valeur par défaut qui ne référence rien. Ici elles restent nullables jusqu'à ce que
       la reprise (E) soit faite et vérifiée (F).
       ========================================================================================== */

    IF COL_LENGTH(N'dbo.LignesTarifaires', N'SensOperationId') IS NULL
        ALTER TABLE dbo.LignesTarifaires ADD SensOperationId uniqueidentifier NULL;
    IF COL_LENGTH(N'dbo.LignesTarifaires', N'TailleConteneurId') IS NULL
        ALTER TABLE dbo.LignesTarifaires ADD TailleConteneurId uniqueidentifier NULL;
    IF COL_LENGTH(N'dbo.LignesTarifaires', N'CategorieMarchandiseId') IS NULL
        ALTER TABLE dbo.LignesTarifaires ADD CategorieMarchandiseId uniqueidentifier NULL;

    IF COL_LENGTH(N'dbo.LignesResultatsSimulation', N'TailleConteneurId') IS NULL
        ALTER TABLE dbo.LignesResultatsSimulation ADD TailleConteneurId uniqueidentifier NULL;
    IF COL_LENGTH(N'dbo.LignesResultatsSimulation', N'TypeConteneurId') IS NULL
        ALTER TABLE dbo.LignesResultatsSimulation ADD TypeConteneurId uniqueidentifier NULL;
    IF COL_LENGTH(N'dbo.LignesResultatsSimulation', N'CategorieMarchandiseId') IS NULL
        ALTER TABLE dbo.LignesResultatsSimulation ADD CategorieMarchandiseId uniqueidentifier NULL;

    IF COL_LENGTH(N'dbo.SimulationsProforma', N'SensOperationId') IS NULL
        ALTER TABLE dbo.SimulationsProforma ADD SensOperationId uniqueidentifier NULL;

    INSERT INTO #JournalReprise (Etape, Message) VALUES (N'D', N'Colonnes de clé étrangère ajoutées en NULLABLE.');

    /* À PARTIR D'ICI, TOUTE INSTRUCTION TOUCHANT CES COLONNES PASSE EN SQL DYNAMIQUE.
       T-SQL compile le lot entier avant de l'exécuter : les colonnes qui viennent d'être ajoutées
       n'existent pas encore pour l'analyseur, et une référence directe échouerait à la compilation
       — avant même que la moindre ligne soit reprise. C'est ce que la répétition a mis au jour. */

    /* ==========================================================================================
       E. REPORT DES ANCIENNES VALEURS VERS LES CLÉS ÉTRANGÈRES

       CE POINT A ÉTÉ CORRIGÉ PAR LA RÉPÉTITION. Une première version de ce script comparait ces
       colonnes à des CODES TEXTE. C'était faux : sur le schéma d'origine,

           LignesTarifaires.Sens                  int   (SensTrafic)
           LignesTarifaires.Taille                int   (TailleConteneur)
           LignesResultatsSimulation.Taille       int   (TailleConteneur)
           LignesResultatsSimulation.Type         int   (TypeConteneur)
           SimulationsProforma.Sens               int   (SensTrafic)
           *.CategorieMarchandise                 nvarchar(64)   ← seule vraie chaîne

       Les trois premières sont des ORDINAUX d'énumération, sérialisés en entier par EF. La
       correspondance est donc établie par ordinal, à partir des définitions d'origine :

           SensTrafic       : ImportLocal = 1, ImportTransit = 2, ExportLocal = 3, ExportTransit = 4
           TailleConteneur  : VingtPieds = 20, QuarantePieds = 40
           TypeConteneur    : Dry = 1, Reefer = 2

       Ces valeurs sont écrites ici en clair, et non déduites : un ordinal mal deviné rattacherait
       une ligne tarifaire au mauvais sens, et produirait un prix faux sans rien signaler.
       ========================================================================================== */

    /* Table de correspondance ordinal → code, matérialisée pour être lisible et vérifiable. */
    /* Tables TEMPORAIRES et non variables table : les reports ci-dessous s'exécutent en SQL
       dynamique — voir la note de la section D — et une variable table n'y serait pas visible. */
    CREATE TABLE #MapSens   (Ordinal int PRIMARY KEY, Code nvarchar(32));
    CREATE TABLE #MapTaille (Ordinal int PRIMARY KEY, Code nvarchar(8));
    CREATE TABLE #MapType   (Ordinal int PRIMARY KEY, Code nvarchar(16));

    INSERT INTO #MapSens (Ordinal, Code) VALUES
        (1, N'IMPORT_LOCAL'), (2, N'IMPORT_TRANSIT'), (3, N'EXPORT_LOCAL'), (4, N'EXPORT_TRANSIT');
    INSERT INTO #MapTaille (Ordinal, Code) VALUES (20, N'20'), (40, N'40');
    INSERT INTO #MapType (Ordinal, Code) VALUES (1, N'DRY'), (2, N'REEFER');

    IF COL_LENGTH(N'dbo.LignesTarifaires', N'Sens') IS NOT NULL
    BEGIN
        EXEC sp_executesql N'
            UPDATE lt SET lt.SensOperationId = s.Id
            FROM dbo.LignesTarifaires lt
            INNER JOIN #MapSens m ON m.Ordinal = lt.Sens
            INNER JOIN dbo.SensOperations s ON s.Code = m.Code
            WHERE lt.SensOperationId IS NULL;';
        SET @Reprises = @@ROWCOUNT;
        INSERT INTO #JournalReprise (Etape, Message, Lignes) VALUES (N'E', N'LignesTarifaires.Sens → SensOperationId', @Reprises);
    END;

    IF COL_LENGTH(N'dbo.LignesTarifaires', N'Taille') IS NOT NULL
    BEGIN
        EXEC sp_executesql N'
            UPDATE lt SET lt.TailleConteneurId = t.Id
            FROM dbo.LignesTarifaires lt
            INNER JOIN #MapTaille m ON m.Ordinal = lt.Taille
            INNER JOIN dbo.TaillesConteneur t ON t.Code = m.Code
            WHERE lt.TailleConteneurId IS NULL;';
        SET @Reprises = @@ROWCOUNT;
        INSERT INTO #JournalReprise (Etape, Message, Lignes) VALUES (N'E', N'LignesTarifaires.Taille → TailleConteneurId', @Reprises);
    END;

    /* La catégorie se résout AVEC le sens : « C1 » seul est ambigu (Annexe A.6). */
    IF COL_LENGTH(N'dbo.LignesTarifaires', N'CategorieMarchandise') IS NOT NULL
    BEGIN
        EXEC sp_executesql N'
            UPDATE lt SET lt.CategorieMarchandiseId = c.Id
            FROM dbo.LignesTarifaires lt
            INNER JOIN dbo.CategoriesMarchandise c
                ON UPPER(LTRIM(RTRIM(lt.CategorieMarchandise))) = c.Code
               AND c.SensOperationId = lt.SensOperationId
            WHERE lt.CategorieMarchandiseId IS NULL;';
        SET @Reprises = @@ROWCOUNT;
        INSERT INTO #JournalReprise (Etape, Message, Lignes) VALUES (N'E', N'LignesTarifaires.CategorieMarchandise → CategorieMarchandiseId', @Reprises);
    END;

    IF COL_LENGTH(N'dbo.SimulationsProforma', N'Sens') IS NOT NULL
    BEGIN
        EXEC sp_executesql N'
            UPDATE sp SET sp.SensOperationId = s.Id
            FROM dbo.SimulationsProforma sp
            INNER JOIN #MapSens m ON m.Ordinal = sp.Sens
            INNER JOIN dbo.SensOperations s ON s.Code = m.Code
            WHERE sp.SensOperationId IS NULL;';
        SET @Reprises = @@ROWCOUNT;
        INSERT INTO #JournalReprise (Etape, Message, Lignes) VALUES (N'E', N'SimulationsProforma.Sens → SensOperationId', @Reprises);
    END;

    IF COL_LENGTH(N'dbo.LignesResultatsSimulation', N'Taille') IS NOT NULL
    BEGIN
        EXEC sp_executesql N'
            UPDATE lr SET lr.TailleConteneurId = t.Id
            FROM dbo.LignesResultatsSimulation lr
            INNER JOIN #MapTaille m ON m.Ordinal = lr.Taille
            INNER JOIN dbo.TaillesConteneur t ON t.Code = m.Code
            WHERE lr.TailleConteneurId IS NULL;';
        SET @Reprises = @@ROWCOUNT;
        INSERT INTO #JournalReprise (Etape, Message, Lignes) VALUES (N'E', N'LignesResultatsSimulation.Taille → TailleConteneurId', @Reprises);
    END;

    IF COL_LENGTH(N'dbo.LignesResultatsSimulation', N'Type') IS NOT NULL
    BEGIN
        EXEC sp_executesql N'
            UPDATE lr SET lr.TypeConteneurId = ty.Id
            FROM dbo.LignesResultatsSimulation lr
            INNER JOIN #MapType m ON m.Ordinal = lr.Type
            INNER JOIN dbo.TypesConteneur ty ON ty.Code = m.Code
            WHERE lr.TypeConteneurId IS NULL;';
        SET @Reprises = @@ROWCOUNT;
        INSERT INTO #JournalReprise (Etape, Message, Lignes) VALUES (N'E', N'LignesResultatsSimulation.Type → TypeConteneurId', @Reprises);
    END;

    /* La catégorie d'une ligne de résultat se résout par le sens de SA simulation. */
    IF COL_LENGTH(N'dbo.LignesResultatsSimulation', N'CategorieMarchandise') IS NOT NULL
    BEGIN
        EXEC sp_executesql N'
            UPDATE lr SET lr.CategorieMarchandiseId = c.Id
            FROM dbo.LignesResultatsSimulation lr
            INNER JOIN dbo.SimulationsProforma sp ON sp.Id = lr.SimulationProformaId
            INNER JOIN dbo.CategoriesMarchandise c
                ON UPPER(LTRIM(RTRIM(lr.CategorieMarchandise))) = c.Code
               AND c.SensOperationId = sp.SensOperationId
            WHERE lr.CategorieMarchandiseId IS NULL;';
        SET @Reprises = @@ROWCOUNT;
        INSERT INTO #JournalReprise (Etape, Message, Lignes) VALUES (N'E', N'LignesResultatsSimulation.CategorieMarchandise → CategorieMarchandiseId', @Reprises);
    END;

    /* ==========================================================================================
       F. VÉRIFICATION — LE POINT DE NON-RETOUR
       Aucune contrainte n'est posée tant qu'une seule ligne reste non résolue. Le script préfère
       s'interrompre et laisser la base intacte plutôt que d'inventer une valeur de repli : une
       ligne tarifaire rattachée à la mauvaise catégorie produirait un prix faux, silencieusement.
       ========================================================================================== */

    DECLARE @Orphelines int = 0;

    /* Comptages en SQL dynamique, pour la même raison qu'en E. */
    EXEC sp_executesql
        N'SELECT @n = (SELECT COUNT(*) FROM dbo.LignesTarifaires
                       WHERE SensOperationId IS NULL OR TailleConteneurId IS NULL OR CategorieMarchandiseId IS NULL)
                    + (SELECT COUNT(*) FROM dbo.SimulationsProforma WHERE SensOperationId IS NULL)
                    + (SELECT COUNT(*) FROM dbo.LignesResultatsSimulation
                       WHERE TailleConteneurId IS NULL OR TypeConteneurId IS NULL OR CategorieMarchandiseId IS NULL);',
        N'@n int OUTPUT', @n = @Orphelines OUTPUT;

    INSERT INTO #JournalReprise (Etape, Message, Lignes)
    VALUES (N'F', N'Lignes non résolues après reprise', @Orphelines);

    IF @Orphelines > 0
    BEGIN
        /* Le rapport est rendu AVANT l'annulation : sans lui, l'exploitant n'aurait aucune trace
           de ce qui a échoué. */
        SELECT * FROM #JournalReprise ORDER BY Ordre;

        EXEC sp_executesql N'
            SELECT TOP (200)
                N''LignesTarifaires'' AS TableConcernee, Id,
                Sens AS SensOrdinal, Taille AS TailleOrdinal, CategorieMarchandise
            FROM dbo.LignesTarifaires
            WHERE SensOperationId IS NULL OR TailleConteneurId IS NULL OR CategorieMarchandiseId IS NULL;';

        THROW 50015, N'R-15 : reprise INCOMPLÈTE — des valeurs d''origine ne correspondent à aucun code de référentiel. Aucune contrainte n''a été posée, la base est inchangée. Corriger les valeurs listées puis relancer.', 1;
    END;

    /* ==========================================================================================
       G. CONTRAINTES — seulement maintenant
       ========================================================================================== */

    /* Passage en NOT NULL puis pose des clés étrangères — en SQL dynamique, ces colonnes n'étant
       pas connues de l'analyseur au moment où il compile ce lot. */
    EXEC sp_executesql N'
        ALTER TABLE dbo.LignesTarifaires ALTER COLUMN SensOperationId uniqueidentifier NOT NULL;';
    EXEC sp_executesql N'
        ALTER TABLE dbo.LignesTarifaires ALTER COLUMN TailleConteneurId uniqueidentifier NOT NULL;';
    EXEC sp_executesql N'
        ALTER TABLE dbo.LignesTarifaires ALTER COLUMN CategorieMarchandiseId uniqueidentifier NOT NULL;';
    EXEC sp_executesql N'
        ALTER TABLE dbo.SimulationsProforma ALTER COLUMN SensOperationId uniqueidentifier NOT NULL;';
    EXEC sp_executesql N'
        ALTER TABLE dbo.LignesResultatsSimulation ALTER COLUMN TailleConteneurId uniqueidentifier NOT NULL;';
    EXEC sp_executesql N'
        ALTER TABLE dbo.LignesResultatsSimulation ALTER COLUMN TypeConteneurId uniqueidentifier NOT NULL;';
    EXEC sp_executesql N'
        ALTER TABLE dbo.LignesResultatsSimulation ALTER COLUMN CategorieMarchandiseId uniqueidentifier NOT NULL;';

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_LignesTarifaires_SensOperations_SensOperationId')
        EXEC sp_executesql N'ALTER TABLE dbo.LignesTarifaires ADD CONSTRAINT FK_LignesTarifaires_SensOperations_SensOperationId FOREIGN KEY (SensOperationId) REFERENCES dbo.SensOperations (Id);';
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_LignesTarifaires_TaillesConteneur_TailleConteneurId')
        EXEC sp_executesql N'ALTER TABLE dbo.LignesTarifaires ADD CONSTRAINT FK_LignesTarifaires_TaillesConteneur_TailleConteneurId FOREIGN KEY (TailleConteneurId) REFERENCES dbo.TaillesConteneur (Id);';
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_LignesTarifaires_CategoriesMarchandise_CategorieMarchandiseId')
        EXEC sp_executesql N'ALTER TABLE dbo.LignesTarifaires ADD CONSTRAINT FK_LignesTarifaires_CategoriesMarchandise_CategorieMarchandiseId FOREIGN KEY (CategorieMarchandiseId) REFERENCES dbo.CategoriesMarchandise (Id);';
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SimulationsProforma_SensOperations_SensOperationId')
        EXEC sp_executesql N'ALTER TABLE dbo.SimulationsProforma ADD CONSTRAINT FK_SimulationsProforma_SensOperations_SensOperationId FOREIGN KEY (SensOperationId) REFERENCES dbo.SensOperations (Id);';
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_LignesResultatsSimulation_TaillesConteneur_TailleConteneurId')
        EXEC sp_executesql N'ALTER TABLE dbo.LignesResultatsSimulation ADD CONSTRAINT FK_LignesResultatsSimulation_TaillesConteneur_TailleConteneurId FOREIGN KEY (TailleConteneurId) REFERENCES dbo.TaillesConteneur (Id);';
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_LignesResultatsSimulation_TypesConteneur_TypeConteneurId')
        EXEC sp_executesql N'ALTER TABLE dbo.LignesResultatsSimulation ADD CONSTRAINT FK_LignesResultatsSimulation_TypesConteneur_TypeConteneurId FOREIGN KEY (TypeConteneurId) REFERENCES dbo.TypesConteneur (Id);';
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_LignesResultatsSimulation_CategoriesMarchandise_CategorieMarchandiseId')
        EXEC sp_executesql N'ALTER TABLE dbo.LignesResultatsSimulation ADD CONSTRAINT FK_LignesResultatsSimulation_CategoriesMarchandise_CategorieMarchandiseId FOREIGN KEY (CategorieMarchandiseId) REFERENCES dbo.CategoriesMarchandise (Id);';

    /* ------------------------------------------------------------------------------------------
       INDEX. L'index d'unicité métier — une seule ligne tarifaire par (grille, sens, taille,
       catégorie) — est le seul qui porte une RÈGLE : le perdre autoriserait deux tarifs
       contradictoires pour la même clé. Il est donc recréé sur les nouvelles colonnes, et non
       simplement supprimé avec les anciennes.
       ------------------------------------------------------------------------------------------ */

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SensOperations_Code')
        CREATE UNIQUE INDEX IX_SensOperations_Code ON dbo.SensOperations (Code);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TaillesConteneur_Code')
        CREATE UNIQUE INDEX IX_TaillesConteneur_Code ON dbo.TaillesConteneur (Code);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TypesConteneur_Code')
        CREATE UNIQUE INDEX IX_TypesConteneur_Code ON dbo.TypesConteneur (Code);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CategoriesMarchandise_Code_SensOperationId')
        CREATE UNIQUE INDEX IX_CategoriesMarchandise_Code_SensOperationId ON dbo.CategoriesMarchandise (Code, SensOperationId);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CategoriesMarchandise_SensOperationId')
        CREATE INDEX IX_CategoriesMarchandise_SensOperationId ON dbo.CategoriesMarchandise (SensOperationId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ParametresMajoration_VersionGrilleTarifaireId_Type')
        CREATE UNIQUE INDEX IX_ParametresMajoration_VersionGrilleTarifaireId_Type ON dbo.ParametresMajoration (VersionGrilleTarifaireId, Type);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_QuotesPartsBeneficiaire_VersionGrilleTarifaireId_Beneficiaire')
        CREATE UNIQUE INDEX IX_QuotesPartsBeneficiaire_VersionGrilleTarifaireId_Beneficiaire ON dbo.QuotesPartsBeneficiaire (VersionGrilleTarifaireId, Beneficiaire);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FranchisesStationnement_VersionGrilleTarifaireId_SensOperationId')
        CREATE UNIQUE INDEX IX_FranchisesStationnement_VersionGrilleTarifaireId_SensOperationId ON dbo.FranchisesStationnement (VersionGrilleTarifaireId, SensOperationId);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FranchisesStationnement_SensOperationId')
        CREATE INDEX IX_FranchisesStationnement_SensOperationId ON dbo.FranchisesStationnement (SensOperationId);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SeuilsPoidsMajoration_VersionGrilleTarifaireId_TailleConteneurId')
        CREATE UNIQUE INDEX IX_SeuilsPoidsMajoration_VersionGrilleTarifaireId_TailleConteneurId ON dbo.SeuilsPoidsMajoration (VersionGrilleTarifaireId, TailleConteneurId);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SeuilsPoidsMajoration_TailleConteneurId')
        CREATE INDEX IX_SeuilsPoidsMajoration_TailleConteneurId ON dbo.SeuilsPoidsMajoration (TailleConteneurId);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TranchesStationnement_VersionGrilleTarifaireId')
        CREATE INDEX IX_TranchesStationnement_VersionGrilleTarifaireId ON dbo.TranchesStationnement (VersionGrilleTarifaireId);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PartsStationnementSimulees_LigneResultatSimulationConteneurId_Beneficiaire')
        CREATE UNIQUE INDEX IX_PartsStationnementSimulees_LigneResultatSimulationConteneurId_Beneficiaire ON dbo.PartsStationnementSimulees (LigneResultatSimulationConteneurId, Beneficiaire);

    /* Index portant sur les colonnes NOUVELLES : SQL dynamique, même raison qu'en E et G. */
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LignesTarifaires_SensOperationId')
        EXEC sp_executesql N'CREATE INDEX IX_LignesTarifaires_SensOperationId ON dbo.LignesTarifaires (SensOperationId);';
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LignesTarifaires_TailleConteneurId')
        EXEC sp_executesql N'CREATE INDEX IX_LignesTarifaires_TailleConteneurId ON dbo.LignesTarifaires (TailleConteneurId);';
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LignesTarifaires_CategorieMarchandiseId')
        EXEC sp_executesql N'CREATE INDEX IX_LignesTarifaires_CategorieMarchandiseId ON dbo.LignesTarifaires (CategorieMarchandiseId);';
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LignesTarifaires_VersionGrilleTarifaireId_SensOperationId_TailleConteneurId_CategorieMarchandiseId')
        EXEC sp_executesql N'CREATE UNIQUE INDEX IX_LignesTarifaires_VersionGrilleTarifaireId_SensOperationId_TailleConteneurId_CategorieMarchandiseId ON dbo.LignesTarifaires (VersionGrilleTarifaireId, SensOperationId, TailleConteneurId, CategorieMarchandiseId);';

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SimulationsProforma_SensOperationId')
        EXEC sp_executesql N'CREATE INDEX IX_SimulationsProforma_SensOperationId ON dbo.SimulationsProforma (SensOperationId);';
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LignesResultatsSimulation_TailleConteneurId')
        EXEC sp_executesql N'CREATE INDEX IX_LignesResultatsSimulation_TailleConteneurId ON dbo.LignesResultatsSimulation (TailleConteneurId);';
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LignesResultatsSimulation_TypeConteneurId')
        EXEC sp_executesql N'CREATE INDEX IX_LignesResultatsSimulation_TypeConteneurId ON dbo.LignesResultatsSimulation (TypeConteneurId);';
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LignesResultatsSimulation_CategorieMarchandiseId')
        EXEC sp_executesql N'CREATE INDEX IX_LignesResultatsSimulation_CategorieMarchandiseId ON dbo.LignesResultatsSimulation (CategorieMarchandiseId);';

    INSERT INTO #JournalReprise (Etape, Message) VALUES (N'G', N'Colonnes passées en NOT NULL, clés étrangères et index créés.');

    /* ==========================================================================================
       H. SUPPRESSION DES COLONNES TEXTE — après la reprise, jamais avant
       ========================================================================================== */

    /* L'ancien index d'unicité porte sur Sens/Taille/CategorieMarchandise : il empêche leur
       suppression. Son équivalent sur les nouvelles clés a été créé en G — la règle d'unicité
       n'est donc à aucun moment absente. */
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LignesTarifaires_VersionGrilleTarifaireId_Sens_Taille_CategorieMarchandise')
        DROP INDEX IX_LignesTarifaires_VersionGrilleTarifaireId_Sens_Taille_CategorieMarchandise ON dbo.LignesTarifaires;

    IF COL_LENGTH(N'dbo.LignesTarifaires', N'Sens') IS NOT NULL                ALTER TABLE dbo.LignesTarifaires DROP COLUMN Sens;
    IF COL_LENGTH(N'dbo.LignesTarifaires', N'Taille') IS NOT NULL              ALTER TABLE dbo.LignesTarifaires DROP COLUMN Taille;
    IF COL_LENGTH(N'dbo.LignesTarifaires', N'CategorieMarchandise') IS NOT NULL ALTER TABLE dbo.LignesTarifaires DROP COLUMN CategorieMarchandise;
    IF COL_LENGTH(N'dbo.SimulationsProforma', N'Sens') IS NOT NULL             ALTER TABLE dbo.SimulationsProforma DROP COLUMN Sens;
    IF COL_LENGTH(N'dbo.LignesResultatsSimulation', N'Taille') IS NOT NULL     ALTER TABLE dbo.LignesResultatsSimulation DROP COLUMN Taille;
    IF COL_LENGTH(N'dbo.LignesResultatsSimulation', N'Type') IS NOT NULL       ALTER TABLE dbo.LignesResultatsSimulation DROP COLUMN Type;
    IF COL_LENGTH(N'dbo.LignesResultatsSimulation', N'CategorieMarchandise') IS NOT NULL ALTER TABLE dbo.LignesResultatsSimulation DROP COLUMN CategorieMarchandise;

    DECLARE @ColonnesScalaires TABLE (Nom sysname);
    INSERT INTO @ColonnesScalaires (Nom) VALUES
        (N'Parametres_TauxMajorationLourd'), (N'Parametres_TauxMajorationDangereux'), (N'Parametres_TauxMajorationOog'),
        (N'Parametres_QuotePartCit'), (N'Parametres_QuotePartOic'), (N'Parametres_QuotePartPaa'),
        (N'Parametres_TarifStationnementTranche1Jour'), (N'Parametres_TarifStationnementTranche2Jour'),
        (N'Parametres_TarifStationnementTranche3Jour'),
        (N'Parametres_SeuilPoidsLourd20Pieds'), (N'Parametres_SeuilPoidsLourd40Pieds'),
        (N'Parametres_FranchiseStationnementImportLocalJours'), (N'Parametres_FranchiseStationnementImportTransitJours');

    DECLARE @NomColonne sysname, @Sql nvarchar(400);
    DECLARE curColonnes CURSOR LOCAL FAST_FORWARD FOR SELECT Nom FROM @ColonnesScalaires;
    OPEN curColonnes;
    FETCH NEXT FROM curColonnes INTO @NomColonne;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF COL_LENGTH(N'dbo.VersionsGrillesTarifaires', @NomColonne) IS NOT NULL
        BEGIN
            SET @Sql = N'ALTER TABLE dbo.VersionsGrillesTarifaires DROP COLUMN ' + QUOTENAME(@NomColonne) + N';';
            EXEC sp_executesql @Sql;
        END;
        FETCH NEXT FROM curColonnes INTO @NomColonne;
    END;
    CLOSE curColonnes;
    DEALLOCATE curColonnes;

    IF COL_LENGTH(N'dbo.LignesResultatsSimulation', N'PartCit') IS NOT NULL ALTER TABLE dbo.LignesResultatsSimulation DROP COLUMN PartCit;
    IF COL_LENGTH(N'dbo.LignesResultatsSimulation', N'PartOic') IS NOT NULL ALTER TABLE dbo.LignesResultatsSimulation DROP COLUMN PartOic;
    IF COL_LENGTH(N'dbo.LignesResultatsSimulation', N'PartPaa') IS NOT NULL ALTER TABLE dbo.LignesResultatsSimulation DROP COLUMN PartPaa;

    INSERT INTO #JournalReprise (Etape, Message) VALUES (N'H', N'Colonnes texte et scalaires supprimées.');

    /* ==========================================================================================
       I. INSCRIPTION DE LA MIGRATION
       La migration EF ne doit PAS être rejouée : son Up() détruirait ce que ce script vient de
       construire. On l'inscrit donc comme appliquée.
       Les tables de paramètres (B2) et leur reprise (C2) sont traitées plus haut : l'inscription
       ne laisse donc derrière elle aucune table vide que la migration aurait dû remplir.
       ========================================================================================== */

    IF @DejaAppliquee = 0
    BEGIN
        INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
        VALUES (@MigrationId, @ProduitVersion);
        INSERT INTO #JournalReprise (Etape, Message) VALUES (N'I', N'Migration inscrite dans __EFMigrationsHistory.');
    END
    ELSE
        INSERT INTO #JournalReprise (Etape, Message) VALUES (N'I', N'Migration déjà inscrite — aucune écriture.');

    /* Comptages APRÈS — aucune ligne ne doit avoir disparu. */
    INSERT INTO #JournalReprise (Etape, Message, Lignes)
    VALUES (N'I', N'LignesTarifaires après reprise',          (SELECT COUNT(*) FROM dbo.LignesTarifaires)),
           (N'I', N'LignesResultatsSimulation après reprise', (SELECT COUNT(*) FROM dbo.LignesResultatsSimulation)),
           (N'I', N'SimulationsProforma après reprise',       (SELECT COUNT(*) FROM dbo.SimulationsProforma));

    IF (SELECT COUNT(*) FROM dbo.LignesTarifaires) <> @NbLignesTarifairesAvant
        THROW 50020, N'R-15 : le nombre de lignes tarifaires a changé pendant la reprise. Annulation.', 1;

    COMMIT TRANSACTION RepriseR15;

    SELECT * FROM #JournalReprise ORDER BY Ordre;
    PRINT N'R-15 : reprise terminée et validée.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;

    PRINT N'R-15 : ÉCHEC — la base est revenue à son état antérieur.';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH;

DROP TABLE IF EXISTS #JournalReprise;
DROP TABLE IF EXISTS #MapSens;
DROP TABLE IF EXISTS #MapTaille;
DROP TABLE IF EXISTS #MapType;
