# Contrats de Cas d'Usage — Feature 007
## Module Avoir & Exonération

> **Document** : `specs/007-avoir-exoneration/contracts/use-cases.md`  
> **Date** : 1er septembre 2026  
> **Source de référence** : `spec.md`, `data-model.md`, `research.md`, `.specify/memory/constitution.md`

---

## 1. Vue d'Ensemble des Contrats Applicatifs (C7-01 à C7-07)

| N° | Contrat | Nature | Acteur & Permission | Exigences CDC |
| :--- | :--- | :---: | :--- | :---: |
| **`C7-01`** | `SoumettreDemandeAvoirExonerationCommand` | Command | Client Authentifié | `AVE-01`, `AVE-04`, `SOC-01`, `SOC-07`, `SOC-10` |
| **`C7-02`** | `ListerMesDemandesAvoirExonerationQuery` | Query | Client Authentifié | `AVE-01`, `SEC-01` |
| **`C7-03`** | `ConsulterFileAvoirExonerationQuery` | Query | `Permission.AvoirExonerationInstruire` | `AVE-02`, `SOC-02` |
| **`C7-04`** | `ObtenirFicheAvoirExonerationQuery` | Query | Propriétaire ou `Permission.AvoirExonerationInstruire` | `AVE-01`, `AVE-03`, `SEC-01` |
| **`C7-05`** | `PrendreEnChargeAvoirExoCommand` | Command | `Permission.AvoirExonerationInstruire` | `AVE-02`, `PEC-01` |
| **`C7-06`** | `DemanderComplementAvoirExoCommand` | Command | `Permission.AvoirExonerationInstruire` | `AVE-02`, `SOC-06`, `ACR-01` |
| **`C7-07`** | `DeciderDemandeAvoirExoCommand` | Command | `Permission.AvoirExonerationDecider` | `AVE-03`, `SOC-13`, `SEC-04` |

---

## 2. Spécification Détaillée des Contrats

### C7-01 — `SoumettreDemandeAvoirExonerationCommand`
- **Objectif** : Permettre à un client de soumettre une demande d'avoir ou d'exonération avec pièces justificatives et liaison optionnelle à un ticket parent existant (`AVE-01`, `AVE-04`).
- **Signature / Données d'Entrée** :
  ```csharp
  public sealed record PieceJointeSoumission(
      string NomFichier,
      string TypeContenu,
      byte[] Contenu
  );

  public sealed record SoumettreDemandeAvoirExoRequest(
      Guid DemandeurId, // Dérivé du contexte serveur (Anti-IDOR)
      Guid SocieteId,   // Dérivé du contexte serveur
      string TypeDemande, // "Avoir" ou "Exoneration"
      string Objet,
      string Description,
      decimal? MontantConteste,
      string? ReferenceMetier,
      Guid? TicketParentId, // Liaison optionnelle AVE-04
      IReadOnlyList<PieceJointeSoumission> PiecesJointes
  );

  public sealed record SoumettreDemandeAvoirExoResponse(
      Guid TicketId,
      string NumeroTicket,
      Guid DossierId,
      string NumeroDossier
  );
  ```
- **Règles Métier & Validations** :
  1. Le demandeur doit être un utilisateur authentifié de nature `Client`.
  2. `TypeDemande` doit valoir strictement `"Avoir"` ou `"Exoneration"`.
  3. `Objet` (max 200 car.) et `Description` (max 2000 car.) sont obligatoires et non vides.
  4. Si `MontantConteste` est renseigné, il doit être >= 0.
  5. Au moins une pièce jointe justificative est requise. Chaque fichier doit respecter les limites de sécurité (format PDF/JPEG/PNG, taille <= 10 Mo).
  6. Si `TicketParentId` est fourni :
     - Le ticket parent doit exister dans la base.
     - Le ticket parent doit appartenir à la même société (`Dossier.ClientId == request.SocieteId`). Rejet avec `AccesRefuseException` sinon.
     - Un enregistrement `LiaisonTicket` est créé pour lier le nouveau ticket au ticket parent (`AVE-04`).
  7. Création du `Dossier` et du `Ticket` avec numéro unique séquentiel formaté `SOC-01` (`AVO-YYYY-XXXXX` ou `EXO-YYYY-XXXXX`).
  8. Dépôt des pièces jointes dans `Documents`.
  9. Consignation dans `HistoriqueTickets` et `JournalAudit`.
  10. Publication de l'événement `DemandeAvoirSoumise` ou `DemandeExonerationSoumise` sur `EvenementSocle`.

---

### C7-02 — `ListerMesDemandesAvoirExonerationQuery`
- **Objectif** : Permettre au client de consulter toutes les demandes Avoir/Exonération rattachées à sa société (`AVE-01`, `SEC-01`).
- **Signature / Données d'Entrée** :
  ```csharp
  public sealed record DemandeAvoirExoResumeDto(
      Guid TicketId,
      string NumeroTicket,
      string? NumeroDossier,
      string TypeDemande,
      string Objet,
      string Statut,
      decimal? MontantConteste,
      decimal? MontantAccorde,
      string? Decision,
      DateTime DateCreationUtc,
      DateTime DateDerniereActiviteUtc
  );
  ```
- **Règles Métier & Validations** :
  1. Dérivation de `SocieteId` côté serveur.
  2. Filtre strict : `Ticket.Module == "AvoirExoneration"` ET `Dossier.ClientId == SocieteId`.
  3. Tri par date de création décroissante.

---

### C7-03 — `ConsulterFileAvoirExonerationQuery`
- **Objectif** : Permettre à un agent instructeur de consulter la file de traitement des demandes Avoir & Exonération (`AVE-02`, `SOC-02`).
- **Signature / Données d'Entrée** :
  ```csharp
  public sealed record CriteresFiltreFileAvoirExo(
      string? TypeDemande, // "Avoir", "Exoneration", ou null (tous)
      StatutTicket? Statut,
      string? RechercheTerme,
      int Page = 1,
      int TaillePage = 50
  );

  public sealed record TicketFileAvoirExoDto(
      Guid TicketId,
      string NumeroTicket,
      string? NumeroDossier,
      string TypeDemande,
      string ClientNom,
      string Objet,
      string Statut,
      decimal? MontantConteste,
      DateTime DateCreationUtc,
      DateTime DateArriveeEnFileUtc,
      Guid? OperateurAssigneId,
      string? OperateurAssigneNom
  );
  ```
- **Règles Métier & Validations** :
  1. Exige la permission `Permission.AvoirExonerationInstruire` via `IAutorisationService`.
  2. Filtre les tickets du module `"AvoirExoneration"` selon les critères fournis.
  3. Pagination bornée (max 100 éléments par page).

---

### C7-04 — `ObtenirFicheAvoirExonerationQuery`
- **Objectif** : Restituer la fiche complète d'une demande Avoir/Exonération pour affichage client ou agent (`AVE-01`, `AVE-03`).
- **Signature / Données d'Entrée** :
  ```csharp
  public sealed record FicheAvoirExonerationDto(
      Guid TicketId,
      string NumeroTicket,
      Guid DossierId,
      string? NumeroDossier,
      string TypeDemande,
      string Objet,
      string Description,
      decimal? MontantConteste,
      decimal? MontantAccorde,
      string Statut,
      string? Decision,
      string? MotifRejet,
      DateTime? DateDecisionUtc,
      Guid? DecideurId,
      string? DecideurNom,
      DateTime DateCreationUtc,
      string ClientNom,
      Guid ClientId,
      TicketParentDto? TicketParent,
      IReadOnlyList<DocumentAvoirExoDto> Documents,
      IReadOnlyList<MessageAvoirExoDto> Messages,
      IReadOnlyList<EntreeChronologieDto> Chronologie
  );
  ```
- **Règles Métier & Validations** :
  1. Contrôle d'accès strict (Anti-IDOR) : l'utilisateur doit être soit membre de la société propriétaire (`Dossier.ClientId == user.SocieteId`), soit agent porteur de `Permission.AvoirExonerationInstruire`.
  2. Si l'utilisateur est un client, les notes internes éventuelles (`EstInterne == true`) sont filtrées et exclues.

---

### C7-05 — `PrendreEnChargeAvoirExoCommand`
- **Objectif** : Permettre à un agent instructeur de prendre en charge un ticket Avoir/Exo en attente (`AVE-02`, `PEC-01`).
- **Signature / Données d'Entrée** :
  ```csharp
  public sealed record PrendreEnChargeAvoirExoRequest(
      Guid AgentId,
      Guid TicketId
  );
  ```
- **Règles Métier & Validations** :
  1. Exige `Permission.AvoirExonerationInstruire`.
  2. Le ticket doit être au statut `Cree` ou `EnAttenteDePriseEnCharge`.
  3. Mise à jour de `OperateurAssigneId` et passage au statut `EnCours`.
  4. Consignation dans `HistoriqueTickets` et `JournalAudit`.

---

### C7-06 — `DemanderComplementAvoirExoCommand`
- **Objectif** : Demander un complément documentaire ou des explications au client (`AVE-02`, `SOC-06`, `ACR-01`).
- **Signature / Données d'Entrée** :
  ```csharp
  public sealed record DemanderComplementAvoirExoRequest(
      Guid AgentId,
      Guid TicketId,
      string MotifComplement
  );
  ```
- **Règles Métier & Validations** :
  1. Exige `Permission.AvoirExonerationInstruire`.
  2. `MotifComplement` obligatoire et non vide.
  3. Délégation à `AttenteClientCommand.MettreEnAttenteAsync` (F001).
  4. Émission de l'événement `AvoirComplementDemande` pour alerte client F009.

---

### C7-07 — `DeciderDemandeAvoirExoCommand`
- **Objectif** : Enregistrer la décision finale (Accord ou Rejet motivé) issue des vérifications financières et clôturer le ticket (`AVE-03`).
- **Signature / Données d'Entrée** :
  ```csharp
  public sealed record DeciderDemandeAvoirExoRequest(
      Guid AgentId,
      Guid TicketId,
      DecisionAvoirExo Decision, // Accorde ou Rejete
      string? MotifRejet,       // Obligatoire si Rejet
      decimal? MontantAccorde,   // Optionnel si Accord
      PieceJointeSoumission? DocumentDecision // Document officiel joint
  );
  ```
- **Règles Métier & Validations** :
  1. Exige impérativement `Permission.AvoirExonerationDecider`.
  2. Le ticket doit appartenir au module `"AvoirExoneration"` et ne doit être ni `Cloture` ni `Annule`.
  3. Si `Decision == DecisionAvoirExo.Rejete` :
     - `MotifRejet` est **obligatoire** (levée de `ArgumentException` si vide ou null).
  4. Si un `DocumentDecision` est fourni, il est persisté dans `Documents` avec typage `"DecisionAvoirExo"`.
  5. Appel de la méthode de domaine `ticket.EnregistrerDecisionAvoirExo(...)`.
  6. Consignation dans `JournalAudit` (`AvoirExoneration.Decision`) et `HistoriqueTickets`.
  7. Publication de l'événement `EvenementSocle` approprié (`AvoirAccorde`, `AvoirRejete`, `ExonerationAccordee`, `ExonerationRejetee`).
  8. Clôture formelle du ticket.
