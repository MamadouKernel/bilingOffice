/* =====================================================================================
   SEC-09 — Remplacer le compte « sa » par un compte applicatif à privilèges minimaux.
   À exécuter UNE FOIS dans SSMS, connecté en administrateur, sur le serveur de la base « yaba ».

   1. Remplacer <<MOT_DE_PASSE_FORT>> par un mot de passe généré (20+ caractères), conservé
      dans le coffre de la DSI — jamais dans un fichier du dépôt.
   2. Exécuter le script.
   3. Mettre à jour la chaîne de connexion sur le serveur IIS (variable d'environnement
      ConnectionStrings__PlateformeDb ou appsettings.Production.json non versionné) :
        Server=10.88.179.104;Database=yaba;User Id=yaba_app;Password=<<MOT_DE_PASSE_FORT>>;TrustServerCertificate=True;Encrypt=True
   4. Redémarrer le pool IIS, vérifier /ready, puis changer le mot de passe de « sa »
      (il a circulé en clair sur un poste de développement).

   Droits : lecture/écriture des données + DDL (les migrations EF au déploiement créent et
   modifient des tables). Aucun droit serveur (pas sysadmin, pas d'accès aux autres bases).
   ===================================================================================== */

USE [master];
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'yaba_app')
    CREATE LOGIN [yaba_app] WITH PASSWORD = N'<<MOT_DE_PASSE_FORT>>',
        CHECK_POLICY = ON, CHECK_EXPIRATION = OFF, DEFAULT_DATABASE = [yaba];
GO

USE [yaba];
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'yaba_app')
    CREATE USER [yaba_app] FOR LOGIN [yaba_app];
GO

ALTER ROLE [db_datareader] ADD MEMBER [yaba_app];
ALTER ROLE [db_datawriter] ADD MEMBER [yaba_app];
ALTER ROLE [db_ddladmin]   ADD MEMBER [yaba_app];
GO

/* Contrôle : doit lister exactement les trois rôles ci-dessus. */
SELECT r.name AS Role
FROM sys.database_role_members m
JOIN sys.database_principals r ON r.principal_id = m.role_principal_id
JOIN sys.database_principals u ON u.principal_id = m.member_principal_id
WHERE u.name = N'yaba_app';
