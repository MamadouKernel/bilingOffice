# CLOSURE REVIEW — FEATURE 017

## Sécurité Granulaire Métier & Rétention Documentaire Réglementaire (ARTCI)

---

### 1. SYNTHÈSE FORENSIQUE DE CLÔTURE

- **Feature** : 017 — Sécurité Granulaire Métier & Rétention Documentaire Réglementaire (ARTCI)
- **Baseline technique** :
  - Compilation : **SUCCESS (0 avertissement, 0 erreur)**
  - EF Core : **0 pending model changes** (1 migration additive créée `20260902151046_AjoutRetentionEtGelLegal`)
  - Suite de tests : **1 041 / 1 041 tests verts (100 %)**
    - Domain : 158 / 158
    - Application : 560 / 560
    - Infrastructure : 94 / 94
    - Web : 229 / 229
- **Baseline CDC certifiée** : **174 / 188 (92,55 %)** (+2 validés par cette review)
  - `SEC-02` (Must) : **CONFORME & CERTIFIÉE (+1)**
  - `SEC-06` (Must) : **CONFORME & CERTIFIÉE (+1)**
- **Projection post-F016 Environment Gate** : **jusqu'à 177 / 188 (94,15 %)** (Preuves NFR-03/04/10 différées)

---

### 2. AUDIT DE LA CHAÎNE CONTRACTUELLE

#### SEC-02 (Must) — Contrôle d'Accès par Rôles au Niveau Champ (RBAC IES)
- **Traçabilité** : CDC §17 $\rightarrow$ PC-05 (`docs/30-dsi-pc-clarification-gate.md`) $\rightarrow$ `spec.md` (DEC-017-01) $\rightarrow$ `plan.md` $\rightarrow$ `tasks.md` $\rightarrow$ `AutorisationChampService.cs` $\rightarrow$ `SecCert02RbacChampTests.cs` (8/8).
- **Moteur & Architecture** :
  - Moteur centralisé : `AutorisationChampService` (Source unique).
  - Évaluation : **BLOCK LEVEL** (Manifeste, Douane, Financier, Chauffeur) avec projection **FIELD LEVEL** (`null` en API, `[RESTREINT]` en export CSV).
  - Fail-secure : PASS (Rôle/bloc/champ inconnu $\implies$ `Masked` / Rejet 403).
  - Audit : Refus tracés dans `JournalAudit` sans fuite de PII.
- **Verdict SEC-02** : **CONFORME & CERTIFIÉE**.

#### SEC-06 (Must) — Rétention Documentaire & Cycle de Vie ARTCI
- **Traçabilité** : CDC §17 $\rightarrow$ PC-14 (`docs/30-dsi-pc-clarification-gate.md`) $\rightarrow$ `spec.md` (DEC-017-02..08) $\rightarrow$ `plan.md` $\rightarrow$ `tasks.md` $\rightarrow$ `MoteurRetentionService.cs` & `GestionnaireRetentionService.cs` $\rightarrow$ `SecCert06RetentionTests.cs` (12/12).
- **Moteur & Délais** :
  - Fondé exclusivement sur `IHorloge` (0 `DateTime.Now` direct).
  - Factures comptables : 10 ans $\implies$ Scellement SHA-256 en lecture seule.
  - Dossiers opérationnels : 5 ans $\implies$ Anonymisation irréversible des PII.
  - Journaux d'audit : 3 ans $\implies$ Maintien inaltérable (0 DELETE).
  - Invariant append-only : **100 % PRÉSERVÉ (0 suppression physique sur `JournalAudit`)**.
  - Brouillons : 30 jours $\implies$ Purge automatique des abandons, récents protégés (`ERR-01`).
  - Gel Légal (*Legal Hold*) : `EstSousGelLegal` suspendant toute purge ou anonymisation.
  - GED & Lot : Suppression physique hermétique F015, `LotsPurgeRetention` sans PII.
  - Idempotence & Concurrence : Vérifiées et protégées.
- **Verdict SEC-06** : **CONFORME & CERTIFIÉE**.

---

### 3. STATUT DES CONTRATS & TEST-SPECS

| Réf Contrat / Spec | Intitulé | Couverture Code | Preuve Test | Statut |
|:---|:---|:---|:---|:---:|
| **`C17-01`** | Évaluation RBAC champ | `AutorisationChampService` | `SecCert02RbacChampTests` | **PROVEN** |
| **`C17-02`** | Masquage API & Export | `AutorisationChampService` | `SecCert02RbacChampTests` | **PROVEN** |
| **`C17-03`** | Moteur Rétention Temporelle | `MoteurRetentionService` | `MoteurRetentionServiceTests` | **PROVEN** |
| **`C17-04`** | Orchestration Purge/Anonymisation | `GestionnaireRetentionService` | `SecCert06RetentionTests` | **PROVEN** |
| **`C17-05`** | Gel Légal & Traçabilité | `GestionnaireRetentionService` | `SecCert06RetentionTests` | **PROVEN** |
| **`SEC-CERT-02`** | Suite nominative RBAC (8 cas) | 8 scénarios nominatifs | 8 / 8 PASS | **PROVEN** |
| **`SEC-CERT-06`** | Suite nominative Rétention (12 cas)| 12 scénarios nominatifs | 12 / 12 PASS | **PROVEN** |

---

### 4. MODÈLE DE DONNÉES & MIGRATION

- **Migration EF Core** : `20260902151046_AjoutRetentionEtGelLegal.cs` (100 % Additive).
- **Tables créées** : `LotsPurgeRetention` (1 table).
- **Colonnes ajoutées** :
  - `Ticket` : `EstSousGelLegal` (bool), `DateArchivageUtc` (DateTime?), `EstAnonymise` (bool).
  - `Document` : `EstScelle` (bool), `DateScellementUtc` (DateTime?), `EmpreinteScellementSha256` (string?).
- **Drop Table / Drop Column** : **0**.
- **Data Loss** : **0**.
- **Pending Model Changes** : **0**.

---

### 5. PRÉSERVATION DES FONCTIONNALITÉS HISTORIQUES

- **F015 (GED / Defender)** : **PASS** (Ports hermétiques, isolation des flux binaires).
- **ERR-01 (Brouillons)** : **PASS** (Reprise de session préservée pour les formulaires actifs).
- **F016 (Observabilité / SLA L2)** : **PASS** (`/healthz` et sondes d'infrastructure).
- **F016 (Environment Evidence)** : **DEFERRED** (Preuves L3/L4 différées en staging).
- **Authentification & 2FA** : **PASS**.
- **API REST NFR-08** : **PASS**.
- **F009 (Notifications multicanales)** : **PASS**.
- **Anti-Scope** : **PASS** (0 développement hors périmètre).

---

### 6. STATUT DU BACKLOG (TASKS.MD)

- **T001 à T028** : **DONE (28/30)**
- **T029** : Préparer le commit atomique F017 $\implies$ **COMMIT_PENDING**
- **T030** : Clôturer F017 et mettre à jour le tableau de bord $\implies$ **PENDING**

---

### 7. DÉCISION FINALE DU COMITÉ DE CLÔTURE

1. **Exigences certifiées** : `SEC-02` (+1) et `SEC-06` (+1).
2. **Gain certifié officiel** : **+2**.
3. **Nouvelle baseline CDC post-F017** : **174 / 188 (92,55 %)**.
4. **Autorisation de commit** : **ACCORDÉE (Prêt pour le Commit Gate T029)**.
