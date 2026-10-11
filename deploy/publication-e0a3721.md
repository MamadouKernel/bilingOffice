# Point de publication — release `e0a3721`

> Branche `feat/roles-non-cit` : rôles non-CIT, 4-yeux enrôlement, relève de poste, jours fériés SLA,
> emprunt de compte audité, manuel utilisateur complété, correctif lien « code de secours » 2FA.
> `main` doit être fast-forwardé sur `e0a3721` (main n'a aucun commit de retard sur la branche).
> Prod avant livraison = `5cd7650` (06/10/2026, d'après les notes de déploiement — à confirmer au §2).
> 62 commits depuis `5cd7650`.

## 0. Pré-vol (sur le poste de dev)
- [x] Suites vertes à `e0a3721` : Domain 312 · Application 1147 · Infrastructure 210.
- [x] Suite Web : 549 vertes (mesurée à `4ef8f20` ; les commits suivants ne touchent que le manuel,
      un test d'architecture et un inventaire de test, repassés : 49 + 18 verts).
- [x] Compilation du projet Web : 0 erreur.
- [x] Script SQL idempotent généré : `deploy/migrations-idempotent-e0a3721.sql` (69 migrations,
      dont **5 nouvelles** vs la prod — voir le résumé en bas). Le script n'est PAS versionné
      (`.gitignore` : `deploy/*.sql`) : il reste sur le poste où il a été généré.
- [ ] `main` fast-forwardé sur `e0a3721` et poussé (`git merge --ff-only feat/roles-non-cit`).

## 1. Décisions à prendre AVANT de publier
- [ ] **Emprunt de compte (« Se connecter en tant que »).** Livré dans cette release. Tout compte
      du profil **AdminFonctionnel** détient `administration.comptes_impersonation` et peut agir
      comme n'importe quel compte ordinaire (client, transporteur, agent métier), avec sa vue et
      ses droits. Garde-fous en place : confirmation du mot de passe de l'administrateur,
      refus de l'auto-emprunt et de l'emprunt imbriqué, refus d'emprunter un autre détenteur du
      droit, audit des deux identités, expiration du cookie. **Décider** si cette capacité doit
      exister en production ; sinon, retirer `ComptesImpersonation` du profil AdminFonctionnel
      avant publication (le code ne la désactive pas par configuration).
- [ ] **2FA obligatoire** : aucun contournement n'est présent dans le code ni dans
      `appsettings*.json`. Ne pas définir `Securite__DoubleAuthentificationObligatoire=false`
      sur le pool (cette clé est réservée aux tests automatisés).

## 2. Sauvegarde base prod (SQL 10.88.179.104, base `yaba`) — AVANT tout
```sql
BACKUP DATABASE [yaba]
  TO DISK = N'C:\Backups\yaba-avant-e0a3721.bak'
  WITH INIT, COMPRESSION, STATS = 5;
```
- [ ] Sauvegarde effectuée et fichier `.bak` vérifié (taille non nulle).
- [ ] Dernière migration appliquée aujourd'hui en prod (relever la valeur) :
```sql
SELECT TOP 1 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC;
-- attendu : 20261005105130_ProformaEtDateEnlevement
--       ou  20261006151551_ReglementDeclaratif (si 6f106dd a déjà été livrée)
```

## 3. Appliquer la migration base (idempotent — ne rejoue que le manquant)
La prod a `MigrationAutomatiqueAuDemarrage: true` : on applique quand même le script avant,
pour maîtriser l'ordre et voir les erreurs.
```bash
sqlcmd -S 10.88.179.104 -d yaba -b -i deploy/migrations-idempotent-e0a3721.sql
```
- [ ] Exécuté sans erreur (`-b` arrête au premier échec).
- [ ] Contrôles :
```sql
SELECT TOP 1 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC;
-- attendu : 20261010122027_AjoutJoursFeriesSla
SELECT name FROM sys.tables WHERE name = 'JoursFeries';                       -- 1 ligne
SELECT name, is_nullable FROM sys.columns
 WHERE object_id = OBJECT_ID('dbo.Tickets')
   AND name IN ('ReglementDeclareUtc','EnAttenteValidationInterneParId');     -- 2 lignes, is_nullable = 1
SELECT name FROM sys.columns
 WHERE object_id = OBJECT_ID('dbo.UtilisateursSociete') AND name = 'DroitValidation';  -- 1 ligne
SELECT name FROM sys.columns
 WHERE object_id = OBJECT_ID('dbo.ChampsFormulaireEnrolement') AND name IN ('Type','Options');  -- 2 lignes
```

## 4. Mettre à jour le worktree de publication (JAMAIS depuis le dossier de travail)
```bash
cd C:/Users/KERNELMK/Documents/cit/yaba-publication
git fetch origin
git checkout --detach origin/main      # -> e0a3721
git log --oneline -1                    # vérifier e0a3721
git status --short                      # doit être vide
```
- [ ] Worktree `yaba-publication` sur `e0a3721`.
- [ ] `src/CIT.Plateforme.Web/appsettings.json` : `"PlateformeDb"` **reste vide** (le secret prod
      n'est jamais committé).

## 5. Publier vers IIS (MSDeploy / WMSVC) — depuis `yaba-publication`
Cible (profil `IISProfile.pubxml`) : serveur `10.88.179.103`, site `yaba`, user `ADMINISTRATOR`.
Le mot de passe WMSVC est **saisi par toi** (`_SavePWD=false`).
```bash
cd C:/Users/KERNELMK/Documents/cit/yaba-publication
dotnet publish src/CIT.Plateforme.Web/CIT.Plateforme.Web.csproj ^
  -c Release ^
  /p:PublishProfile=IISProfile ^
  /p:Password=TON_MOT_DE_PASSE_WMSVC
```
Options du profil déjà posées : `EnableMsDeployAppOffline`, `EnableMSDeployBackup`,
`SkipExtraFilesOnServer` (le `appsettings.Production.json` du serveur est conservé).
- [ ] Publish terminé sans erreur.
- [ ] `Stockage__CleChiffrement` toujours présente **sur le pool `yaba`** (variable d'environnement,
      non republiée). La perdre rend les pièces chiffrées illisibles.

## 6. Vérifications post-déploiement
- [ ] Santé : `GET http://10.88.179.103:8091/ready` → `Healthy` (5 contrôles : database, storage,
      antivirus, chiffrement-documents, maintenance).
- [ ] Connexion avec 2FA (code de l'application) puis **lien « Utiliser un code de secours »**
      sur l'écran du code : la page s'ouvre (correctif : `?secours=true`, ancien lien = erreur 500).
- [ ] Ouverture d'un ticket Billing Import : la fiche s'affiche ; bandeau « Que faire ensuite ? ».
- [ ] Menu **Supervision** : « Performance agents » et « Jours fériés SLA » présents.
- [ ] Écran **Jours fériés** (administrateur fonctionnel) : liste vide au départ — aucun jour férié
      n'est exclu du calcul SLA tant que rien n'est saisi (comportement inchangé à l'identique).
- [ ] Ticket pris en charge : bouton **« Rendre à la file »** présent pour un agent.
- [ ] Manuel d'utilisation (`/aide/manuel`) : sections Client Import (proforma), Facturation,
      Pilote Manifeste, Validation de paiement, Comptabilité Client, Contrôle Documentaire.
- [ ] **Enrôlement société 4-yeux** : si une société a un validateur tiers, ses soumissions d'enrôlement
      sont parquées jusqu'à approbation (nouveau comportement) — prévenir les équipes Agent Gate.
- [ ] Paramètre **« Facturation.ModeConfirmationPaiement »** : valeur actuelle conservée
      (0 = preuve + caisse ; 1 = règlement déclaratif).
- [ ] Journal d'audit : consulter les connexions et, le cas échéant, les emprunts de compte.

## 7. Rollback (si besoin)
- Application : republier `5cd7650` depuis `yaba-publication` (même procédure §4-§5 avec
  `git checkout --detach 5cd7650`).
- Base : les 5 migrations n'ajoutent que des objets neufs (1 table, 1 index, 5 colonnes nullables
  ou à valeur par défaut) — attendu **non bloquant** pour l'ancienne version, qui les ignore.
  À confirmer en préproduction si possible. Restauration complète possible via
  `C:\Backups\yaba-avant-e0a3721.bak`, seulement pour un problème indépendant : toute donnée
  saisie après la publication serait perdue.

---
### Résumé impact base (5 migrations nouvelles vs `5cd7650`)
| Migration | Objet | Changement | Rétro-compatible |
|---|---|---|---|
| `20261006151551_ReglementDeclaratif` | `Tickets.ReglementDeclareUtc` | colonne `datetime2 NULL` | ✅ |
| `20261007123836_TypeChampEnrolement` | `ChampsFormulaireEnrolement.Type`, `.Options` | `nvarchar(16) NOT NULL DEFAULT 'Texte'` ; `nvarchar(512) NULL` | ✅ (défaut) |
| `20261008100448_AjoutDroitValidationCollaborateur` | `UtilisateursSociete.DroitValidation` | `bit NOT NULL DEFAULT 0` | ✅ (défaut) |
| `20261008141558_AjoutValidationInterneEnrolement` | `Tickets.EnAttenteValidationInterneParId` | `uniqueidentifier NULL` | ✅ |
| `20261010122027_AjoutJoursFeriesSla` | table `JoursFeries` + index `IX_JoursFeries_EstActif` | nouvelle table | ✅ (ignorée) |

Aucune suppression de colonne ni de donnée dans ces 5 migrations (les `DROP COLUMN` visibles dans le
script appartiennent à d'anciennes migrations déjà appliquées, gardées par `__EFMigrationsHistory`).
Aucune donnée migrée.
