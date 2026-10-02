/* =====================================================================================
   NFR-06 — Sauvegarde complète de « yaba » + TEST DE RESTAURATION réel.
   À planifier (SQL Server Agent, quotidien) ; la partie test peut être jouée chaque semaine.
   Adapter @Dossier : un partage distinct du serveur de base (sinon une panne disque emporte
   la base ET sa sauvegarde). Les documents déposés (dossier de stockage IIS) se sauvegardent
   à part, avec la sauvegarde fichiers de la DSI.
   ===================================================================================== */

DECLARE @Dossier  NVARCHAR(400) = N'D:\Sauvegardes\yaba\';
DECLARE @Fichier  NVARCHAR(500) = @Dossier + N'yaba_' + FORMAT(SYSUTCDATETIME(), 'yyyyMMdd_HHmmss') + N'.bak';

/* 1. Sauvegarde complète, compressée, avec somme de contrôle. */
BACKUP DATABASE [yaba] TO DISK = @Fichier WITH COMPRESSION, CHECKSUM, INIT, STATS = 10;

/* 2. Le fichier est-il lisible et intègre ? */
RESTORE VERIFYONLY FROM DISK = @Fichier WITH CHECKSUM;

/* 3. Restauration RÉELLE dans une base jetable (ne touche jamais « yaba »). */
DECLARE @Data NVARCHAR(400) = CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS NVARCHAR(400));
DECLARE @Log  NVARCHAR(400) = CAST(SERVERPROPERTY('InstanceDefaultLogPath')  AS NVARCHAR(400));
DECLARE @NomData SYSNAME = (SELECT name FROM yaba.sys.database_files WHERE type = 0);
DECLARE @NomLog  SYSNAME = (SELECT name FROM yaba.sys.database_files WHERE type = 1);

IF DB_ID(N'yaba_test_restauration') IS NOT NULL
    DROP DATABASE [yaba_test_restauration];

RESTORE DATABASE [yaba_test_restauration] FROM DISK = @Fichier
WITH MOVE @NomData TO @Data + N'yaba_test_restauration.mdf',
     MOVE @NomLog  TO @Log  + N'yaba_test_restauration.ldf',
     CHECKSUM, STATS = 10;

/* 4. Preuve : les volumes restaurés égalent ceux de la production au moment du test. */
SELECT 'Tickets' AS TableControlee,
       (SELECT COUNT_BIG(*) FROM yaba.dbo.Tickets) AS Production,
       (SELECT COUNT_BIG(*) FROM yaba_test_restauration.dbo.Tickets) AS Restauree
UNION ALL
SELECT 'AspNetUsers',
       (SELECT COUNT_BIG(*) FROM yaba.dbo.AspNetUsers),
       (SELECT COUNT_BIG(*) FROM yaba_test_restauration.dbo.AspNetUsers)
UNION ALL
SELECT '__EFMigrationsHistory',
       (SELECT COUNT_BIG(*) FROM yaba.dbo.__EFMigrationsHistory),
       (SELECT COUNT_BIG(*) FROM yaba_test_restauration.dbo.__EFMigrationsHistory);

/* 5. Nettoyage. Conserver la sortie de ce script (date + comptages) comme preuve NFR-06. */
DROP DATABASE [yaba_test_restauration];
