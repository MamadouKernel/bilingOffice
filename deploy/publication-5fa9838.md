# Point de publication — release `5fa9838` (AVEC migration)

> Depuis la dernière prod, ce lot ajoute : modale d'habilitations instructive (profils métier),
> cloisonnement supervision (charge par opérateur), notifications aux agents/valideurs, logo YABA,
> et **champs d'enrôlement chauffeur typés** (NOM, Prénoms, téléphone, date d'expiration du permis,
> catégorie optionnelle).
>
> ⚠️ **Contient une migration de schéma** (`TypeChampEnrolement` : colonnes `Type` + `Options` sur
> `ChampsFormulaireEnrolement`). Script idempotent fourni.

## 0. Pré-vol (poste de dev)
- [x] `main` = `5fa9838`, poussé.
- [x] Suites vertes : Domain 310 · Application 1121 · Infrastructure 210 · Web 546.
- [x] Script SQL idempotent généré : `deploy/migrations-idempotent-5fa9838.sql`.

## 1. Sauvegarde base prod (SQL 10.88.179.104, base `yaba`) — AVANT tout
```sql
BACKUP DATABASE [yaba]
  TO DISK = N'C:\Backups\yaba-avant-5fa9838.bak'
  WITH INIT, COMPRESSION, STATS = 5;
```
- [ ] Sauvegarde faite, fichier `.bak` vérifié (taille non nulle).

## 2. Appliquer la migration (idempotent — ne rejoue que le manquant)
Chaque bloc est gardé par `__EFMigrationsHistory` : sûr à relancer.
```bash
sqlcmd -S 10.88.179.104 -d yaba -b -i deploy/migrations-idempotent-5fa9838.sql
```
- [ ] Exécuté sans erreur (`-b` = arrêt au 1er échec).
- [ ] Contrôle : les colonnes existent
```sql
SELECT name FROM sys.columns
WHERE object_id = OBJECT_ID('dbo.ChampsFormulaireEnrolement') AND name IN ('Type','Options');
-- attendu : 2 lignes
```

## 3. Mettre à jour le worktree de publication (JAMAIS depuis le dossier de travail)
```bash
cd C:/Users/KERNELMK/Documents/cit/yaba-publication
git fetch origin
git checkout --detach origin/main      # -> 5fa9838
git log --oneline -1                    # vérifier 5fa9838
```
- [ ] Worktree `yaba-publication` sur `5fa9838`.
- [ ] `appsettings.json` : `"PlateformeDb"` **reste vide** (secret prod jamais committé).

## 4. Publier vers IIS (MSDeploy / WMSVC) — depuis `yaba-publication`
Cible (`IISProfile.pubxml`) : serveur `10.88.179.103`, site `yaba`, user `ADMINISTRATOR`. Mot de passe **saisi par toi** (non stocké).
```bash
cd C:/Users/KERNELMK/Documents/cit/yaba-publication
dotnet publish src/CIT.Plateforme.Web/CIT.Plateforme.Web.csproj ^
  -c Release ^
  /p:PublishProfile=IISProfile ^
  /p:Password=TON_MOT_DE_PASSE_WMSVC
```
Options du profil : `app_offline` pendant le push, sauvegarde du site, `SkipExtraFilesOnServer`.
- [ ] Publish terminé sans erreur.
- [ ] Clé de chiffrement toujours présente sur le pool `yaba` (non républiée).

> **Note ordre migration/app** : la migration n'ajoute que des colonnes (compatibles avec l'ancienne
> version). Tu peux donc migrer (étape 2) **avant** le publish sans risque de casse de l'ancienne app.
> Au premier démarrage, l'app **amorce automatiquement** les champs chauffeur (NOM, Prénoms,
> téléphone, date d'expiration, catégorie) s'ils n'existent pas.

## 5. Vérifications post-déploiement
- [ ] `GET http://10.88.179.103:8091/ready` → `Healthy` (5 contrôles).
- [ ] Onglet : **logo YABA** (Ctrl+F5 pour vider le cache du favicon).
- [ ] **Enrôlement chauffeur** : le formulaire demande NOM, Prénoms, Téléphone, Date d'expiration (sélecteur de date), Catégorie (liste, optionnelle) ; pas de CNI.
- [ ] **Agent Gate** : à une nouvelle soumission, notification in-app « Nouvelle demande dans votre file ».
- [ ] **Supervision** : « Charge par opérateur » ne montre que les opérateurs du périmètre.
- [ ] Admin : modale d'habilitations avec profils métier.

## 6. Rollback
- Application : republier la version précédente (`git checkout --detach <commit_precedent>` puis publish).
- Base : les colonnes ajoutées sont **nullable / à défaut** et ignorées par l'ancienne version → pas de rollback SQL nécessaire ; restauration complète via `C:\Backups\yaba-avant-5fa9838.bak` seulement en cas d'incident indépendant.

---
### Résumé impact base
| Objet | Changement | Rétro-compatible |
|------|------------|------------------|
| `ChampsFormulaireEnrolement.Type` | nouvelle colonne `nvarchar(16)` (défaut « Texte ») | ✅ |
| `ChampsFormulaireEnrolement.Options` | nouvelle colonne `nvarchar(512)` nullable | ✅ |
| Champs chauffeur | **amorcés par l'app** au démarrage (pas par SQL) | ✅ idempotent |
