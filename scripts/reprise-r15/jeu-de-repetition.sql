/* =============================================================================================
   R-15 — JEU DE DONNÉES DE RÉPÉTITION
   À exécuter sur une base montée au schéma d'AVANT la migration 3FN
   (20260904053850_AjoutPiecesExigeesEtape), pour éprouver `reprise-r15.sql`.

   Le jeu couvre les quatre sens, les deux tailles, les deux types et plusieurs catégories, afin
   qu'aucune branche de correspondance ne reste non exercée.
   ============================================================================================= */

SET NOCOUNT ON;

DECLARE @Grille uniqueidentifier = '11111111-1111-1111-1111-111111111111';
DECLARE @Sim    uniqueidentifier = '22222222-2222-2222-2222-222222222222';
DECLARE @Auteur uniqueidentifier = '33333333-3333-3333-3333-333333333333';

DELETE FROM dbo.LignesResultatsSimulation WHERE SimulationProformaId = @Sim;
DELETE FROM dbo.SimulationsProforma       WHERE Id = @Sim;
DELETE FROM dbo.LignesTarifaires          WHERE VersionGrilleTarifaireId = @Grille;
DELETE FROM dbo.VersionsGrillesTarifaires WHERE Id = @Grille;

INSERT INTO dbo.VersionsGrillesTarifaires
    (Id, NumeroVersion, DateEffetUtc, DateCreationUtc, AuteurId, EstActive,
     Parametres_TauxMajorationLourd, Parametres_TauxMajorationDangereux, Parametres_TauxMajorationOog,
     Parametres_SeuilPoidsLourd20Pieds, Parametres_SeuilPoidsLourd40Pieds,
     Parametres_FranchiseBranchementJours,
     Parametres_FranchiseStationnementImportLocalJours, Parametres_FranchiseStationnementImportTransitJours,
     Parametres_TarifStationnementTranche1Jour, Parametres_TarifStationnementTranche2Jour,
     Parametres_TarifStationnementTranche3Jour,
     Parametres_QuotePartCit, Parametres_QuotePartOic, Parametres_QuotePartPaa)
VALUES
    (@Grille, N'v1.0', '2026-01-01T00:00:00', '2026-01-01T00:00:00', @Auteur, 1,
     0.3000, 0.5000, 0.5000,
     15.00, 26.00,
     2,
     11, 21,
     850.00, 1250.00, 1850.00,
     0.3000, 0.2000, 0.5000);

/* Lignes tarifaires : les quatre sens, les deux tailles, plusieurs catégories.
   Sens   : 1=ImportLocal 2=ImportTransit 3=ExportLocal 4=ExportTransit
   Taille : 20 / 40 */
INSERT INTO dbo.LignesTarifaires (Id, VersionGrilleTarifaireId, Sens, Taille, CategorieMarchandise, Acconage, Relevage, TarifJourBranchement)
VALUES
    (NEWID(), @Grille, 1, 20, N'C1', 66800.00, 25000.00, 35000.00),
    (NEWID(), @Grille, 1, 40, N'C2', 90000.00, 30000.00, 35000.00),
    (NEWID(), @Grille, 2, 20, N'C4', 70000.00, 26000.00, 35000.00),
    (NEWID(), @Grille, 2, 40, N'C5', 95000.00, 31000.00, 35000.00),
    (NEWID(), @Grille, 3, 20, N'C1', 60000.00, 24000.00, 35000.00),
    (NEWID(), @Grille, 4, 40, N'C3', 85000.00, 29000.00, 35000.00);

INSERT INTO dbo.SimulationsProforma
    (Id, Reference, SocieteId, AuteurId, DateCreationUtc, VersionGrilleTarifaireId, VersionGrilleLibelle,
     Sens, DateArrivee, DateEnlevementPrevisionnel, DureeSejourJours,
     NomNavire, NumeroVoyage, NumeroBlOuBooking, MontantTotalXof)
VALUES
    (@Sim, N'SIM-REPET-0001', NEWID(), @Auteur, '2026-02-01T00:00:00', @Grille, N'v1.0',
     1, '2026-02-01', '2026-02-05', 5,
     N'NAVIRE-TEST', N'V001', N'BL-REPET', 91800.00);

/* Lignes de résultat : les deux tailles et les deux types.
   Type : 1=Dry 2=Reefer */
INSERT INTO dbo.LignesResultatsSimulation
    (Id, SimulationProformaId, NumeroConteneur, Taille, Type, PoidsTonnes, EstDangereux, EstHorsGabarit,
     CategorieMarchandise, MontantAcconage, MontantRelevage, MontantBranchement,
     MontantMajorationLourd, MontantMajorationDangereux, MontantMajorationOog,
     MontantStationnement, PartCit, PartOic, PartPaa, TotalLigneXof)
VALUES
    (NEWID(), @Sim, N'CONT-001', 20, 1, 10.00, 0, 0, N'C1', 66800.00, 25000.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 91800.00),
    (NEWID(), @Sim, N'CONT-002', 40, 2, 30.00, 0, 0, N'C1', 90000.00, 30000.00, 35000.00, 27000.00, 0.00, 0.00, 2550.00, 765.00, 510.00, 1275.00, 184550.00);

SELECT
    (SELECT COUNT(*) FROM dbo.LignesTarifaires          WHERE VersionGrilleTarifaireId = @Grille) AS LignesTarifaires,
    (SELECT COUNT(*) FROM dbo.SimulationsProforma       WHERE Id = @Sim)                          AS Simulations,
    (SELECT COUNT(*) FROM dbo.LignesResultatsSimulation WHERE SimulationProformaId = @Sim)        AS LignesResultat;
