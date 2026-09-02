# Recherche Technique & Décisions d'Architecture — Feature 004 : Billing Office EXPORT

> **Dossier** : `specs/004-billing-office-export/`  
> **Date** : 30 Août 2026  
> **Version** : 1.3 (Précision Technique CLAR-BOOK-01)  
> **Statut** : Validé pour Planification (`/speckit.plan`)  
> **Sources de vérité** : CIT-CIV-DSI-RFP-XXXX v1.0, `.specify/memory/constitution.md`, `specs/004-billing-office-export/spec.md`

---

## 1. Décisions Techniques Numérotées (D1 à D12)

### D1 — Modélisation du Numéro de Booking dans l'Agrégat `Ticket`
- **Contexte** : `EXP-01` impose qu'un ticket Export traite un numéro de booking.
- **Décision** : Ajouter la propriété additive `public string? NumeroBooking { get; private set; }` sur la classe `Ticket` dans `CIT.Plateforme.Domain.Tickets`.
- **Garanties** : Typage fort, rétrocompatibilité totale avec Feature 001/002/003.

### D2 — Stratégie d'Unicité Globale du Booking Actif (`UNI-04` / `PC-09`)
- **Contexte** : `UNI-04` et la décision humaine A1 imposent qu'un booking actif ne puisse pas faire l'objet de plusieurs tickets simultanés, tout en autorisant la réutilisation après clôture, rejet ou annulation.
- **Décision** : Implémenter un **index unique filtré SQL Server** sur la colonne `NumeroBooking` avec collation fixe `Latin1_General_CI_AS` :
  ```sql
  CREATE UNIQUE INDEX [IX_Tickets_NumeroBooking_Actif] 
  ON [Tickets] ([NumeroBooking]) 
  WHERE [NumeroBooking] IS NOT NULL AND [Statut] IN (0, 1, 2)
  ```
  Où `StatutTicket` : `EnAttenteDePriseEnCharge=0`, `EnCours=1`, `EnAttenteClient=2`.
  Les statuts terminaux `Cloture=3` et `Annule=4` (comprenant les rejets) sont exclus par liste d'inclusion positive.
- **Garanties** : Sécurité absolue sous forte concurrence garantie par le moteur SGBD.

### D3 — Normalisation de Saisie & Comparaison d'Unicité (`CLAR-BOOK-01`)
- **Contexte** : Distinction stricte entre le nettoyage de saisie et la comparaison technique d'unicité `UNI-04`.
- **Décision** :
  1. **Normalisation de Saisie (Stockage & Affichage)** :
     - Les espaces externes de saisie sont nettoyés avant persistance : `NumeroBooking = NumeroBooking.Trim()`.
     - La casse saisie par l'utilisateur après `Trim` est conservée telle quelle en base de données et constitue la valeur de référence affichée (pas de `ToUpperInvariant()` forcé).
  2. **Comparaison d'Unicité** :
     - La comparaison d'unicité est rendue insensible à la casse au niveau SGBD par la collation déterministe `Latin1_General_CI_AS` (`abc123` == `ABC123` == `AbC123`).
  3. **Caractères Conservés** :
     - Les tirets et les espaces internes sont strictement préservés (`ABC-123` != `ABC123`, `ABC 123` != `ABC123`).

### D4 — Transactionnalité & Indépendance Multi-Bookings (`EXP-01` / Décision A2)
- **Contexte** : Le client peut soumettre 1 à N bookings dans une même demande. La décision A2 exige que chaque ticket booking soit totalement indépendant.
- **Décision** : La commande `SoumettreDemandeExportCommand` (`C5-01`) crée un `Dossier` de type `EXPORT` et itère sur chaque booking pour créer son `Ticket` dédié. En cas de conflit d'unicité sur un booking soumis, la transaction de soumission rejette le booking en conflit avec message clair, tandis que les tickets créés avec succès poursuivent leur cycle de vie indépendant.

### D5 — File Back Office & Codification IPAKI (`EXP-02`)
- **Contexte** : L'opérateur Back Office effectue la codification dans le système externe IPAKI (hors portail) et prononce son verdict dans la plateforme.
- **Décision** : Créer la file de traitement `Back Office Export` associée au module `BillingExport`. Aucun appel API ou connecteur externe automatisé n'est inventé. L'opérateur enregistre son statut (Validé, Rejeté avec motif obligatoire, ou Demande de complément avec motif) via la commande `ValiderTicketExportCommand` (`C5-03`).

### D5bis — Représentation Métier du Rejet Export
- **Contexte** : Préserver la distinction métier entre une annulation client ordinaire et un rejet Back Office Export sans modifier `StatutTicket.Annule`.
- **Décision** : Ajouter la propriété additive `public TypeAnnulationExport? TypeAnnulationExport { get; private set; }` avec la valeur `RejetBackOffice = 0` sur `Ticket`, symétrique à `TypeAnnulationImport`.

### D6 — Circuit Facturation, Draft IN & Aiguillage selon `Dossier.TypeClient` (`EXP-03`)
- **Contexte** : En Export, après validation Back Office, le ticket s'oriente vers la Facturation où l'agent dépose le `Draft IN` et la `FactureExport` avant orientation selon `Dossier.TypeClient`.
- **Décision** : Commande `EmettreFactureExportCommand` (`C5-05`) rattachant les pièces `DraftIn` et `FactureExport`. L'aiguillage s'effectue ensuite selon le snapshot immuable `Dossier.TypeClient`.

### D7 — Habilitation pour la Validation de Paiement (`PC-03` / Décision A3)
- **Contexte** : La validation des preuves de paiement pour les clients `Normal` doit être sécurisée par RBAC.
- **Décision** : Réutiliser la permission existante `BillingPaiementValider` partagée entre les flux Import et Export (`C5-06`).

### D8 — Contrôle Documentaire & Délivrance du BEE (`EXP-03` / Décision A4)
- **Contexte** : Le document officiel final remis au client pour autoriser l'accès au terminal est le BEE (*Bordereau d'Embarquement Électronique*).
- **Décision** : En file `Contrôle Documentaire`, l'agent dépose le document de sortie `Bee` (fichier PDF) via `FinaliserControleDocumentaireExportCommand` (`C5-07`). Le ticket passe à l'état `Cloture` et le BEE devient immédiatement téléchargeable par le client.

### D9 — Traitement Spécifique Client IES Export (`EXP-04`)
- **Contexte** : Le client IES Export fait l'objet d'un traitement parallèle entre le Back Office et le Contrôle Documentaire.
- **Décision** : Ajouter la valeur `BrancheIes.BackOffice = 2` sur l'énumération `BrancheIes`. Le ticket IES Export crée deux branches (`BackOffice` et `ControleDocumentaire`). La jointure est effective dès que les deux branches sont achevées via `AcheverBrancheIesExportCommand` (`C5-08`).

### D10 — Intégration Dynamique dans ACR-01 ("Mes actions requises")
- **Contexte** : Les tickets Export en attente d'une action client doivent remonter dans le bandeau du tableau de bord.
- **Décision** : `ObtenirDashboardClientQuery` prend en compte les tickets du module `BillingExport` lorsque `Statut == StatutTicket.EnAttenteClient` (Complément Back Office, Paiement attendu, Preuve rejetée).

### D11 — Interface Utilisateur Blazor & Zéro GUID Manuel
- **Contexte** : L'accès aux formulaires et files Export doit respecter les acquis de Feature 003.5.
- **Décision** :
  - Page `SoumettreDemandeExport.razor` (`/billing-export/nouvelle-demande`) avec résolution automatique de la société connectée.
  - Page `FileBackOfficeExport.razor` (`/billing-export/back-office`).
  - Navigation enrichie dans `NavMenu.razor`.

### D12 — Stratégie Test-First & Non-Régression
- **Contexte** : Baseline de départ : **310 tests verts**.
- **Décision** : Écriture préalable systématique des tests de non-régression et E2E (`ParcoursExportE2ETests.cs` pour `REC-SCE-05`).
