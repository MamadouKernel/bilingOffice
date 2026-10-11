# Point de publication — release `76b2a30` (zone de dépôt « panier »)

> Branche `feat/roles-non-cit`. Delta vs `e0a3721` : **interface uniquement** — composant `ZoneDepot`
> (panier de basket) pour les pièces jointes, 6 écrans, FR+EN. **Aucune migration, aucun changement
> de schéma, aucune clé de configuration.**

## 1. Base de données (SQL 10.88.179.104, base `yaba`)
Le script idempotent généré à `76b2a30` est **octet pour octet identique** à celui de `e0a3721`
(187 245 octets, 69 migrations ; dernière = `20261010122027_AjoutJoursFeriesSla`).
Le fichier à passer est donc `deploy/migrations-idempotent-76b2a30.sql` (ou `-e0a3721.sql`, même contenu ;
non versionné : `.gitignore` `deploy/*.sql`).

- [ ] Si la prod est **déjà** à `e0a3721` : **aucune action SQL** (contrôle ci-dessous).
- [ ] Sinon : sauvegarde puis script (voir `publication-e0a3721.md` §2–§3).
```sql
SELECT TOP 1 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC;
-- attendu : 20261010122027_AjoutJoursFeriesSla
```

## 2. Publication IIS (pool et application `yaba`)
- [ ] `main` contient `76b2a30` (merge/ff depuis `feat/roles-non-cit`, poussé).
- [ ] Publier **depuis le worktree propre** `C:\Users\KERNELMK\Documents\cit\yaba-publication`
      (`git fetch && git checkout --detach origin/main`), profil VS `IISProfile`
      (SkipExtraFilesOnServer = `appsettings.Production.json` du serveur conservé). Jamais depuis le dossier de travail.
- [ ] Les nouveaux fichiers statiques partent avec la publication : `wwwroot/css/zone-depot.css`,
      `wwwroot/js/zone-depot.js` (références versionnées via `@Assets` / `js/…`).

## 3. Vérifications après publication
- [ ] `/ready` : 5 lignes Healthy.
- [ ] Rechargement forcé (Ctrl+F5) puis ouvrir une nouvelle demande d'avoir/exonération : le panier
      apparaît ; choisir un fichier → animation de tir ; le fichier est listé (badge, nom, taille).
- [ ] Console navigateur sans erreur CSP ; réseau : `zone-depot.*.css` et `zone-depot.js` en 200.
- [ ] Mobile : libellé « Touchez pour ajouter des fichiers ».

## 4. Retour arrière
Republier l'artefact précédent (`e0a3721`). Aucun retour SQL nécessaire (pas de migration).
