# Point de publication — release `6f106dd`

> Écarts 06/10 : #1 document commun multi-BL, #2 confirmation paiement paramétrable, #3 (déjà couvert).
> `main` = `6f106dd`. Prod avant livraison = `5cd7650`.

## 0. Pré-vol (sur le poste de dev, branche dev)
- [x] `main` fast-forwardé sur `6f106dd` et poussé.
- [x] Suites vertes : Domain 306 · Application 1109 · Infrastructure 210 · Web 546.
- [x] Script SQL idempotent généré : `deploy/migrations-idempotent-6f106dd.sql`
      (seule nouveauté vs prod : colonne nullable `Tickets.ReglementDeclareUtc`).

## 1. Sauvegarde base prod (SQL 10.88.179.104, base `yaba`) — AVANT tout
Depuis SSMS ou sqlcmd sur le serveur SQL :
```sql
BACKUP DATABASE [yaba]
  TO DISK = N'C:\Backups\yaba-avant-6f106dd.bak'
  WITH INIT, COMPRESSION, STATS = 5;
```
- [ ] Sauvegarde effectuée et fichier `.bak` vérifié (taille non nulle).

## 2. Appliquer la migration base (idempotent — ne rejoue que le manquant)
Le script est sûr à relancer : chaque bloc est gardé par `__EFMigrationsHistory`.
```bash
sqlcmd -S 10.88.179.104 -d yaba -b -i deploy/migrations-idempotent-6f106dd.sql
```
- [ ] Exécuté sans erreur (`-b` arrête au premier échec).
- [ ] Contrôle : la colonne existe
```sql
SELECT name, is_nullable FROM sys.columns
WHERE object_id = OBJECT_ID('dbo.Tickets') AND name = 'ReglementDeclareUtc';
-- attendu : 1 ligne, is_nullable = 1
```

## 3. Mettre à jour le worktree de publication (JAMAIS depuis le dossier de travail)
```bash
cd C:/Users/KERNELMK/Documents/cit/yaba-publication
git fetch origin
git checkout --detach origin/main      # -> 6f106dd
git log --oneline -1                    # vérifier 6f106dd
```
- [ ] Worktree `yaba-publication` sur `6f106dd`.
- [ ] `src/CIT.Plateforme.Web/appsettings.json` : `"PlateformeDb"` **reste vide** (le secret prod n'est jamais committé ; il est fourni au moment du publish / via la config serveur).

## 4. Publier vers IIS (MSDeploy / WMSVC) — depuis `yaba-publication`
Cible (profil `IISProfile.pubxml`) : serveur `10.88.179.103`, site `yaba`, user `ADMINISTRATOR`.
Le mot de passe WMSVC est **saisi par toi** (il n'est pas stocké : `_SavePWD=false`).
```bash
cd C:/Users/KERNELMK/Documents/cit/yaba-publication
dotnet publish src/CIT.Plateforme.Web/CIT.Plateforme.Web.csproj ^
  -c Release ^
  /p:PublishProfile=IISProfile ^
  /p:Password=TON_MOT_DE_PASSE_WMSVC
```
Options du profil déjà posées : `EnableMsDeployAppOffline` (app_offline pendant le push),
`EnableMSDeployBackup` (sauvegarde du site), `SkipExtraFilesOnServer`.
- [ ] Publish terminé sans erreur.
- [ ] La clé de chiffrement des documents est bien présente **sur le pool `yaba`** (variable d'environnement du pool, non republiée — vérifier qu'elle n'a pas été écrasée).

## 5. Vérifications post-déploiement
- [ ] Santé : `GET http://10.88.179.103:8091/ready` → `Healthy` (5 contrôles : database, storage, antivirus, chiffrement-documents, maintenance).
- [ ] Connexion + ouverture d'un ticket Billing Import (fiche s'affiche).
- [ ] Paramètres plateforme : le paramètre **« Facturation.ModeConfirmationPaiement »** est visible, valeur par défaut **0** (preuve + caisse). Le comportement de paiement actuel est donc **inchangé** tant que tu ne passes pas à 1.
- [ ] (si tu veux activer le mode déclaratif) le passer à **1**, puis vérifier côté client le bouton « Règlement effectué » et côté facturier « Confirmer le règlement ».

## 6. Rollback (si besoin)
- Application : republier `5cd7650` depuis `yaba-publication` (même procédure §3-§4 avec `git checkout --detach 5cd7650`).
- Base : la migration n'ajoute qu'une colonne nullable — **non bloquante** pour l'ancienne version (elle l'ignore). Restauration complète possible via `C:\Backups\yaba-avant-6f106dd.bak` seulement en cas de problème indépendant.

---
### Résumé impact base
| Objet | Changement | Rétro-compatible |
|------|------------|------------------|
| `Tickets.ReglementDeclareUtc` | nouvelle colonne `datetime2 NULL` | ✅ (ancienne version l'ignore) |

Aucune autre modification de schéma. Aucune donnée migrée.
