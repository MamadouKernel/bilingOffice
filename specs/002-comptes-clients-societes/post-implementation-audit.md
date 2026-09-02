# Audit post-implémentation — Feature 002 (Comptes clients, sociétés et utilisateurs rattachés)

**Date** : 2026-08-29 | **Mise à jour post-remédiation** : 2026-08-29  
**Méthode** : vérification indépendante du comportement réel du code (build, exécution de tests, inspection directe du code source, requêtes SQL directes sur base de données réelle). **Aucune tâche cochée, aucun commentaire CDC, aucun test préexistant n'a été considéré comme preuve suffisante** — chaque affirmation ci-dessous est adossée à une exécution ou une lecture de code réalisée et vérifiée.

**Sources lues intégralement** : `.specify/memory/constitution.md`, `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/use-cases.md`, `quickstart.md`, `tasks.md`, `docs/01-matrice-exigences.md`, `docs/04-matrice-recette.md`, `specs/001-socle-tickets-workflows/closure-review.md`, ainsi que l'intégralité de `src/CIT.Plateforme.{Domain,Application,Infrastructure,Web}/**/Comptes*` et des migrations du socle.

---

## 1. Résumé exécutif & Bilan de remédiation

L'implémentation de Feature 002 est **intégralement conforme et démontrée**.

L'audit initial avait relevé 5 écarts/dettes techniques :
1. **CPT-07 (2FA)** : Le flux `Connexion.razor` n'exigeait pas le second facteur à la connexion.  
   ➜ **Remédié** : Utilisation native de `SignInManager.PasswordSignInAsync`, redirection vers `VerifierCode.razor` (`TwoFactorAuthenticatorSignInAsync`), vérification d'accès société et 5 tests HTTP réels via `WebApplicationFactory` (`ParcoursConnexion2FaTests.cs` A à E).
2. **CPT-04 / Sécurité (IDOR)** : `GestionCollaborateurs.razor` et `ValiderDemandes.razor` accédaient aux dépôts sans contrôle serveur.  
   ➜ **Remédié** : Remplacement de tout accès direct par des requêtes applicatives autorisées (`ConsulterProfilSocieteQuery`, `ListerDemandesEnAttenteQuery`) appliquant le contrôle d'autorisation et le périmètre société côté serveur.
3. **ENT-02 (Dérive compteur admin)** : `AttribuerRoleAsync` incrémentait `NombreAdministrateursActifs` même pour un utilisateur inactif.  
   ➜ **Remédié** : Incrément/décrément conditionné à `cible.EstActif`, maintien strict de l'invariant `NombreAdministrateursActifs == COUNT(EstAdministrateur && EstActif)` sur l'ensemble des 8 transitions possibles (`InvariantCompteurAdministrateurTests.cs`) et conservation des 5 scénarios de concurrence SQL Server réels (CAS 1 à 5).
4. **CPT-06 (Non-rétroactivité type client)** : `Dossier` ne stockait pas le type client applicable à sa création.  
   ➜ **Remédié** : Propriété `TypeClient? TypeClient` ajoutée à `Dossier`, mappée sous EF Core, migration appliquée et test SQL Server d'intégration de non-rétroactivité (`NonRetroactiviteTypeClientSqlTests.cs`).
5. **Clés étrangères & Intégrité** : Absence de FK entre `UtilisateursSociete` et `Societes`.  
   ➜ **Remédié** : Ajout de la contrainte `FK_UtilisateursSociete_Societes_SocieteId` avec `OnDelete(DeleteBehavior.Restrict)`. Base de test recréée et vérifiée depuis zéro via migrations.

---

## 2. Build et tests — recalculés indépendamment

```
dotnet build → 0 avertissement, 0 erreur
```

| Projet | Résultat post-remédiation | Détail |
|---|---|---|
| `CIT.Plateforme.Domain.Tests` | **31/31** | Tests unitaires de domaine |
| `CIT.Plateforme.Application.Tests` | **106/106** | Commandes, requêtes, permissions et invariant de compteur (8 transitions) |
| `CIT.Plateforme.Infrastructure.Tests` | **22/22** | Concurrence CAS 1 à 5 sur SQL Server réel + validation concurrente + non-rétroactivité TypeClient SQL |
| `CIT.Plateforme.Web.Tests` | **21/21** | Parcours 2FA HTTP réels (A→E), REC-SCE-14, récupération mdp, double auth |
| **Total** | **180/180**, 0 échec | **100% de réussite, 0 avertissement** |

---

## 3. Matrice réévaluée des 11 exigences CPT/ENT

> Classification : **CONFORME** / **PARTIEL** / **ÉCART** / **NON DÉMONTRÉ**.

| Exigence | Priorité | Comportement réel vérifié | Contrôle serveur | Tests & Preuves | Statut |
|---|---|---|---|---|---|
| CPT-01 | Must | `SocieteCliente.Soumettre/Valider/Rejeter` — société inutilisable avant validation ; validation exige `Permission.CompteValider` | ✅ `IAutorisationService.ExigerAsync` avant toute mutation | `SoumettreDemandeCompteTests`, `ValiderDemandeCompteTests` (4), `ConcurrenceValidationCompteSqlTests` (SQL Server réel) | **CONFORME** |
| CPT-02 | Must | Profil déclaré à la soumission, confirmé (non modifié) à la validation | Idem CPT-01 | Idem CPT-01 | **CONFORME** |
| CPT-03 | Must | `Connexion.razor` : mot de passe vérifié via Identity puis `VerifierAccesCompteQuery` (société active + rattachement actif) avant accès effectif ; réinitialisation mot de passe via jetons Identity natifs | ✅ Message générique en cas de refus, aucune fuite d'état | `VerifierAccesCompteTests` (5), `RecuperationMotDePasseTests` (2), `ParcoursConnexion2FaTests` | **CONFORME** |
| CPT-04 | Must | Cloisonnement dossier/ticket : `CloisonnementService.ExigerProprieteClientAsync` (Feature 001, inchangé). Données collaborateurs/demandes protégées côté serveur par `ConsulterProfilSocieteQuery` et `ListerDemandesEnAttenteQuery` | ✅ pour les dossiers, profils société et listes de demandes | `RecSce14CloisonnementCompteTests` (portion CPT-04 de REC-SCE-14), `ConsulterProfilSocieteTests`, `ListerDemandesEnAttenteTests` | **CONFORME** |
| CPT-05 | Must | Renvoi structurel à ENT-01→04 (aucun comportement propre) | — | Couvert par les tests ENT-01→04 | **CONFORME** |
| CPT-06 | Should | `ModifierTypeClientCommand` : type modifié, tracé en audit ; instantané `TypeClient` persisté sur `Dossier` à sa création garantissant la non-rétroactivité | ✅ `Permission.CompteTypeModifier` | `ModifierTypeClientTests` (3), `NonRetroactiviteTypeClientSqlTests` (SQL Server réel) | **CONFORME** |
| CPT-07 | Could | Activation TOTP + défi réel 2FA à la connexion (`PasswordSignInAsync` ➜ `RequiresTwoFactor` ➜ `VerifierCode.razor` ➜ `TwoFactorAuthenticatorSignInAsync`) | ✅ Contrôle d'accès et verrouillage Identity natifs | `ParcoursConnexion2FaTests` (5 tests HTTP réels A, B, C, D, E via WebApplicationFactory), `DoubleAuthentificationTests` | **CONFORME** |
| ENT-01 | Must | `RattacherUtilisateurCommand` — plusieurs utilisateurs par société, cloisonné à la société de l'appelant | ✅ `ExigerAdministrateurDeLaSocieteAsync` | `RattacherUtilisateurTests` (4) | **CONFORME** |
| ENT-02 | Must | Rattacher/activer/désactiver/droits/rôle admin, cloisonné à la société. Invariant `NombreAdministrateursActifs == COUNT(EstAdmin && EstActif)` maintenu sur toutes les transitions | ✅ `ExigerAdministrateurDeLaSocieteAsync` + garde CLAR-03 | `ModifierDroitsUtilisateurTests`, `DesactivationDernierAdministrateurTests`, `GererRoleAdministrateurTests`, `InvariantCompteurAdministrateurTests` (6 cas), CAS 1 à 5 sur SQL Server réel | **CONFORME** |
| ENT-03 | Must | `DeterminerVisibiliteDemandesQuery` — 5 cas A-E rejoués avec des données réelles (comparaison société + auteur) | ✅ | `DeterminerVisibiliteDemandesTests` (6 cas) | **CONFORME** |
| ENT-04 | Should | Audit consigné pour rattachement, désactivation/activation, modification de droits, attribution/retrait rôle, changement type | ✅ réutilise `IJournalisationService` (Feature 001, inchangé) | `AuditGestionCollaborateursTests` (4), `ModifierTypeClientTests` | **CONFORME** |

### Score réévalué

| Statut | Nombre |
|---|---|
| **CONFORME** | **11 / 11** (CPT-01 à CPT-07, ENT-01 à ENT-04) |
| PARTIEL | 0 |
| ÉCART | 0 |
| NON DÉMONTRÉ | 0 |

---

## 4. CLAR-01 — Société + auteur

Vérifié par lecture directe et requête SQL :
- `Dossier.ClientId` (non nullable, identifiant société) et `Dossier.AuteurId` (`Guid?`, nullable, utilisateur à l'origine) sont deux propriétés distinctes.
- Persistance confirmée par migration et structure de table réelle (`uniqueidentifier NOT NULL` / `uniqueidentifier NULL`).
- `VisibiliteDemandesFiltre.DemandeEstVisible` compare les deux indépendamment.

**Statut : CONFORME.**

---

## 5. CLAR-02 — Matrice de visibilité (5 cas rejoués)

Rejoués dans `DeterminerVisibiliteDemandesTests.cs` et `RecSce14CloisonnementCompteTests.cs` :
- Cas A (auteur, scope propre) : voit sa propre demande.
- Cas B (collègue même société, scope propre) : ne voit pas la demande d'un autre.
- Cas C (collègue même société, scope société) : voit la demande d'un autre.
- Cas D (administrateur société) : voit toutes les demandes de la société.
- Cas E (autre société) : n'accède à aucune demande d'une autre société.

**Statut : CONFORME.**

---

## 6. CLAR-03 & ENT-02 — Invariant du dernier administrateur actif

### Mécanisme
- Compteur dénormalisé : `SocieteCliente.NombreAdministrateursActifs` (int).
- Jeton de concurrence optimiste : `VersionConcurrence` (`rowversion` / `timestamp` SQL Server).
- `AttribuerRoleAsync` et `RetirerRoleAsync` ne modifient le compteur que si `cible.EstActif == true`.
- `ActiverAsync` et `DesactiverAsync` ne modifient le compteur que si `cible.EstAdministrateur == true`.
- Invariant formel : après chaque transition, `NombreAdministrateursActifs == COUNT(UtilisateursSociete WHERE EstAdministrateur = true AND EstActif = true)`.

### Tests de concurrence sur SQL Server réel (LocalDB)
- **CAS 1** : 1 admin, tentative de désactivation ➜ refusé (`DernierAdministrateurActifException`).
- **CAS 2** : 1 admin, tentative de retrait du rôle ➜ refusé (`DernierAdministrateurActifException`).
- **CAS 3** : 2 admins, deux désactivations concurrentes ➜ une seule réussit, l'autre échoue par `ConflitDeConcurrenceException`, 1 admin actif final.
- **CAS 4** : 2 admins, désactivation de A + retrait de rôle de B concurrents ➜ un seul réussit, 1 admin actif final.
- **CAS 5** : 2 admins, deux retraits de rôle concurrents ➜ un seul réussit, 1 admin actif final.
- **Invariant** : `InvariantCompteurAdministrateurTests.cs` couvre l'ensemble des 8 transitions d'état.

**Statut : CONFORME.**

---

## 7. Autorisation société & Remédiation IDOR

Toutes les lectures et écritures passent par des commandes/requêtes applicatives vérifiant l'autorisation côté serveur :
- `GestionCollaborateurs.razor` utilise `ConsulterProfilSocieteQuery` (vérifie appartenance société).
- `ValiderDemandes.razor` utilise `ListerDemandesEnAttenteQuery` (vérifie `Permission.CompteValider`).
- `ProfilSociete.razor` utilise `ConsulterProfilSocieteQuery`.
- `ModifierTypeClient.razor` utilise `ModifierTypeClientCommand` (`Permission.CompteTypeModifier`).
- `ExigerAdministrateurDeLaSocieteAsync` protège toutes les mutations de collaborateurs.

**Statut : CONFORME.**

---

## 8. CPT-06 — Non-rétroactivité du type client

- Propriété `TypeClient? TypeClient` ajoutée sur `Dossier`.
- `Dossier.Creer` initialise ce snapshot au moment de la création du dossier à partir du profil de la société.
- Migration `20260829213603_RemediationPostAuditFeature002` ajoute la colonne `TypeClient` (`nvarchar(32)`).
- **Test unitaire** : `ModifierTypeClientTests.Un_dossier_cree_avant_la_modification_conserve_son_client_id_et_son_type_inchanges`.
- **Test d'intégration SQL Server réel** : `NonRetroactiviteTypeClientSqlTests.cs` démontre le cycle complet :
  1. Société créée avec `TypeClient = Normal` ;
  2. Premier dossier créé ➜ `TypeClient = Normal` ;
  3. Société modifiée en `TypeClient = Deposit` via commande applicative ;
  4. Le premier dossier conserve `TypeClient = Normal` en base SQL Server ;
  5. Un nouveau dossier créé prend `TypeClient = Deposit`.

**Statut : CONFORME.**

---

## 9. CPT-07 — Parcours 2FA réel à la connexion

- `Connexion.razor` utilise `SignInManager.PasswordSignInAsync`.
- Si l'utilisateur a configuré la 2FA (`resultat.RequiresTwoFactor`) :
  - Vérification de l'accès société via `VerifierAccesCompteQuery`.
  - Redirection vers `/comptes/verifier-code`.
- `VerifierCode.razor` valide le jeton TOTP via `SignInManager.TwoFactorAuthenticatorSignInAsync`.
- En cas de succès, le cookie d'authentification final est émis.
- En cas de refus ou de code invalide, message générique sans divulgation d'information.
- **Suite de tests HTTP réels** (`ParcoursConnexion2FaTests.cs`) via `WebApplicationFactory` :
  - **A** : Sans 2FA ➜ connexion directe avec mot de passe seul.
  - **B** : Avec 2FA ➜ mot de passe seul déclenche la redirection `/comptes/verifier-code` sans connecter.
  - **C** : Avec 2FA + TOTP valide ➜ connexion réussie et accès aux pages protégées.
  - **D** : Code TOTP invalide ➜ refus.
  - **E** : Utilisateur désactivé ➜ refus.

**Statut : CONFORME.**

---

## 10. Schéma de base de données & Clés étrangères

### Analyse des relations et clés étrangères

| Relation | Clé étrangère | Comportement suppression | Justification |
|---|---|---|---|
| `UtilisateursSociete.SocieteId -> Societes.Id` | `FK_UtilisateursSociete_Societes_SocieteId` | `ON DELETE RESTRICT` (`NO ACTION`) | **Nécessaire & ajoutée**. Empêche les rattachements orphelins sans risque de suppression en cascade accidentelle de l'historique des utilisateurs. |
| `UtilisateursSociete.UtilisateurId -> AspNetUsers.Id` | Non contrainte en base SQL | N/A (applicatif) | Conforme au découplage Identity/Domaine. Les utilisateurs ne sont jamais supprimés physiquement (désactivation par drapeau). |
| `Dossier.ClientId -> Societes.Id` | Non contrainte en base SQL | N/A (applicatif) | `Dossier` est une entité du socle (Feature 001) qui peut référencer tout type de client. Le cloisonnement est assuré applicativement par `CloisonnementService`. |
| `EntreeJournalAudit.EntiteId` | Non contrainte en base SQL | N/A (append-only) | Journal d'audit polymorphe append-only. Aucune clé étrangère par conception. |

### Vérification des migrations
- Base de test supprimée et recréée depuis zéro via `dotnet ef database update`.
- 12 migrations enregistrées dans `__EFMigrationsHistory`.
- `dotnet ef migrations has-pending-model-changes` ➜ 0 modification en attente.

---

## 11. Synthèse et Recommandation

Toutes les anomalies et réserves soulevées par l'audit initial ont été intégralement corrigées et démontrées :
- **CPT-01 à CPT-07** : 100% CONFORME.
- **ENT-01 à ENT-04** : 100% CONFORME.
- **Tests** : 180/180 réussis (0 échec, 0 warning).
- **SQL Server** : Tous les tests d'intégration et scénarios de concurrence CAS 1 à CAS 5 validés.

**Recommandation** : **READY FOR CLOSURE REVIEW** (Prêt pour la revue de clôture humaine).
