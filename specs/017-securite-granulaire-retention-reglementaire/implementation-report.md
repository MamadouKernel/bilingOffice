# RAPPORT D'IMPLÉMENTATION TECHNIQUE — FEATURE 017

## Sécurité Granulaire Métier & Rétention Documentaire Réglementaire (ARTCI)

---

### 1. SYNTHÈSE FORENSIQUE

- **Feature** : 017 — Sécurité Granulaire Métier & Rétention Documentaire Réglementaire (ARTCI)
- **Baseline technique** :
  - Compilation : **SUCCESS (0 avertissement, 0 erreur)**
  - EF Core : **0 pending model changes** (1 migration additive créée `AjoutRetentionEtGelLegal`)
  - Suite de tests : **1 041 / 1 041 tests verts (100 %)**
    - Domain : 158 / 158
    - Application : 560 / 560
    - Infrastructure : 94 / 94
    - Web : 229 / 229
- **Baseline CDC officielle** : **172 / 188 (91,49 %)**
- **Gain potentiel F017** : **+2** (Conditionné par la validation du Post-Implementation Audit & Closure Review)
- **Projection max F017** : **174 / 188 (92,55 %)**

---

### 2. EXIGENCES CONTRACTUELLES LIVRÉES

#### SEC-02 (Must) — Contrôle d'Accès par Rôles au Niveau Champ (RBAC IES)
- **Composants livrés** :
  - `BlocDonneesIes.cs` & `EtatDroitChamp.cs` dans `CIT.Plateforme.Domain.Securite`
  - `IAutorisationChampService.cs` & `AutorisationChampService.cs` dans `CIT.Plateforme.Application.Securite`
- **Garanties techniques** :
  - Matrice validée DEC-017-01 appliquée côté serveur avec fail-secure absolu (droit par défaut = `Masked`).
  - Masquage contextuel dans les requêtes de consultation et exports CSV (`[RESTREINT]`).
  - Suite de certification `SecCert02RbacChampTests.cs` : **8 / 8 tests verts**.

#### SEC-06 (Must) — Rétention Documentaire & Cycle de Vie ARTCI
- **Composants livrés** :
  - `CategorieRetention.cs` & `ActionRetention.cs` dans `CIT.Plateforme.Domain.Securite`
  - `MoteurRetentionService.cs` dans `CIT.Plateforme.Application.Securite` (moteur pur basé sur `IHorloge`)
  - `GestionnaireRetentionService.cs` dans `CIT.Plateforme.Infrastructure.Securite`
  - `LotPurgeRetention.cs` dans `CIT.Plateforme.Domain.Supervision`
  - Extensions sur `Ticket` (`EstSousGelLegal`, `DateArchivageUtc`, `EstAnonymise`) et `Document` (`EstScelle`, `DateScellementUtc`, `EmpreinteScellementSha256`)
- **Garanties techniques** :
  - Rétention décennale des factures scellées (10 ans).
  - Anonymisation irréversible des dossiers opérationnels clôturés (5 ans).
  - Maintien inaltérable des journaux d'audit (3 ans, 0 DELETE).
  - Purge automatique des brouillons abandonnés ($> 30$ jours).
  - Débrayage par gel légal (*Legal Hold*) préservé.
  - Idempotence absolue et 0 PII dans les traces de lots.
  - Suite de certification `SecCert06RetentionTests.cs` : **12 / 12 tests verts**.

---

### 3. STATUT DES CONTRATS C17

| Contrat | Description | Implémentation | Preuve Test | Statut |
|:---|:---|:---|:---|:---:|
| **`C17-01`** | Évaluation RBAC par champ | `AutorisationChampService` | `SecCert02RbacChampTests` (8/8) | **IMPLEMENTED** |
| **`C17-02`** | Projection et masquage contextuel | `AutorisationChampService` | `SecCert02RbacChampTests` | **IMPLEMENTED** |
| **`C17-03`** | Évaluation de rétention temporelle | `MoteurRetentionService` | `MoteurRetentionServiceTests` (7/7) | **IMPLEMENTED** |
| **`C17-04`** | Exécution de purge / anonymisation | `GestionnaireRetentionService` | `SecCert06RetentionTests` (12/12) | **IMPLEMENTED** |
| **`C17-05`** | Gestion du gel légal & traçabilité | `GestionnaireRetentionService` | `SecCert06RetentionTests` | **IMPLEMENTED** |

---

### 4. PRÉSERVATION DES FONCTIONNALITÉS HISTORIQUES & NON-RÉGRESSION

- **Invariant Append-Only `JournalAudit`** : **PASS** (0 suppression, tests historiques préservés).
- **F015 (Sécurité & Intégrité GED)** : **PASS** (Stockage hermétique, antivirus Defender CLI préservé).
- **F013 (Brouillons `ERR-01`)** : **PASS** (Brouillons récents protégés).
- **F016 (Observabilité & Sondes SLA)** : **PASS** (`/healthz` et `/ready` inchangés).
- **Authentification & 2FA** : **PASS**.
- **API REST (NFR-08)** : **PASS**.
- **Notifications (F009)** : **PASS**.
- **Anti-Scope** : **PASS** (0 code sur NFR-03/04/10, NFR-05, NFR-09, ENR-04, IA).
