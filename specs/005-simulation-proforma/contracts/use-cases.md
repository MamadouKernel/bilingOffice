# Contrats de Cas d'Utilisation (C6-01 à C6-08) — Feature 005 : Simulation de Proforma

> **Dossier** : `specs/005-simulation-proforma/`  
> **Date** : 30 Août 2026  
> **Statut** : **VALIDÉ**  
> **Auteur** : Antigravity (Pair Programming AI)  
> **Sources de vérité** : `spec.md`, `research.md`, `data-model.md`, `.specify/memory/constitution.md`

---

## 1. Matrice des Contrats Applicatifs (Série C6)

| Contrat | Intitulé & Rôle | Type | Acteur | Exigences CDC | Habilitation & Cloisonnement |
| :--- | :--- | :---: | :--- | :---: | :--- |
| **`C6-01`** | `CalculerSimulationProformaQuery` | Query | Client / Agent | `SIM-01..05`, `SIM-11` | Utilisateur authentifié |
| **`C6-02`** | `SauvegarderSimulationProformaCommand` | Command | Client | `SIM-08` | Client rattaché à une société active |
| **`C6-03`** | `ConsulterSimulationProformaQuery` | Query | Client / Agent | `SIM-05`, `SIM-08` | Cloisonnement société (`SocieteId`) |
| **`C6-04`** | `ListerSimulationsProformaQuery` | Query | Client / Agent | `SIM-08` | Cloisonnement société (`SocieteId`) |
| **`C6-05`** | `GenererDocumentProformaPdfQuery` | Query | Client / Agent | `SIM-09` | Cloisonnement société (`SocieteId`) |
| **`C6-06`** | `CreerVersionGrilleTarifaireCommand` | Command | Admin CIT | `SIM-06`, `SIM-07` | `Permission.TarificationAdministrer` |
| **`C6-07`** | `ImporterGrilleTarifaireCsvCommand` | Command | Admin CIT | `SIM-06`, `SIM-07`, `SIM-10` | `Permission.TarificationAdministrer` |
| **`C6-08`** | `ConsulterVersionsGrillesTarifairesQuery` | Query | Admin CIT | `SIM-06`, `SIM-07` | `Permission.TarificationAdministrer` |

---

## 2. Spécification Détaillée des Contrats

### C6-01 : `CalculerSimulationProformaQuery`
- **Objectif** : Exécuter le moteur de calcul pur sans obligation de persistance en base.
- **Entrées** :
  ```csharp
  public sealed record CalculerSimulationRequete(
      SensTrafic Sens,
      DateOnly DateArrivee,
      DateOnly DateEnlevementPrevisionnel,
      IReadOnlyList<ConteneurSimulationRequete> Conteneurs,
      string? NomNavire = null,
      string? NumeroVoyage = null,
      string? NumeroBlOuBooking = null);

  public sealed record ConteneurSimulationRequete(
      string NumeroConteneur,
      TailleConteneur Taille,
      TypeConteneur Type,
      decimal PoidsTonnes,
      bool EstDangereux,
      bool EstHorsGabarit,
      string CategorieMarchandise);
  ```
- **Sortie** : `ResultatSimulationVue` contenant le détail conteneur par conteneur (7 postes), la répartition stationnement, le total général, la mention légale et la version de grille appliquée.
- **Erreurs & Règles** :
  - Si `DateEnlevement < DateArrivee` ➔ `ValidationException`.
  - Si aucun conteneur ➔ `ValidationException`.
  - Si combinaison non tarifée dans la grille active ➔ `TarifNonTrouveException` (`CLAR-SIM-03`).

---

### C6-02 : `SauvegarderSimulationProformaCommand`
- **Objectif** : Enregistrer le résultat d'une simulation calculée pour le compte de la société cliente.
- **Entrées** : `Guid AuteurId, Guid SocieteId, CalculerSimulationRequete Requete`.
- **Sortie** : `Guid SimulationId, string ReferenceSimulation` (ex. `SIM-20260830-0042`).
- **Règles** :
  - Vérifie que `AuteurId` est membre actif de `SocieteId`.
  - Fige le snapshot complet des entrées et du résultat calculé.
  - Journalise l'enregistrement dans `JournalAudit`.

---

### C6-03 : `ConsulterSimulationProformaQuery`
- **Objectif** : Consulter les détails d'une simulation enregistrée avec détection d'obsolescence tarifaire.
- **Entrées** : `Guid AppelantId, Guid SimulationId`.
- **Sortie** : `SimulationProformaDetailVue` incluant l'indicateur `bool EstGrilleObsolete` et `string VersionActiveLibelle`.
- **Règles** :
  - Vérifie que la simulation appartient à la société de l'appelant (`SocieteId`) ➔ Sinon `AccesRefuseException` (403).

---

### C6-04 : `ListerSimulationsProformaQuery`
- **Objectif** : Récupérer l'historique paginé des simulations de la société du client.
- **Entrées** : `Guid AppelantId, Guid SocieteId, int Page = 1, int TaillePage = 20`.
- **Sortie** : `IReadOnlyList<SimulationProformaSommaireVue>` (Référence, Date, Sens, Nombre conteneurs, Total XOF, Version grille).
- **Règles** :
  - Filtrage serveur obligatoire sur `SocieteId`.

---

### C6-05 : `GenererDocumentProformaPdfQuery`
- **Objectif** : Produire le flux binaire PDF de la proforma.
- **Entrées** : `Guid AppelantId, Guid SimulationId`.
- **Sortie** : `byte[] ContenuFichier, string NomFichier, string TypeContenu ("application/pdf")`.
- **Règles** :
  - Contrôle multi-tenant de propriété.
  - Intègre le tableau récapitulatif, la décomposition des frais, le total XOF et la mention légale (`SIM-05`).

---

### C6-06 : `CreerVersionGrilleTarifaireCommand`
- **Objectif** : Créer manuellement une nouvelle version de grille avec une date d'effet future.
- **Entrées** : `Guid AgentId, string NumeroVersion, DateTime DateEffetUtc, ParametresTarification Parametres, IReadOnlyList<LigneTarifaireRequete> Lignes`.
- **Sortie** : `Guid VersionId`.
- **Règles** :
  - Exige `Permission.TarificationAdministrer`.
  - Rejette les doublons de triplets $(\text{Sens}, \text{Taille}, \text{Catégorie})$.
  - Journalisation de la création.

---

### C6-07 : `ImporterGrilleTarifaireCsvCommand`
- **Objectif** : Importer atomiquement une nouvelle version de grille à partir d'un fichier CSV.
- **Entrées** : `Guid AgentId, string NumeroVersion, DateTime DateEffetUtc, ParametresTarification Parametres, Stream FluxCsv`.
- **Sortie** : `ResultatImportCsvVue` (`bool Succes, int LignesImportees, IReadOnlyList<string> Erreurs`).
- **Règles** :
  - Exige `Permission.TarificationAdministrer`.
  - Validation atomique (tout ou rien) selon `CLAR-SIM-01`.
  - En cas d'erreur de parsing ou montant négatif, rollback total et retour des lignes en anomalie.

---

### C6-08 : `ConsulterVersionsGrillesTarifairesQuery`
- **Objectif** : Lister l'historique immuable des versions de grilles et visualiser la grille active.
- **Entrées** : `Guid AgentId`.
- **Sortie** : `IReadOnlyList<VersionGrilleTarifaireSommaireVue>`.
- **Règles** :
  - Exige `Permission.TarificationAdministrer`.
