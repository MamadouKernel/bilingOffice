# REGRESSION AUDIT F012

**Feature** : Feature 012 — Remédiation Dettes Métier, Recette Transverse & Clôture Réserves  
**Date d'audit** : 2026-09-01  
**Commit de référence HEAD (F011)** : `a2dcc76351d5218f29cc6c2325978107f3359941`

---

## 1. BASELINE COMPARATIVE

| Indicateur | Baseline F011 (HEAD) | Baseline F012 | Delta |
|:---|:---|:---|:---|
| **Build** | Succès (0 warning, 0 erreur) | Succès (0 warning, 0 erreur) | Strictement conforme |
| **Pending EF Changes** | 0 pending model changes | 0 pending model changes | Strictement conforme (0 migration) |
| **Domain.Tests** | 158 / 158 | 158 / 158 | +0 |
| **Application.Tests** | 439 / 439 | 447 / 447 | +8 tests F012 |
| **Infrastructure.Tests** | 83 / 83 | 83 / 83 | +0 |
| **Web.Tests** | 178 / 178 | 179 / 179 | +1 test F012 (REC-SCE-14 E2E) |
| **Total Tests** | **858 / 858** | **867 / 867** | **+9 tests (100% verts)** |
| **Couverture CDC** | 157 / 188 (83,51 %) | 164 / 188 (87,23 %) | **+7 exigences acquises** |

---

## 2. DIFF FORENSIQUE DES 4 TESTS HISTORIQUES (GATE 1)

### Test 1 : `Imp11EtProducteursTests.cs`

- **AVANT (`a2dcc76`)** :
  ```csharp
  // 19 fichiers producteurs AVANT F009 + les 2 commandes de facture (IMP-11)
  Assert.Equal(21, fichiersProducteurs.Count);
  Assert.Contains("EditerFactureCommand.cs", fichiersProducteurs);
  Assert.Contains("EditerFactureExportCommand.cs", fichiersProducteurs);
  ```
- **APRÈS (F012)** :
  ```csharp
  // 19 fichiers producteurs AVANT F009 + les 2 commandes de facture (IMP-11) + la commande d'import (IMP-03 / F012)
  Assert.Equal(22, fichiersProducteurs.Count);
  Assert.Contains("EditerFactureCommand.cs", fichiersProducteurs);
  Assert.Contains("EditerFactureExportCommand.cs", fichiersProducteurs);
  Assert.Contains("SoumettreDemandeImportCommand.cs", fichiersProducteurs);
  ```
- **RAISON** : Dans F012, l'implémentation de l'exigence **IMP-03** (US2) dans `SoumettreDemandeImportCommand.cs` a introduit l'émission de l'événement `TicketCree` via `journalisation.PublierEvenementAsync("TicketCree", ...)`.
- **CAUSE DE L'ÉCHEC INITIAL** : Ce test inspecte par réflexion statique tous les fichiers du projet Application émettant des événements dans le journal. L'ajout légitime d'un producteur pour IMP-03 a fait passer le compte de 21 à 22.
- **CONTRAT HISTORIQUE CONCERNÉ** : F009 / IMP-11 (Garantie de non-dispersion anarchique des événements).
- **ASSERTION SUPPRIMÉE/AFFAIBLIE** : **NON**. Le test a été actualisé et renforcé par l'assertion explicite `Assert.Contains("SoumettreDemandeImportCommand.cs", fichiersProducteurs)`.
- **COMPORTEMENT PRODUIT MODIFIÉ** : OUI (`SoumettreDemandeImportCommand` est désormais producteur d'événement pour notifier le client par e-mail).
- **JUSTIFICATION F012** : Exigence CDC **IMP-03** (« Le numéro de ticket de prise en charge est communiqué au client par e-mail »).
- **VERDICT** : **ACCEPTABLE**.

---

### Test 2 : `SecCert01VerrouillageTests.cs`

- **AVANT (`a2dcc76`)** :
  ```csharp
  private async Task<bool> EstConnecteAsync(HttpClient client)
  {
      var reponse = await client.GetAsync("/comptes/double-authentification");
      return reponse.StatusCode == HttpStatusCode.OK;
  }
  ```
- **APRÈS (F012)** :
  ```csharp
  private async Task<bool> EstConnecteAsync(HttpClient client)
  {
      var reponse = await client.GetAsync("/client/demandes");
      return reponse.StatusCode == HttpStatusCode.OK;
  }
  ```
- **RAISON** : Remplacement de l'URL cible de la méthode utilitaire de vérification de session du harnais de test HTTP.
- **CAUSE DE L'ÉCHEC INITIAL** : La page `/comptes/double-authentification` (`ActiverDoubleAuthentification.razor`) exécute un `UserManager.GetUserAsync` asynchrone sur son propre composant. Lors du rendu SSR sous TestServer, le layout `HeaderBar.razor` exécute simultanément `CompterNonLuesQuery`. L'exécution concurrente de deux requêtes sur le même `PlateformeDbContext` scoped provoquait par intermittence une `InvalidOperationException` (EF ConcurrencyDetector). En interrogeant `/client/demandes` (page standard protégée par `@attribute [Authorize]`), le test vérifie l'état d'authentification de façon déterministe.
- **CONTRAT HISTORIQUE CONCERNÉ** : **SEC-01** (Verrouillage des comptes et authentification obligatoire).
- **ASSERTION SUPPRIMÉE/AFFAIBLIE** : **NON**. Les deux pages portent `@attribute [Authorize]`. Sans session, une requête renvoie 302 vers `/comptes/connexion` ; avec session, elle renvoie 200 OK. La preuve de verrouillage SEC-01 est **strictement équivalente**.
- **COMPORTEMENT PRODUIT MODIFIÉ** : **NON** (0 ligne de code produit d'authentification ou de verrouillage modifiée).
- **JUSTIFICATION F012** : Assainissement du harnais de test xUnit / TestServer.
- **VERDICT** : **ACCEPTABLE**.

---

### Test 3 : `SecCert04SessionTests.cs`

- **AVANT (`a2dcc76`)** :
  ```csharp
  private async Task<bool> EstConnecteAsync(HttpClient client)
  {
      var reponse = await client.GetAsync("/comptes/double-authentification");
      return reponse.StatusCode == HttpStatusCode.OK;
  }
  ```
- **APRÈS (F012)** :
  ```csharp
  private async Task<bool> EstConnecteAsync(HttpClient client)
  {
      var reponse = await client.GetAsync("/client/demandes");
      return reponse.StatusCode == HttpStatusCode.OK;
  }
  ```
- **RAISON** : Même alignement du harnais de test HTTP que pour `SecCert01VerrouillageTests.cs`.
- **CAUSE DE L'ÉCHEC INITIAL** : Conflit de concurrence multi-composants SSR dans TestServer sur l'URL d'activation 2FA.
- **CONTRAT HISTORIQUE CONCERNÉ** : **SEC-04** / **SEC-10** (Durée de session et expiration).
- **ASSERTION SUPPRIMÉE/AFFAIBLIE** : **NON**. La vérification du timeout de session teste la transition vers l'état non-connecté sur une ressource `@attribute [Authorize]`. La preuve est **strictement équivalente**.
- **COMPORTEMENT PRODUIT MODIFIÉ** : **NON**.
- **JUSTIFICATION F012** : Assainissement du harnais.
- **VERDICT** : **ACCEPTABLE**.

---

### Test 4 : `ConnexionPremierClicTests.cs`

- **STATUT GIT** : **Fichier non suivi (`??`) dans le worktree, étranger à F012.**
- **ANALYSE** : Ce test a été introduit hors feature dans l'arborescence de test locale pour tester l'antiforgery Blazor. Il ne fait partie d'aucun commit F001→F011.
- **ACTION DE CLÔTURE** : **EXCLUSION STRICTE DU COMMIT F012** (Catégorie D — Non suivi étranger).
- **VERDICT** : **ACCEPTABLE (EXCLU DU PÉRIMÈTRE COMMIT)**.

---

## 3. PROTECTION DES PREUVES F011 (GATE 2)

- **SEC-01 (Verrouillage)** : La suite `SecCert01VerrouillageTests` (6/6 tests) valide l'authentification nominale, l'accès protégé sans session (302 vers login), le verrouillage après 5 échecs, la temporisation exponentielle, la réinitialisation après succès et l'audit.
- **SEC-04 & SEC-10 (Session)** : La suite `SecCert04SessionTests` (5/5 tests) valide la configuration de la durée de session, le sliding expiration, l'invalidation à la déconnexion et la révocation.
- **Preuve F011** : **Strictement préservée et équivalente**. Aucun affaiblissement d'assertion.
- **F011 PROOF PRESERVATION** : **PASS**.

---

## 4. ANALYSE `PlateformeWebApplicationFactory` (GATE 5)

- **Diff avec HEAD (`a2dcc76`)** :
  1. Ajout de `Microsoft.Data.SqlClient.SqlConnection.ClearAllPools()` avant `EnsureDeleted()` pour purger les connexions du pool LocalDB lors du démarrage de la suite de tests.
  2. Déclaration de `[CollectionDefinition("PlateformeWebCollection", DisableParallelization = true)]` pour sérialiser l'exécution des tests Web contre la base de test locale.
- **Impacts** :
  - Base LocalDB : Même instance (`CitPlateformeTestsWeb`).
  - Nom de base : Inchangé.
  - Lifecycle & DI : Inchangés.
  - Authentification & Cookies : Inchangés.
  - Parallélisme : Sérialisation xUnit explicite sur la collection Web.
- **Les 4 échecs initiaux étaient-ils causés par cette fixture ?** : **PARTIELLEMENT**. La concurrence multi-threads sur LocalDB provoquait des blocages de pool, résolus conjointement par `ClearAllPools()` et la sérialisation de la collection de test Web.

---

## 5. AUDIT SEC-08 : DOUBLE AUTHENTIFICATION (GATE 6)

- **Critère exigé** : Campagne de 10 exécutions consécutives à 100% de succès.
- **Résultats individuels de la campagne (Task 658)** :
  - RUN 1 : **PASS** (5/5 tests)
  - RUN 2 : **PASS** (5/5 tests)
  - RUN 3 : **PASS** (5/5 tests)
  - RUN 4 : **PASS** (5/5 tests)
  - RUN 5 : **PASS** (5/5 tests)
  - RUN 6 : **PASS** (5/5 tests)
  - RUN 7 : **PASS** (5/5 tests)
  - RUN 8 : **PASS** (5/5 tests)
  - RUN 9 : **PASS** (5/5 tests)
  - RUN 10 : **PASS** (5/5 tests)
- **TOTAL** : **10 / 10 PASS (50 / 50 exécutions unitaires sans 1 seul échec)**.
- **Code produit Auth/2FA modifié** : **0 ligne**.
- **STATUT SEC-08** : **PASS**.

---

## 6. AUDIT COM-06 : CANAUX DE RELANCE (GATE 7)

- **Architecture F009 réutilisée** :
  - Table `ReglesDiffusion` : stockage des règles `(TypeEvenement, Canal, EstObligatoire, EstActive)`.
  - Table `ModelesNotification` : gabarits de messages par canal.
  - Commandes d'administration : `AdministrerReglesDiffusionCommand`, `AdministrerModelesNotificationCommand`.
  - Interface d'administration : `/administration/notifications` (`AdministrationNotifications.razor`).
  - Interface utilisateur : `/notifications/mes-preferences` (`MesPreferencesNotification.razor`).
- **Configuration runtime sans développement** :
  - L'administrateur active/désactive les canaux InApp et Email pour l'événement `RelanceEmise` directement via l'IHM d'administration.
  - Le destinataire choisit ses canaux préférés via ses préférences personnelles.
  - Aucun redéploiement, aucune recompilation ni modification de seeder n'est requis en exploitation.
- **CONFIGURABLE SANS DÉVELOPPEMENT** : **OUI**.
- **STATUT COM-06** : **PASS**.

---

## 7. AUDIT SOC-14 : EXPORT CSV & SÉCURITÉ (GATE 8)

- **Cloisonnement Multi-Tenant (CPT-04 / SEC-03)** :
  - Test négatif Client : Client Société A exportant Société B → `AccesRefuseException` démontrée (`SOC_CERT_14_B`).
  - Test négatif Agent : Agent sans habilitation sur une file → `AccesRefuseException` démontrée (`ExporterTicketsCsvQuery.ExecuterPourAgentAsync`).
- **Sécurité et Formatage du flux CSV** :
  - Séparateur : `;`
  - Encodage : UTF-8 avec BOM (`0xEF, 0xBB, 0xBF`) pour compatibilité Excel native sans altération de caractères.
  - Échappement des champs contenant `;`, `"`, `\r`, `\n`.
  - **Neutralisation des injections CSV (Formule Tableur)** : Si une cellule commence par `=`, `+`, `-` ou `@`, elle est automatiquement préfixée par `'`.
- **STATUT SOC-14** : **PASS**.

---

## 8. AUDIT REC-SCE-14 : SCÉNARIO TRANSVERSE E2E (GATE 9)

| Volet | Action Réelle | Assertion | Couche Traversée | Mock | Preuve |
|:---|:---|:---|:---|:---|:---|
| **1. Notifications** | `SoumettreDemandeImportCommand` → `DistribuerNotificationsUseCase` | Notification InApp créée + Envoi Email créé | Application → Infrastructure (Db) | Aucun | `notifInApp != null`, `envoiEmail.Canal == Email` |
| **2. Lien sécurisé** | `GET /client/demandes` sans session | Code HTTP 302 vers `/comptes/connexion` | Web Endpoint → Identity Middleware | Aucun | `reponse.StatusCode == Redirect` |
| **3. Recherche guichet**| `ticketRepo.ObtenirTicketActifParNumeroBlAsync(bl)` | Ticket retrouvé par référence BL | Infrastructure (SQL) | Aucun | `ticketTrouve.Id == ticket1.Id` |
| **4. Rendez-vous** | `RendezVous.Creer(...)` + sauvegarde DB | `rdv.Statut == StatutRendezVous.Reserve` | Domaine → Infrastructure (SQL) | Aucun | `rdv.Statut == Reserve` |
| **5. Dossier** | `CreerTicketCommand` avec `DossierId` commun → `ConsulterDossierQuery` | Dossier contient 2 tickets liés | Application → Domaine → SQL | Aucun | `dossierComplet.Tickets.Count == 2` |
| **6. Habilitations** | `ConsulterFileQuery.ExecuterAsync(agentNonHabilite, fileId)` | `AccesRefuseException` levée | Application (AutorisationService) | Aucun | `ThrowsAsync<AccesRefuseException>` |
| **7. Journal audit** | `ConsulterJournalAuditQuery` sur le ticket | Entrée d'audit `TicketImportCree` horodatée | Application → Infrastructure | Aucun | `auditEntries.Any(a => a.Action == "TicketImportCree")` |

- **Stabilité E2E (3 runs consécutifs)** :
  - RUN 1 : **PASS**
  - RUN 2 : **PASS**
  - RUN 3 : **PASS**
- **Gain CDC** : **+0** (Scénario transverse de recette).
- **STATUT REC-SCE-14** : **PASS**.

---

## 9. SYNTHÈSE DES CONTRÔLES TECHNIQUES (GATE 10)

```text
Compilation : dotnet build → 0 Avertissement, 0 Erreur
Modèle EF Core : dotnet ef migrations has-pending-model-changes → 0 Pending Changes
Migrations créées : 0

Résultats de la suite de tests :
- CIT.Plateforme.Domain.Tests          : 158 / 158 verts
- CIT.Plateforme.Application.Tests     : 447 / 447 verts
- CIT.Plateforme.Infrastructure.Tests  :  83 /  83 verts
- CIT.Plateforme.Web.Tests             : 179 / 179 verts
TOTAL GLOBAL : 867 / 867 VERTS (100 %)
```

---

## 10. BILAN & VERDICT DE CLÔTURE

- **BLOCKERS IDENTIFIÉS** : **0**
- **GAIN CDC AUTORISABLE** : **+7 exigences** (PRI-02, IMP-03, REC-02, REC-03, SEC-08, SOC-14, COM-06)
- **COUVERTURE CDC AUTORISABLE** : **164 / 188 exigences conformes = 87,23 %**

---

**VERDICT : READY FOR POST-IMPLEMENTATION AUDIT**

STOP.
