# PLAN TECHNIQUE DURCI — FEATURE 017

## Sécurité Granulaire Métier & Rétention Documentaire Réglementaire (ARTCI)

---

### 1. SYNTHÈSE & BASELINE

- **Feature** : 017 — Sécurité Granulaire Métier & Rétention Documentaire Réglementaire (ARTCI)
- **Exigences CDC** :
  - **`SEC-02`** (Must) : Contrôle d'accès RBAC au niveau champ pour les traitements parallèles IES.
  - **`SEC-06`** (Must) : Protection des données personnelles & politique de rétention ARTCI (Loi n° 2013-450).
- **Baseline CDC officielle** : **172 / 188 (91,49 %)**.
- **Gain maximal certifiable** : **+2** $\implies$ **Projection : 174 / 188 (92,55 %)**.
- **Suite de tests actuelle** : 991 / 991 tests verts (100 %) | 0 warning | 0 pending model changes.

---

### 2. ARCHITECTURE TECHNIQUE HARDENED

#### 2.1 Volet SEC-02 : Moteur d'Autorisation par Champ (RBAC IES)
1. **Centralisation & Fail-Secure** :
   - Un unique service métier `AutorisationChampService` (`CIT.Plateforme.Application.Securite`) évalue les droits.
   - Par défaut, toute permission, rôle ou champ non explicitement autorisé est évalué en `EtatDroitChamp.Masked` (Lecture) ou `Rejet` (Écriture).
2. **Matrice RBAC Validée (4 Blocs & 5 Profils)** :
   - *Manifeste* : Édition par Pilote Manifeste & Superviseur ; Lecture seule par Pilote CD & Facturation ; Lecture seule par Admin.
   - *Douane & CD* : Édition par Pilote CD & Superviseur ; Lecture seule par Pilote Manifeste & Facturation ; Lecture seule par Admin.
   - *Financier* : Édition par Facturation/Caisse & Superviseur ; **Masqué** pour Pilote Manifeste & Pilote CD ; Lecture seule par Admin.
   - *Chauffeur* : Édition par Pilote Manifeste & Superviseur ; Lecture seule par Pilote CD ; Masqué pour Facturation.
3. **Application aux Surfaces (UI, API & Export)** :
   - *API REST (NFR-08)* : Les champs masqués sont retournés avec valeur `null` dans les DTOs ; toute tentative d'écriture interdite renvoie `403 Forbidden` (`AccesInterditException`).
   - *Exports CSV / Excel* : Les colonnes financières sont remplacées par `[RESTREINT]` pour les rôles non habilités (compatible protection injection CSV F012).
   - *Audit des Refus* : Toute tentative d'écriture illégitime est consignée dans le journal de sécurité (`SEC-05`) sans divulgation de valeur sensible.

#### 2.2 Volet SEC-06 : Moteur de Rétention & Cycle de Vie ARTCI
1. **Évaluation Temporelle Pure (`MoteurRetentionService`)** :
   - Calcul fondé exclusivement sur l'abstraction `IHorloge` (zéro appel direct à `DateTime.Now`).
   - *Pièces comptables & Factures* : 10 ans à compter de `DateEmissionUtc` (Archivage scellé SHA-256).
   - *Dossiers opérationnels (`Ticket`)* : 5 ans à compter de `DateClotureUtc` (Anonymisation irréversible des données à caractère personnel).
   - *Journaux d'audit (`JournalAudit`)* : 3 ans à compter de `DateCreationUtc` (Conservation inaltérable préservant l'invariant append-only historique).
   - *Brouillons (`BrouillonSaisie`)* : 30 jours à compter de `DateDerniereModificationUtc` (Purge automatique sans altérer les brouillons récents $< 30$ jours).
2. **Préservation de l'Invariant Append-Only (`JournalAudit`)** :
   - Aucun `DELETE` sur `JournalAudit`. L'immutabilité du journal historique est 100% préservée ; la rétention des journaux d'audit $> 3$ ans s'opère par masquage/archivage logique sans altération physique du journal d'audit.
3. **Gel Légal (*Legal Hold*)** :
   - Propriété `EstSousGelLegal` sur `Ticket`. Si active (`true`), le dossier, ses pièces et son historique sont exemptés de toute anonymisation ou purge.
4. **Anonymisation Irréversible des Dossiers Expirés** :
   - Remplacement des données personnelles (`NomChauffeur`, `TelephoneChauffeur`, `NumeroPermisChauffeur`, `EmailDemandeur`, `NomDemandeur`) par des mentions anonymisées pseudonymisées `ANONYMISE`.
   - Préservation des clés étrangères et de l'intégrité référentielle pour les statistiques et rapports agrégés.
5. **Suppression Physique Hermétique GED (`F015`)** :
   - Pour les brouillons purgés, suppression via `IStockageSecuriseService` garantissant l'absence de path traversal.
   - En cas d'échec GED : l'unité transactionnelle consigne une anomalie sans générer de faux certificat de succès.
6. **Idempotence & Certificat de Lot (`LotsPurgeRetention`)** :
   - Chaque passe de purge est strictement idempotente et enregistre un lot d'audit horodaté avec volume traité, sans aucune PII.
7. **Traitement d'Arrière-Plan & Concurrence** :
   - `RetentionBackgroundService` configurable (`Retention:FrequenceTraitementMinutes`).
   - Verrou logique empêchant toute exécution simultanée concurrente de la purge.

---

### 3. MODÈLE DE DONNÉES & MIGRATION EF CORE

- **Option A Retenue** : Extensions minimales sur entités existantes + table dédiée de traçabilité de lot.
- **Migration EF Core unique** : `AjoutRetentionEtGelLegal`.
- **Modifications d'entités** :
  - `Ticket` : `EstSousGelLegal` (bool, default `false`), `DateArchivageUtc` (DateTime?, default `null`), `EstAnonymise` (bool, default `false`).
  - `Document` : `EstScelle` (bool, default `false`), `DateScellementUtc` (DateTime?, default `null`), `EmpreinteScellementSha256` (string?, default `null`).
  - `LotPurgeRetention` : Nouvelle table `LotsPurgeRetention` (Id, DateExecutionUtc, Categorie, NombreElementsTraites, EstSucces, MessageErreur).
- **Opérations destructives** : **0 DROP TABLE, 0 DROP COLUMN, 0 RENAMING**.
- **Backfill sécurisé** : Valeurs par défaut sûres (`false`, `null`) garantissant 0 régression sur les données existantes.

---

### 4. TEST-SPECIFICATION EXHAUSTIVE

#### 4.1 TEST-SPEC SEC-CERT-02 (Contrôle d'Accès par Champ — 8 Scénarios)
1. *Scénario 1* : Pilote Manifeste consulte et modifie les champs manifeste $\implies$ Succès.
2. *Scénario 2* : Pilote Manifeste tente de modifier les champs douane $\implies$ Exception `403 Forbidden`.
3. *Scénario 3* : Pilote Manifeste consulte les données financières $\implies$ Champs masqués (`null` en API, astérisques en UI).
4. *Scénario 4* : Facturation consulte et modifie les données financières $\implies$ Succès.
5. *Scénario 5* : Export CSV par Pilote CD $\implies$ Colonnes financières libellées `[RESTREINT]`.
6. *Scénario 6* : Rôle inconnu ou non mappé $\implies$ Masquage total (Fail-Secure).
7. *Scénario 7* : Superviseur / Administrateur $\implies$ Respect strict des permissions de la matrice sans dérogation arbitraire.
8. *Scénario 8* : Tentative d'écriture non autorisée $\implies$ Consignation dans `JournalAudit` (`SEC-05`) sans fuite de PII.

#### 4.2 TEST-SPEC SEC-CERT-06 (Rétention ARTCI & Purge — 12 Scénarios)
1. *Scénario 1* : Brouillon de 29 jours $\implies$ Conservé intact.
2. *Scénario 2* : Brouillon de 30 jours et 1 heure $\implies$ Purgé automatiquement de la base et du stockage GED.
3. *Scénario 3* : Dossier clôturé de 4 ans et 364 jours $\implies$ Conservé intact avec données personnelles.
4. *Scénario 4* : Dossier clôturé de 5 ans et 1 jour $\implies$ Données personnelles anonymisées (`ANONYMISE`), intégrité référentielle préservée.
5. *Scénario 5* : Dossier clôturé de 6 ans avec `EstSousGelLegal = true` $\implies$ Exempté de toute anonymisation ou purge.
6. *Scénario 6* : Facture comptable de 9 ans $\implies$ Conservée scellée avec empreinte SHA-256 en lecture seule.
7. *Scénario 7* : Facture comptable de 10 ans $\implies$ Maintien scellé d'archive légale.
8. *Scénario 8* : Événements de `JournalAudit` $< 3$ ans $\implies$ Intacts.
9. *Scénario 9* : Invariant append-only $\implies$ Zéro `DELETE` sur `JournalAudit`, tests append-only 100% verts.
10. *Scénario 10* : Purge exécutée deux fois consécutives $\implies$ Idempotence absolue, 0 corruption.
11. *Scénario 11* : Échec de suppression physique GED $\implies$ Traitement unitaire sécurisé, enregistrement de l'anomalie.
12. *Scénario 12* : Certificat de purge dans `LotsPurgeRetention` $\implies$ Volume tracé, 0 donnée personnelle exposée.

---

### 5. PRÉSERVATION ABSOLUE DES FONCTIONNALITÉS HISTORIQUES

- **F015 (Protection & GED)** : PASS (Stockage hermétique, antivirus, intégrité SHA-256 réutilisée sans duplication).
- **F013 (Brouillons ERR-01)** : PASS (Purge immédiate après soumission préservée, purge 30j uniquement sur abandon).
- **F016 (Observabilité & Résilience)** : PASS (Sondes `/healthz` et `/ready` préservées, métrologie active).
- **Journal d'Audit Append-Only (`SEC-05`)** : PASS (Aucune suppression sur la table d'audit).
- **Authentification & 2FA** : PASS.
- **API REST (NFR-08)** : PASS.
- **Anti-Scope** : PASS (0 développement sur NFR-03/04/10, NFR-05, NFR-09, ENR-04, IA).
