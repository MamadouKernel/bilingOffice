# BACKLOG DES TÂCHES TECHNIQUES — FEATURE 017

## Sécurité Granulaire Métier & Rétention Documentaire Réglementaire (ARTCI)

---

### Phase 1 — Baseline & Worktree Safety
- [x] **T001** : Valider la baseline technique initiale (Build 0 warning, 991 tests verts, 0 pending model changes) et configurer l'isolation du worktree.
- [x] **T002** : Définir la whitelist forensique des fichiers F017 et interdire strictement `git add .` et `git add -A`.

---

### Phase 2 — SEC-02 : Moteur d'Autorisation RBAC par Champ (Domain & Application)
- [x] **T003** : [TEST RED] Rédiger les tests unitaires RED pour `AutorisationChampService` (`AutorisationChampServiceTests.cs`) couvrant les 4 blocs de données et 5 profils avec cas fail-secure.
- [x] **T004** : Créer les enums `BlocDonneesIes` et `EtatDroitChamp` dans `CIT.Plateforme.Domain.Securite`.
- [x] **T005** : Implémenter l'interface `IAutorisationChampService` et la classe `AutorisationChampService` dans `CIT.Plateforme.Application.Securite` appliquant la matrice DEC-017-01 (GREEN).
- [x] **T006** : Enregistrer `AutorisationChampService` dans `DependencyInjection.cs` de la couche Application/Infrastructure.

---

### Phase 3 — SEC-02 : Intégrations API REST, Export CSV & UI Blazor
- [x] **T007** : [TEST RED] Rédiger les tests d'intégration RED pour la projection des DTOs d'API et l'export CSV (`Sec02ProjectionEtExportTests.cs`).
- [x] **T008** : Implémenter la projection sécurisée dans les requêtes de consultation et l'API REST NFR-08 (assignation à `null` des champs financiers masqués, rejet 403 sur écriture non autorisée).
- [x] **T009** : Adapter les services d'export CSV pour masquer les colonnes financières par `[RESTREINT]` pour les rôles non habilités en préservant la protection injection CSV (F012).

---

### Phase 4 — SEC-02 : Traçabilité des Refus & Certification `SEC-CERT-02`
- [x] **T010** : [TEST RED] Rédiger les tests RED de journalisation d'audit des refus de mutation champ dans `JournalAudit` (`SEC-05`) sans exposition de valeur confidentielle.
- [x] **T011** : Rédiger et valider la suite de certification nominative `SecCert02RbacChampTests.cs` (8 scénarios validant C17-01 et C17-02).

---

### Phase 5 — SEC-06 : Modèle de Données & Migration Additive
- [x] **T012** : Étendre les entités Domaine `Ticket` (`EstSousGelLegal`, `DateArchivageUtc`, `EstAnonymise`) et `Document` (`EstScelle`, `DateScellementUtc`, `EmpreinteScellementSha256`).
- [x] **T013** : Créer l'entité `LotPurgeRetention` dans `CIT.Plateforme.Domain.Supervision` et mapper `DbSet<LotPurgeRetention>` dans `PlateformeDbContext`.
- [x] **T014** : Générer la migration EF Core additive `AjoutRetentionEtGelLegal`, inspecter pour vérifier 0 DROP TABLE / 0 DROP COLUMN, et valider `dotnet ef migrations has-pending-model-changes`.

---

### Phase 6 — SEC-06 : Moteur Temporel de Rétention & Gel Légal
- [x] **T015** : [TEST RED] Rédiger les tests unitaires purs RED pour `MoteurRetentionService` (`MoteurRetentionServiceTests.cs`) fondés sur `IHorloge` (30j, 3 ans, 5 ans, 10 ans, gel légal).
- [x] **T016** : Implémenter `MoteurRetentionService` dans `CIT.Plateforme.Application.Securite` évaluant l'éligibilité sans effet de bord (GREEN).
- [x] **T017** : Implémenter les commandes d'activation/désactivation du gel légal `ActiverGelLegalTicketCommand` et `DesactiverGelLegalTicketCommand` avec traçabilité `SEC-05`.

---

### Phase 7 — SEC-06 : Exécution de Purge, Anonymisation & Traçabilité de Lot
- [x] **T018** : [TEST RED] Rédiger les tests d'intégration RED pour la purge des brouillons expirés, l'anonymisation des dossiers et le scellement comptable (`GestionnaireRetentionTests.cs`).
- [x] **T019** : Implémenter `GestionnaireRetentionService` réalisant la purge physique GED (F015), l'anonymisation irréversible des PII (`ANONYMISE`) et le scellement SHA-256 avec garantie d'idempotence.
- [x] **T020** : Enregistrer le certificat d'audit de lot dans `LotsPurgeRetention` sans ré-exposition de données personnelles.

---

### Phase 8 — SEC-06 : Certification Temporelle & Invariant Append-Only `SEC-CERT-06`
- [x] **T021** : Vérifier que l'invariant append-only de `JournalAudit` est 100% préservé et rejouer `JournalAuditAppendOnlyTests`.
- [x] **T022** : Rédiger et valider la suite de certification nominative `SecCert06RetentionTests.cs` (12 scénarios temporels validant C17-03, C17-04 et C17-05).

---

### Phase 9 — Non-Régression Globale & Hardening
- [x] **T023** : Rejouer la suite de tests complète et vérifier la préservation de F015 (Antivirus/GED), F013 (Brouillons ERR-01), F016 (Health/SLA).
- [x] **T024** : Vérifier les tests négatifs de sécurité (IDOR, mutations directes, rôles non mappés, failles de concurrence).
- [x] **T025** : Exécuter le scan Anti-Scope pour confirmer 0 ligne de code sur NFR-03/04/10, NFR-05, NFR-09, ENR-04, IA.

---

### Phase 10 — Audit de Non-Régression & Clôture
- [x] **T026** : Exécuter le Regression Gate Audit exhaustif (53 points de contrôle), vérifier la non-régression globale et consigner `regression-audit.md`.
- [x] **T027** : Exécuter le Post-Implementation Audit formel et consigner `post-implementation-audit.md`.
- [x] **T028** : Exécuter la Closure Review finale et rédiger `closure-review.md`.
- [x] **T029** : Préparer le commit atomique F017 et valider le Commit Gate.
- [ ] **T030** : Clôturer F017 et mettre à jour le tableau de bord CDC global.
