# Contrats d'Utilisation & Cas d'Usage — Feature 004 : Billing Office EXPORT

> **Dossier** : `specs/004-billing-office-export/`  
> **Date** : 30 Août 2026  
> **Statut** : Validé pour Planification (`/speckit.plan`)  
> **Sources de vérité** : CIT-CIV-DSI-RFP-XXXX v1.0, `.specify/memory/constitution.md`, `specs/004-billing-office-export/spec.md`

---

## Matrice des Contrats Applicatifs (C5-01 à C5-08)

| Identifiant | Cas d'Usage / Commande / Requête | Exigences Couvertes | Acteur Principal | Rôle / Permission Exigée |
| :--- | :--- | :--- | :--- | :--- |
| **C5-01** | `SoumettreDemandeExportCommand` | `EXP-01`, `UNI-04` | Client | `DroitSoumission == true` |
| **C5-02** | `ConsulterFileBillingExportQuery` | `EXP-02` | Opérateur Back Office | `BillingExportBackOfficeInstruire` |
| **C5-03** | `ValiderTicketExportCommand` | `EXP-02` | Opérateur Back Office | `BillingExportBackOfficeInstruire` |
| **C5-04** | `GestionComplementExportCommand` | `EXP-02`, `ACR-01` | Client | Propriétaire de la demande |
| **C5-05** | `EmettreFactureExportCommand` | `EXP-03` | Pilote Facturation | `BillingFacturationInstruire` |
| **C5-06** | `ValiderPaiementExportCommand` | `EXP-03` | Caisse / Finance | `BillingPaiementValider` |
| **C5-07** | `FinaliserControleDocumentaireExportCommand` | `EXP-03` | Pilote Contrôle Doc | `BillingControleDocumentaireInstruire` |
| **C5-08** | `AcheverBrancheIesExportCommand` | `EXP-04` | Back Office / CD | Habilitation selon la branche |

---

## Détail des Contrats

### C5-01 : `SoumettreDemandeExportCommand`
- **Description** : Soumission par un client d'un dossier d'embarquement comprenant 1 à N bookings et leurs pièces justificatives.
- **Entrées** :
  ```csharp
  public sealed record PieceJointeSoumission(string NomFichier, string TypeContenu, byte[] Contenu);
  
  public sealed record SoumettreDemandeExportCommand(
      Guid DemandeurId,
      Guid SocieteId,
      IReadOnlyList<string> NumerosBooking,
      string? Marchandises,
      string? PortDestination,
      IReadOnlyList<PieceJointeSoumission> Documents
  );
  ```
- **Sorties** : `ResultatSoumissionExportVue(Guid DossierId, string NumeroDossier, IReadOnlyList<TicketCreeVue> Tickets)`.
- **Règles & Erreurs** :
  - Vérification de l'habilitation `DroitSoumission` sur la société.
  - Normalisation minimale des numéros de booking (`Trim()` + `ToUpperInvariant()`).
  - Contrôle d'unicité `UNI-04` : si un booking est déjà actif, renvoie `ConflitUniciteBookingException`.
  - Création du `Dossier` (module `BillingExport`, type `EXPORT`) avec snapshot du `TypeClient` de la société.
  - Création d'un `Ticket` indépendant par booking, placé en file `Back Office Export`.

---

### C5-02 : `ConsulterFileBillingExportQuery`
- **Description** : Consultation de la liste des tickets en attente ou en cours dans la file Back Office Export.
- **Entrées** : `Guid AgentId, Guid FileId`.
- **Sorties** : `FileBillingExportVue(Guid FileId, string NomFile, IReadOnlyList<TicketFileExportItemVue> Tickets)`.
- **Règles & Erreurs** : Vérifie que l'agent possède l'habilitation `BillingExportBackOfficeInstruire`.

---

### C5-03 : `ValiderTicketExportCommand`
- **Description** : Verdict de l'opérateur Back Office après codification du booking dans IPAKI.
- **Entrées** :
  ```csharp
  public enum DecisionBackOfficeExport { CodifieValide, Rejete, DemandeComplement }
  
  public sealed record ValiderTicketExportCommand(
      Guid AgentId,
      Guid TicketId,
      DecisionBackOfficeExport Decision,
      string? Motif
  );
  ```
- **Règles & Erreurs** :
  - Si `Decision == Rejete` ➔ `Motif` obligatoire, ticket marqué `TypeAnnulationExport.RejetBackOffice`, passe à `StatutTicket.Annule` et libère le booking.
  - Si `Decision == DemandeComplement` ➔ `Motif` obligatoire, ticket passe à `EnAttenteClient` (déclenche ACR-01).
  - Si `Decision == CodifieValide` ➔ Aiguillage selon le `Dossier.TypeClient` :
    - `Normal`, `EnCompte`, `Deposit` ➔ file `Facturation`.
    - `Ies` ➔ Branche Back Office achevée.

---

### C5-04 : `GestionComplementExportCommand`
- **Description** : Dépôt par le client des documents ou informations complémentaires demandés par le Back Office.
- **Entrées** : `Guid ClientId, Guid TicketId, string ReponseTexte, IReadOnlyList<PieceJointeSoumission> Documents`.
- **Règles & Erreurs** : Vérifie l'appartenance du ticket à la société du client, enregistre les pièces, passe le ticket à `EnAttenteDePriseEnCharge` et le repositionne dans la file Back Office.

---

### C5-05 : `EmettreFactureExportCommand`
- **Description** : Dépôt du Draft IN et édition de la Facture Export par le Pilote Facturation.
- **Entrées** :
  ```csharp
  public sealed record EmettreFactureExportCommand(
      Guid AgentId,
      Guid TicketId,
      PieceJointeSoumission FichierDraftIn,
      PieceJointeSoumission FichierFacture,
      decimal MontantTtc
  );
  ```
- **Règles & Erreurs** :
  - Dépôt des deux documents rattachés au ticket (`DraftIn` et `FactureExport`).
  - Si `TypeClient == Normal` ➔ ticket passe à `EnAttenteClient` (attente de preuve de paiement).
  - Si `TypeClient == EnCompte` ➔ ticket avance directement vers la file `Contrôle Documentaire`.
  - Si `TypeClient == Deposit` ➔ ticket avance vers la file `Comptabilité Client`.

---

### C5-06 : `ValiderPaiementExportCommand`
- **Description** : Validation de la quittance / preuve de paiement pour le profil Client Normal.
- **Entrées** : `Guid AgentId, Guid TicketId, bool EstValide, string? MotifRejet`.
- **Règles & Erreurs** : Vérifie la permission `BillingPaiementValider`. Si validé, le ticket avance en file `Contrôle Documentaire` avec statut `Payé confirmé`. Si rejeté, retourne en attente preuve client.

---

### C5-07 : `FinaliserControleDocumentaireExportCommand`
- **Description** : Dépôt du BEE officiel et clôture du ticket par le Pilote Contrôle Documentaire.
- **Entrées** : `Guid AgentId, Guid TicketId, PieceJointeSoumission FichierBee`.
- **Règles & Erreurs** :
  - Vérifie l'habilitation `BillingControleDocumentaireInstruire`.
  - Attache le document `Bee` (PDF officiel).
  - Marque le ticket à l'état `Cloture` avec motif de clôture normal.
  - Le numéro de booking est désormais libéré pour l'unicité future tout en restant consultable en archive.

---

### C5-08 : `AcheverBrancheIesExportCommand`
- **Description** : Achèvement d'une des branches parallèles du circuit Client IES Export (`BackOffice` ou `ControleDocumentaire`).
- **Entrées** : `Guid AgentId, Guid TicketId, BrancheIes Branche`.
- **Règles & Erreurs** : Achève la branche demandée sur l'entité `SuiviBrancheIes`. Dès que les deux branches sont complétées, le ticket est prêt pour la clôture sur la plateforme IES.
