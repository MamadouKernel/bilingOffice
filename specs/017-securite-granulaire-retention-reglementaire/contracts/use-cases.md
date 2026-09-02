# CONTRATS D'UTILISATION HARDENED — FEATURE 017

## Sécurité Granulaire Métier & Rétention Documentaire Réglementaire (ARTCI)

---

### 1. CONTRAT C17-01 : ÉVALUATION DE PERMISSION PAR CHAMP (RBAC IES)

- **Composant** : `AutorisationChampService`
- **Méthode** : `EtatDroitChamp EvaluerDroit(BlocDonneesIes bloc, string roleUtilisateur)`
- **Entrées** : `BlocDonneesIes` (`Manifeste`, `Douane`, `Financier`, `Chauffeur`), `roleUtilisateur` (`PiloteManifeste`, `PiloteCd`, `Facturation`, `Superviseur`, `Administrateur`).
- **Sorties** : `EtatDroitChamp` (`AllowWrite`, `ReadOnly`, `Masked`).
- **Invariants** :
  - Bloc ou rôle non mappé $\implies$ `EtatDroitChamp.Masked` (Fail-Secure).
  - Tentative de mutation non autorisée $\implies$ Exception `AccesInterditException` (HTTP 403) + journalisation d'audit `SEC-05`.

---

### 2. CONTRAT C17-02 : PROJECTION ET MASQUAGE CONTEXTUEL

- **Composant** : `ProjectionDonneesSecuriseesService`
- **Méthode** : `TDto Projeter<TSource, TDto>(TSource source, string roleUtilisateur)`
- **Règles** :
  - En API REST : Les champs masqués sont retournés avec valeur `null`.
  - En Export CSV : Les colonnes financières sont libellées `[RESTREINT]`.

---

### 3. CONTRAT C17-03 : ÉVALUATION D'ÉLIGIBILITÉ À LA RÉTENTION

- **Composant** : `MoteurRetentionService`
- **Méthode** : `ResultatEligibiliteRetention EvaluerEligibilite(CategorieRetention categorie, DateTime dateReferenceUtc, bool estSousGelLegal, DateTime dateCouranteUtc)`
- **Règles** :
  - `Brouillons` : Éligible si `dateCouranteUtc >= dateReferenceUtc.AddDays(30)`.
  - `Dossiers` : Éligible à l'anonymisation si `!estSousGelLegal && dateCouranteUtc >= dateReferenceUtc.AddYears(5)`.
  - `Comptable` : Éligible au scellement si `dateCouranteUtc < dateReferenceUtc.AddYears(10)`.
  - Si `estSousGelLegal == true` $\implies$ Non éligible (Gel actif).

---

### 4. CONTRAT C17-04 : EXÉCUTION DE PURGE ET ANONYMISATION

- **Composant** : `GestionnaireRetentionService`
- **Méthodes** :
  - `Task<int> PurgerBrouillonsExpiresAsync(CancellationToken ct)`
  - `Task<int> AnonymiserDossiersExpiresAsync(CancellationToken ct)`
- **Règles** :
  - Exécution unitaire transactionnelle par lot.
  - Idempotence garantie : ré-exécuter la commande n'altère aucun état stable.
  - Enregistrement du résultat dans `LotsPurgeRetention`.

---

### 5. CONTRAT C17-05 : GESTION DU GEL LÉGAL (*LEGAL HOLD*)

- **Composant** : `GelLegalService`
- **Méthodes** :
  - `Task ActiverGelLegalAsync(Guid ticketId, string motif, string utilisateurId, CancellationToken ct)`
  - `Task DesactiverGelLegalAsync(Guid ticketId, string motif, string utilisateurId, CancellationToken ct)`
- **Règles** :
  - Modification du drapeau `Ticket.EstSousGelLegal`.
  - Journalisation obligatoire dans `JournalAudit` (`SEC-05`).
