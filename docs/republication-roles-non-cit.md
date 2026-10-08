# Republication — rôles non-CIT + validation interne 4-yeux

Branche : `feat/roles-non-cit` → `main`. Incréments 1+2+3.

## Contenu livré
- **Inc. 1** — rôle Validateur non-CIT (`DroitValidation` sur le rattachement société, attribution dans Collaborateurs).
- **Inc. 2** — workflow 4-yeux : soumissions parquées si la société a un validateur tiers, écran validateur approuver/rejeter, exclusion de l'Agent Gate tant que non validé.
- **Inc. 3** — notifications intra-société : alerte des validateurs à la soumission ; information du préparateur à l'approbation / au rejet.

## Deux migrations EF (toutes deux additives, rétro-compatibles)
1. `AjoutDroitValidationCollaborateur` → `UtilisateursSociete.DroitValidation bit NOT NULL DEFAULT 0`
2. `AjoutValidationInterneEnrolement` → `Tickets.EnAttenteValidationInterneParId uniqueidentifier NULL`

Aucune colonne supprimée, aucun backfill requis. Les sociétés sans validateur conservent le comportement direct.

## Procédure (ordre strict)
1. **Merge** `feat/roles-non-cit` → `main`, push. CI verte.
2. **Backup base `yaba`** (SQL 10.88.179.104) AVANT migration — sauvegarde complète.
3. **Publier depuis le worktree** `C:\Users\KERNELMK\Documents\cit\yaba-publication` (jamais le dossier de travail) :
   - `git -C ..\yaba-publication fetch && git -C ..\yaba-publication reset --hard origin/main`
   - publier profil IISProfile (MSDeploy). **Le mot de passe ADMINISTRATOR est saisi par l'utilisateur.**
4. **Migrations** appliquées au démarrage (ou `dotnet ef database update`). Vérifier les 2 colonnes présentes.
5. **Provisionnement notifications** : `ProvisionnementNotifications` est idempotent (par `TypeEvenement`) → les 3 règles `EnrolementAValiderInterne` / `...ValideInterne` / `...RejeteInterne` + `TicketReclamationCloture` + `TicketArriveeEnFile` (email obligatoire) sont semées/mises à jour au boot. Si messages déjà en base et à rafraîchir : réexécuter le script SQL de refresh déjà fourni.
6. **Smoke** : `GET http://10.88.179.103:8091/ready` → 5 health checks Healthy.

## Vérifs fonctionnelles post-déploiement
- [ ] Attribuer « Validateur » à un collaborateur (Collaborateurs) → badge visible.
- [ ] Soumission flotte par un soumissionnaire d'une société AVEC validateur tiers → ticket parqué, absent de l'Agent Gate, validateur reçoit l'alerte (in-app + mail).
- [ ] Validateur approuve → tickets visibles Agent Gate + mail aux agents ; préparateur notifié.
- [ ] Validateur rejette avec motif → tickets annulés, préparateur notifié.
- [ ] SoD : un validateur ne peut pas valider sa propre soumission (refus).
- [ ] Société SANS validateur tiers → soumission directe inchangée.

## Rollback
Les colonnes étant additives et nullables/`DEFAULT 0`, le binaire précédent reste compatible. Rollback = republier le commit précédent ; les colonnes peuvent rester (inertes).
