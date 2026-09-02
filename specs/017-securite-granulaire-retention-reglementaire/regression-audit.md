# REGRESSION GATE AUDIT REPORT — FEATURE 017

## Sécurité Granulaire Métier & Rétention Documentaire Réglementaire (ARTCI)

---

### 1. BASELINE TECHNIQUE

- **Build** : `dotnet build /p:UseSharedCompilation=false` $\implies$ **SUCCESS (0 avertissement, 0 erreur)**
- **EF Migrations** : `dotnet ef migrations has-pending-model-changes` $\implies$ **0 pending model changes**
- **Tests exécutés** : `dotnet test -- NUnit.NumberOfTestWorkers=1 xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=1` $\implies$ **1 041 / 1 041 tests verts (100 %)**
  - Domain : 158 / 158
  - Application : 560 / 560
  - Infrastructure : 94 / 94
  - Web : 229 / 229
- **Statut Baseline** : **PASS**

---

### 2. DIFF FORENSIQUE

- `git status --short` :
  - `M src/CIT.Plateforme.Domain/Documents/Document.cs` [A — Code F017]
  - `M src/CIT.Plateforme.Domain/Tickets/Ticket.cs` [A — Code F017]
  - `M src/CIT.Plateforme.Infrastructure/DependencyInjection.cs` [A — Code F017]
  - `M src/CIT.Plateforme.Infrastructure/Persistence/Migrations/PlateformeDbContextModelSnapshot.cs` [C — Migration F017]
  - `M src/CIT.Plateforme.Infrastructure/Persistence/PlateformeDbContext.cs` [A/C — Code/Migration F017]
  - `?? src/CIT.Plateforme.Application/Securite/AutorisationChampService.cs` [A — Code F017]
  - `?? src/CIT.Plateforme.Application/Securite/IAutorisationChampService.cs` [A — Code F017]
  - `?? src/CIT.Plateforme.Application/Securite/MoteurRetentionService.cs` [A — Code F017]
  - `?? src/CIT.Plateforme.Domain/Securite/BlocDonneesIes.cs` [A — Code F017]
  - `?? src/CIT.Plateforme.Domain/Securite/CategorieRetention.cs` [A — Code F017]
  - `?? src/CIT.Plateforme.Domain/Securite/EtatDroitChamp.cs` [A — Code F017]
  - `?? src/CIT.Plateforme.Domain/Supervision/LotPurgeRetention.cs` [A — Code F017]
  - `?? src/CIT.Plateforme.Infrastructure/Persistence/Migrations/20260902151046_AjoutRetentionEtGelLegal.Designer.cs` [C — Migration F017]
  - `?? src/CIT.Plateforme.Infrastructure/Persistence/Migrations/20260902151046_AjoutRetentionEtGelLegal.cs` [C — Migration F017]
  - `?? src/CIT.Plateforme.Infrastructure/Securite/GestionnaireRetentionService.cs` [A — Code F017]
  - `?? tests/CIT.Plateforme.Application.Tests/Securite/AutorisationChampServiceTests.cs` [B — Tests F017]
  - `?? tests/CIT.Plateforme.Application.Tests/Securite/MoteurRetentionServiceTests.cs` [B — Tests F017]
  - `?? tests/CIT.Plateforme.Web.Tests/Securite/SecCert02RbacChampTests.cs` [B — Tests F017]
  - `?? tests/CIT.Plateforme.Web.Tests/Securite/SecCert06RetentionTests.cs` [B — Tests F017]
- **Catégorie E (Test historique modifié)** : **0**
- **Catégorie F (Étranger préexistant)** : **0**
- **Fichiers stagés (`git diff --cached`)** : **0**

---

### 3. RECONSTITUTION 991 $\rightarrow$ 1 041 TESTS (+50 TESTS)

- **Domain** : 158 $\rightarrow$ 158 (+0)
- **Application** : 530 $\rightarrow$ 560 (+30)
  - `AutorisationChampServiceTests.cs` : +23 tests unitaires RBAC champ
  - `MoteurRetentionServiceTests.cs` : +7 tests unitaires rétention pure
- **Infrastructure** : 94 $\rightarrow$ 94 (+0)
- **Web** : 209 $\rightarrow$ 229 (+20)
  - `SecCert02RbacChampTests.cs` : +8 tests de certification SEC-CERT-02
  - `SecCert06RetentionTests.cs` : +12 tests de certification SEC-CERT-06
- **TOTAL** : **+50 tests** (100 % issus de Feature 017, 0 test préexistant étranger).

---

### 4. SEC-02 — CHAÎNE DE PREUVE

- **CDC** : Exigence Must SEC-02 (« Contrôle d'accès par rôles au niveau champ pour les traitements parallèles IES »).
- **Atelier PC-05** : Résolution formelle dans `docs/30-dsi-pc-clarification-gate.md` (4 blocs x 5 profils).
- **Spécification** : `spec.md` & `checklists/requirements.md` (DEC-017-01).
- **Plan / Tâches** : `plan.md` & `tasks.md` (T003 à T011).
- **Code** : `AutorisationChampService.cs` dans `CIT.Plateforme.Application.Securite`.
- **Tests** : 23 tests unitaires + 8 tests de certification `SecCert02RbacChampTests.cs` verts.
- **Verdict** : **PROVEN (Côté serveur, fail-secure, non contournable par l'UI)**.

---

### 5. AUTORISATIONCHAMPSERVICE

- **Source unique de décision** : OUI (`AutorisationChampService.cs`).
- **Matrice non dupliquée** : OUI (centralisée dans un `switch` exhaustif).
- **Fail-secure** : PASS (Rôle `null`, vide ou inconnu $\implies$ `EtatDroitChamp.Masked`).
- **Permissions inconnues** : Refusées / masquées par défaut.
- **Bypass Admin** : NON (L'administrateur a un profil `ReadOnly` explicite pour l'audit, sans bypass d'écriture non tracé).

---

### 6. MATRICE RBAC

| Profil | Manifeste | Douane/CD | Financier | Chauffeur | Conforme DEC-017-01 |
|:---|:---:|:---:|:---:|:---:|:---:|
| **PiloteManifeste** | `AllowWrite` | `ReadOnly` | `Masked` | `AllowWrite` | **OUI** |
| **PiloteCd** | `ReadOnly` | `AllowWrite` | `Masked` | `ReadOnly` | **OUI** |
| **Facturation / Caisse** | `ReadOnly` | `ReadOnly` | `AllowWrite` | `Masked` | **OUI** |
| **Superviseur** | `AllowWrite` | `AllowWrite` | `AllowWrite` | `AllowWrite` | **OUI** |
| **Administrateur** | `ReadOnly` | `ReadOnly` | `ReadOnly` | `ReadOnly` | **OUI** |
| **Inconnu / Non-mappé** | `Masked` | `Masked` | `Masked` | `Masked` | **OUI (Fail-Secure)** |

---

### 7. FIELD-LEVEL VS BLOCK-LEVEL

- **Moteur réel** : **BLOCK LEVEL & FIELD PROJECTION LEVEL**.
- **Preuve** : Le moteur évalue les 4 blocs conceptuels (`Manifeste`, `Douane`, `Financier`, `Chauffeur`) et mappe directement sur les champs individuels DTOs/projections (`null` sur les montants financiers, masquage des numéros de permis, etc.).
- **Adéquation CDC** : **CONFORME** (Le CDC §17 regroupe les champs IES en sections fonctionnelles étanches).

---

### 8. API REST (NFR-08)

- **DTOs et projections** : Les champs masqués sont assignés à `null` côté serveur lors de la projection.
- **Protection mutation** : Rejet HTTP 403 / `AccesInterditException` en cas de tentative d'écriture non autorisée.
- **Verdict** : **PASS**.

---

### 9. EXPORT CSV

- **Format** : Les colonnes financières pour les rôles non habilités sont remplacées par `[RESTREINT]`.
- **Fuite de données** : Aucune valeur brute présente dans les logs, URLs ou colonnes alternatives.
- **Protection injection CSV** : Préserve la politique SOC-14 / F012.
- **Verdict** : **PASS**.

---

### 10. WRITE FORBIDDEN

- **Évaluation au niveau Application** : Vérifiée avant d'exécuter toute mutation sur l'entité.
- **Verdict** : **PASS**.

---

### 11. ROLE UNKNOWN & 12. BLOCK UNKNOWN

- **Rôle inconnu / chaîne vide / null** $\implies$ `EtatDroitChamp.Masked` (Testé dans `Scenario6_RoleInconnuOuNonMappe_FailSecure_MasquageTotal`).
- **Bloc inconnu** $\implies$ `EtatDroitChamp.Masked`.
- **Verdict** : **PASS**.

---

### 13. ADMIN / SUPERVISEUR

- Passent tous deux par la matrice `AutorisationChampService` sans embranchement sauvage.
- **Verdict** : **PASS**.

---

### 14. AUDIT DES REFUS

- Traçabilité assurée dans `JournalAudit` (`SEC-05`) avec `TypeEvenementAudit.Securite` sans divulgation des données sensibles.
- **Verdict** : **PASS**.

---

### 15. SEC-CERT-02 (8 SCÉNARIOS)

1. `Scenario1_PiloteManifeste_PeutEcrireManifesteEtChauffeur_MaisPasDouaneNiFinancier` : **PASS**
2. `Scenario2_PiloteCd_PeutEcrireDouane_MaisPasManifesteNiFinancier` : **PASS**
3. `Scenario3_Facturation_PeutEcrireFinancier_MaisChauffeurMasque` : **PASS**
4. `Scenario4_Superviseur_AccesCompletEnEcritureSurTousLesBlocs` : **PASS**
5. `Scenario5_Administrateur_LectureSeuleAuditSurTousLesBlocs` : **PASS**
6. `Scenario6_RoleInconnuOuNonMappe_FailSecure_MasquageTotal` : **PASS**
7. `Scenario7_MasquageExportCsv_FormatteValeurRestreinteCorrectement` : **PASS**
8. `Scenario8_MasquageApiDto_AssignationNullSurChampMasque` : **PASS**

---

### 16. SEC-06 — CHAÎNE DE PREUVE

- **CDC** : Exigence Must SEC-06 (« Rétention, archivage, purge et protection des données personnelles »).
- **Atelier PC-14** : Arbitrage DSI validé dans `docs/30-dsi-pc-clarification-gate.md`.
- **Spécification / Plan** : `spec.md` & `plan.md` (DEC-017-02 à DEC-017-08).
- **Modèle / Migration** : `LotPurgeRetention`, extensions `Ticket` et `Document`, migration additive `AjoutRetentionEtGelLegal`.
- **Moteur & Orchestration** : `MoteurRetentionService` (Application) & `GestionnaireRetentionService` (Infrastructure).
- **Tests** : 7 tests unitaires + 12 tests de certification `SecCert06RetentionTests.cs` verts.
- **Verdict** : **PROVEN**.

---

### 17. MOTEURRETENTIONSERVICE & 18. DURÉES & 19. DATES DE RÉFÉRENCE

- **IHorloge** : Moteur 100 % pur sans appel direct à `DateTime.Now`.
- **Règles et durées** :
  - **Comptable** : 10 ans depuis `DateEmissionUtc` $\implies$ Scellement & lecture seule.
  - **Dossiers** : 5 ans depuis `DateClotureUtc` $\implies$ Anonymisation irréversible.
  - **Audit** : 3 ans depuis `DateCreationUtc` $\implies$ Maintien inaltérable (0 DELETE).
  - **Brouillons** : 30 jours depuis `DateDerniereModificationUtc` $\implies$ Purge automatique.
- **Verdict** : **PASS**.

---

### 20. BORNES TEMPORELLES

- Brouillon à 29 jours $\implies$ Non éligible (`EstEligible = false`).
- Brouillon à 30 jours et plus $\implies$ Éligible à la purge (`Action = Purge`).
- Dossier à 4 ans 364 jours $\implies$ Non éligible.
- Dossier à 5 ans + 1 jour $\implies$ Éligible anonymisation.
- **Verdict** : **PASS**.

---

### 21. AUDIT 3 ANS & 22. TESTS APPEND-ONLY HISTORIQUES

- **Recherche de suppressions** :
  - `grep "Delete.*JournalAudit"` $\implies$ **0 résultat**.
  - `grep "Remove.*JournalAudit"` $\implies$ **0 résultat**.
- **Action post 3 ans** : Maintien inaltérable scellé sans aucune suppression physique SQL.
- **Invariant append-only** : **PRESERVED (100 %)**.
- **Verdict** : **PASS**.

---

### 23. COMPTABLE 10 ANS & 24. HASH SHA-256

- Les factures et pièces comptables sont scellées avec empreinte SHA-256 (`EmpreinteScellementSha256`) et date de scellement (`DateScellementUtc`).
- Réutilise les abstractions cryptographiques du socle sans divergence.
- **Verdict** : **PASS**.

---

### 25. DOSSIERS 5 ANS, 26. ANONYMISATION PII & 27. FK / INTÉGRITÉ

- Anonymisation irréversible via flag d'archivage et écrasement contrôlé des PII.
- L'intégrité référentielle (`DossierId`, `TicketId`, liaisons, montants financiers pour reporting) est strictement préservée.
- **Verdict** : **PASS**.

---

### 28. BROUILLONS 30 JOURS

- Les brouillons récents ($< 30$ jours) nécessaires à la reprise de session `ERR-01` sont protégés.
- Les brouillons abandonnés ($> 30$ jours) sont purgés avec traçabilité dans `LotsPurgeRetention`.
- **Verdict** : **PASS**.

---

### 29. LEGAL HOLD (GEL LÉGAL)

- Suspension garantie de toute purge ou anonymisation lorsque `EstSousGelLegal == true`.
- Débrayage immédiat lors de la désactivation du gel par un agent habilité avec traçabilité audit.
- **Verdict** : **PASS**.

---

### 30. LOTPURGERETENTION & 31. GESTION DES ERREURS

- Table `LotsPurgeRetention` créée sans aucune PII (colonnes : `Id`, `DateExecutionUtc`, `Categorie`, `NombreElementsTraites`, `EstSucces`, `MessageErreur`).
- Les statuts d'erreur consignent l'échec sans faux succès.
- **Verdict** : **PASS**.

---

### 32. GED & 33. ERREUR PARTIELLE & 34. IDEMPOTENCE

- Purge physique s'appuyant sur les ports de stockage sécurisés (F015).
- Passes multiples vérifiées : 2ème passage $\implies 0$ élément retraité, idempotence totale.
- **Verdict** : **PASS**.

---

### 35. CONCURRENCE & 36. FIRST RUN SAFETY

- **Stratégie** : Traitement par requêtes filtrées avec transaction unitaire par lot et version de concurrence optimiste sur les entités modifiées.
- **Multi-instance safe** : OUI (Filtres stricts `!EstAnonymise` et `DateDerniereModificationUtc <= seuilUtc`).
- **First Run Safety** : Bornes temporelles explicites empêchant toute purge non intentionnelle.
- **Verdict** : **PASS**.

---

### 37. MIGRATION EF CORE `AjoutRetentionEtGelLegal`

- **DropTable** : 0
- **DropColumn** : 0
- **RenameColumn** : 0
- **SQL destructif** : 0
- **Colonnes ajoutées** :
  - `Tickets.EstSousGelLegal` (bit, not null, default: false)
  - `Tickets.DateArchivageUtc` (datetime2, nullable)
  - `Tickets.EstAnonymise` (bit, not null, default: false)
  - `Documents.EstScelle` (bit, not null, default: false)
  - `Documents.DateScellementUtc` (datetime2, nullable)
  - `Documents.EmpreinteScellementSha256` (nvarchar(max), nullable)
  - Table `LotsPurgeRetention` (PK uniqueidentifier, DateExecutionUtc datetime2, Categorie nvarchar(max), NombreElementsTraites int, EstSucces bit, MessageErreur nvarchar(max) nullable)
- **Verdict** : **PASS (100 % Additive)**.

---

### 38. BACKFILL & 39. VALEURS PAR DÉFAUT & 40. SNAPSHOT & 41. DATA LOSS

- Anciennes lignes : booléens à `false`, dates et hash à `null` $\implies 0$ date fictive injectée.
- `PlateformeDbContextModelSnapshot.cs` synchronisé.
- `has-pending-model-changes` $\implies 0$.
- Perte de données $\implies 0$.
- **Verdict** : **PASS**.

---

### 42. F015, 43. F013, 44. F016, 45. AUTH/2FA, 46. API REST, 47. F009

- F015 (Antivirus Defender / GED) : **PASS**
- F013 (Brouillons `ERR-01`) : **PASS**
- F016 (Health Checks NFR-03/04/10 L2) : **PASS**
- Authentification & 2FA : **PASS**
- API REST NFR-08 : **PASS**
- F009 (Notifications multicanales) : **PASS**

---

### 48. TESTS HISTORIQUES TOUCHÉS

- **Nombre de tests historiques modifiés** : **0**
- **Force probante historique** : **100 % PRÉSERVÉE**.

---

### 49. SCAN ANTI-SCOPE

- 0 code écrit sur NFR-03/04/10 (Preuves réelles L3/L4 différées en Environment Gate).
- 0 code sur NFR-05, NFR-09, ENR-04, IA, SMS/WhatsApp externes.
- **Verdict** : **PASS**.

---

### 50. STATUT DES TÂCHES (TASKS.MD)

- **T001 à T025** : **DONE (25/30)**
- **T026** : REGRESSION_GATE_AUDIT $\implies$ **DONE (Ce livrable)**
- **T027** : POST_IMPLEMENTATION_AUDIT $\implies$ **PENDING**
- **T028** : CLOSURE_REVIEW $\implies$ **PENDING**
- **T029** : COMMIT_GATE $\implies$ **PENDING**
- **T030** : POST_CLOSURE $\implies$ **PENDING**

---

### 51. BILAN DU GAIN CDC

- **Baseline CDC actuelle** : **172 / 188 (91,49 %)**
- **Gain certifiable F017** : **+2 (SEC-02, SEC-06)**
- **Projection CDC post-clôture** : **174 / 188 (92,55 %)**
- **Autorisation de gain au Regression Gate** : **AUTORISÉ (Conditionné à la Closure Review finale)**.

---

### 52. LIVRABLE

- `specs/017-securite-granulaire-retention-reglementaire/regression-audit.md` (Présent document).

---

### 53. VERDICT FINAL DU REGRESSION GATE

- **VERDICT** : **READY FOR POST-IMPLEMENTATION AUDIT**
- **Bloqueurs** : **0**
- **Avertissements** : **0**
- **Régressions** : **0**
