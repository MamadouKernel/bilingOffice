# Contrats de Cas d'Usage — Feature 010
## Continuité Guichet & Rendez-vous

> **Document** : `specs/010-continuite-guichet-rendez-vous/contracts/use-cases.md`  
> **Statut** : Validé pour implémentation  
> **Date** : 1er septembre 2026

---

## 1. Répertoire des Cas d'Usage (Série C10)

| Réf. | Type | Cas d'Usage | Exigence Source | Acteur(s) | Autorisation / Règle |
|:---|:---:|:---|:---:|:---|:---|
| **`C10-01`** | Query | `RechercherGuichetQuery` | `CON-02` | Agent de Guichet | `Permission.GuichetRecherche` |
| **`C10-02`** | Query | `ConsulterDossierGuichetQuery` | `CON-01` | Agent de Guichet | `Permission.GuichetRecherche` |
| **`C10-03`** | Query | `ListerCreneauxDisponiblesQuery` | `CON-03` | Client · Agent | Authentifié |
| **`C10-04`** | Command | `ReserverRendezVousCommand` | `CON-03` | Client · Agent | Authentifié + Propriété ticket |
| **`C10-05`** | Command | `AnnulerRendezVousCommand` | `CON-03` | Client · Agent | Propriétaire ou `Permission.RendezVousGerer` |
| **`C10-06`** | Command | `ReporterRendezVousCommand` | `CON-03` | Client · Agent | Propriétaire ou `Permission.RendezVousGerer` |
| **`C10-07`** | Command | `MarquerRendezVousHonoreCommand` | `CON-03`, `CON-04` | Agent de Guichet | `Permission.RendezVousGerer` |
| **`C10-08`** | Command | `MarquerRendezVousNonPresenteCommand` | `CON-03` | Agent de Guichet | `Permission.RendezVousGerer` |
| **`C10-09`** | Query | `ListerMesRendezVousQuery` | `CON-03` | Client | Authentifié (DemandeurId = UtilisateurId) |
| **`C10-10`** | Command | `BasculerAlternativePresentielleCommand` | `IMP-15` | Client | Authentifié + Propriété ticket |
| **`C10-11`** | Command | `RegulariserModeDegradeCommand` | `ERR-04` | Agent de Guichet | `Permission.GuichetRecherche` |

---

## 2. Spécification Détaillée des Contrats

### C10-01 : `RechercherGuichetQuery`
- **Entrée** : `CriteresRechercheGuichet(string? Terme, string? Module, int Limite = 50)`
- **Sortie** : `IReadOnlyList<ResultatRechercheGuichetDto>`
  - `TicketId` (Guid)
  - `NumeroTicket` (string)
  - `NumeroDossier` (string?)
  - `Module` (string)
  - `ClientNom` (string)
  - `ReferenceBlOuBooking` (string?)
  - `EtapeActuelle` (string)
  - `StatutTicket` (string)
  - `DateCreation` (DateTime)
  - `PossedeRendezVousActif` (bool)
- **Règles** :
  - Vérification de la permission `GuichetRecherche`.
  - Normalisation : `Terme.Trim()`, recherche multicritère (N° ticket, dossier, BL, booking, client).
  - Ne retourne que les tickets pour lesquels l'agent est habilité selon ses files.

### C10-02 : `ConsulterDossierGuichetQuery`
- **Entrée** : `Guid TicketId`
- **Sortie** : `DossierGuichetCompletDto` (Informations formulaires, métadonnées, documents téléchargeables, historique, fil messages, rendez-vous rattachés).
- **Règles** :
  - Accès direct au même objet sans duplication (`CON-01`).
  - Trace automatique d'une consultation guichet.

### C10-03 : `ListerCreneauxDisponiblesQuery`
- **Entrée** : `DateOnly Date`
- **Sortie** : `IReadOnlyList<CreneauDisponibleDto>`
  - `Date` (DateOnly)
  - `HeureDebut` (TimeSpan)
  - `HeureFin` (TimeSpan)
  - `PlacesRestantes` (int)
  - `EstComplet` (bool)
- **Règles** :
  - Génère les tranches de la journée (ex. 08:00 à 16:30 par pas de 30 min).
  - Déduit le nombre de réservations actives (`Statut == Reserve`) pour chaque créneau.

### C10-04 : `ReserverRendezVousCommand`
- **Entrée** : `ReserverRendezVousRequest(Guid TicketId, DateOnly Date, TimeSpan HeureDebut, string Motif)`
- **Sortie** : `ResultatReservationRdvDto(Guid RendezVousId, string Message)`
- **Règles** :
  - Dérive `DemandeurId` côté serveur.
  - Vérifie que le ticket appartient au client connecté.
  - Vérifie que le ticket n'est pas clôturé/rejeté.
  - Vérifie qu'aucun autre RDV `Reserve` n'existe pour ce ticket.
  - Vérifie la disponibilité du créneau dans une transaction isolée.
  - En cas de succès : persiste le `RendezVous` et publie l'événement `RendezVousReserve` sur `EvenementSocle`.

### C10-05 : `AnnulerRendezVousCommand`
- **Entrée** : `AnnulerRendezVousRequest(Guid RendezVousId, string MotifAnnulation)`
- **Sortie** : `bool`
- **Règles** :
  - Autorisé si l'utilisateur est le demandeur du RDV ou un agent avec `RendezVousGerer`.
  - Passe le statut à `Annule`, horodate, et publie `RendezVousAnnule`.

### C10-06 : `ReporterRendezVousCommand`
- **Entrée** : `ReporterRendezVousRequest(Guid RendezVousId, DateOnly NouvelleDate, TimeSpan NouvelleHeureDebut)`
- **Sortie** : `bool`
- **Règles** :
  - Opération atomique : vérifie la capacité sur le nouveau créneau.
  - Met à jour la date/heure du même RDV, horodate `DateModificationUtc` et ajoute une trace dans l'historique du ticket.

### C10-07 : `MarquerRendezVousHonoreCommand`
- **Entrée** : `MarquerHonoreRequest(Guid RendezVousId, string? Commentaire)`
- **Sortie** : `bool`
- **Règles** :
  - Réservé aux agents habilités (`Permission.RendezVousGerer`).
  - Passe le statut à `Honore`, trace dans l'audit et publie `RendezVousHonore`.

### C10-08 : `MarquerRendezVousNonPresenteCommand`
- **Entrée** : `MarquerNonPresenteRequest(Guid RendezVousId, string? Commentaire)`
- **Sortie** : `bool`
- **Règles** :
  - Passe le statut à `NonPresente`.

### C10-09 : `ListerMesRendezVousQuery`
- **Entrée** : `Guid DemandeurId`
- **Sortie** : `IReadOnlyList<RendezVousClientDto>`
- **Règles** :
  - Filtré par `DemandeurId` authentifié serveur.

### C10-10 : `BasculerAlternativePresentielleCommand`
- **Entrée** : `Guid TicketId`
- **Sortie** : `bool`
- **Règles** :
  - Valide l'étape de contrôle BL terminée.
  - Ajoute la trace dans l'historique et émet `BasculePresentielleEffectuee`.

### C10-11 : `RegulariserModeDegradeCommand`
- **Entrée** : `RegulariserModeDegradeRequest(Guid TicketId, DateTime DateOperationReelleUtc, string DescriptionOperation)`
- **Sortie** : `bool`
- **Règles** :
  - Réservé aux agents habilités.
  - Consigne l'opération rétroactive dans l'audit avec étiquette `ModeDegrade`.
