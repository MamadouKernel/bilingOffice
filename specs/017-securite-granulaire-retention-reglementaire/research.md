# RECHERCHE TECHNIQUE & ARCHITECTURALE — FEATURE 017

## Sécurité Granulaire Métier & Rétention Documentaire Réglementaire (ARTCI)

---

### 1. ÉTAT DE L'ART & ANALYSE DE L'EXISTANT

- **Contrôle d'accès (`SEC-02`)** :
  - `IAutorisationService` existe dans `CIT.Plateforme.Application.Abstractions` pour les permissions globales.
  - *Besoin F017* : Introduire un service d'autorisation par bloc/champ (`IAutorisationChampService`) appliquant la matrice RBAC IES sur 4 blocs de données (`Manifeste`, `Douane/CD`, `Données Financières`, `Chauffeur`) avec fail-secure absolu.
- **Rétention & Cycle de vie (`SEC-06`)** :
  - `BrouillonSaisie` existe dans `CIT.Plateforme.Domain.Tickets` avec `DateDerniereModification`.
  - `Ticket` existe dans `CIT.Plateforme.Domain.Tickets` avec `DateCreation` et `DateCloture`.
  - `IHorloge` existe dans `CIT.Plateforme.Application.Abstractions` (zéro appel direct à `DateTime.Now`).
  - `IStockageSecuriseService` / `StockageLocalSecuriseService` existe (F015) pour la suppression physique hermétique des pièces jointes.
  - *Besoin F017* : Moteur de rétention pur (`MoteurRetentionService`), drapeaux de rétention/scellement/gel légal sur `Ticket` et `Document`, et entité d'enregistrement d'audit de lot (`LotPurgeRetention`).

---

### 2. DÉCISIONS ARCHITECTURALES

1. **D1 - Moteur RBAC Centralisé & Fail-Secure** :
   - Définition d'un enum `EtatDroitChamp` (`AllowWrite`, `ReadOnly`, `Masked`).
   - Évaluation centralisée dans `AutorisationChampService` injecté dans les requêtes/commandes de consultation et d'export.
2. **D2 - Moteur de Rétention Pur & Idempotent** :
   - `MoteurRetentionService` évalue l'éligibilité sans effet de bord à partir d'`IHorloge`.
   - Purge exécutée par lots avec gestion des transactions unitaires et journalisation d'audit anonymisée.
3. **D3 - Modèle de Gel Légal (*Legal Hold*)** :
   - Ajout d'une propriété booléenne `EstSousGelLegal` sur `Ticket`. Si `true`, le ticket et ses pièces jointes sont exemptés de toute purge automatique.
4. **D4 - Protection des Pièces Comptables Décennales** :
   - Les pièces comptables scellées conservent leur empreinte SHA-256 (héritée de F015) et basculent en lecture seule d'archive.
