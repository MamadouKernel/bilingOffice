# CONTRATS ET CAS D'USAGE — FEATURE 013

**Feature** : Feature 013 — Continuité Opérationnelle, Brouillons de Saisie, Exposition d'API & Clôture Démarches  
**Date** : 2026-09-01  

---

## 1. CONTRATS D'USAGE ERR-01 (BROUILLONS)

### Contrat `C13-01` : `EnregistrerBrouillonCommand`
- **Entrées** : `UtilisateurId` (Guid), `SocieteId` (Guid), `TypeFormulaire` (string), `DonneesJson` (string).
- **Autorisation** : Authentification requise + rattachement à la `SocieteId`.
- **Comportement** :
  - Recherche si un brouillon existe déjà pour `(UtilisateurId, TypeFormulaire)`.
  - Si existant : mise à jour du payload et de `DateDerniereModificationUtc`.
  - Si inexistant : création d'un nouveau `BrouillonSaisie`.
  - Sauvegarde transactionnelle.
- **Sortie** : `Id` du brouillon enregistré.
- **Erreurs** : `AccesRefuseException` si tentative d'enregistrement pour une société tierce.

---

### Contrat `C13-02` : `ObtenirBrouillonQuery`
- **Entrées** : `UtilisateurId` (Guid), `TypeFormulaire` (string).
- **Autorisation** : Authentification requise.
- **Comportement** : Retourne le brouillon actif de l'utilisateur pour le type de formulaire demandé, ou `null` si aucun brouillon.
- **Sortie** : `BrouillonDto?` (`Id`, `TypeFormulaire`, `DonneesJson`, `DateDerniereModificationUtc`).

---

### Contrat `C13-03` : `SupprimerBrouillonCommand`
- **Entrées** : `UtilisateurId` (Guid), `TypeFormulaire` (string).
- **Autorisation** : Authentification requise (propriétaire).
- **Comportement** : Supprime le brouillon associé. Utilisé lors de la soumission définitive ou lors de l'abandon manuel par l'utilisateur.

---

## 2. CONTRATS D'USAGE NFR-08 (API OUVERTE)

### Contrat `C13-04` : `GET /api/v1/tickets/{numero}`
- **Entrées** : `numero` (string, paramètre de route), Header Authorization ou Cookie.
- **Autorisation** : Utilisateur authentifié ayant accès à la société propriétaire du ticket ou Agent habilité.
- **Sortie** : `TicketStatutDto` (`Numero`, `Module`, `TypeDemande`, `Statut`, `DateCreationUtc`, `DerniereMiseAJourUtc`).
- **Codes HTTP** : `200 OK`, `401 Unauthorized`, `403 Forbidden` (CPT-04), `404 Not Found`.

---

### Contrat `C13-05` : `GET /api/v1/dossiers/{reference}`
- **Entrées** : `reference` (string, paramètre de route).
- **Autorisation** : Habilitation / appartenance à la société propriétaire du dossier.
- **Sortie** : `DossierDetailDto` (`Reference`, `ClientId`, `SocieteId`, `DateCreationUtc`, `Tickets[]`).

---

### Contrat `C13-06` : `POST /api/v1/simulations/calculer`
- **Entrées** : `CalculerSimulationApiRequete` (`Sens`, `DateArrivee`, `DateEnlevement`, `Conteneurs[]`).
- **Comportement** : Exécute le calcul tarifaire déterministe F005 et retourne le détail complet.
- **Sortie** : `SimulationResultatDto` (`MontantTotalHt`, `PostesDetails[]`, `RepartitionStationnement`).

---

## 3. CONTRATS D'USAGE SIM-11 (CONTRÔLES DE COHÉRENCE SIMULATION)

### Contrat `C13-07` : `ValiderCoherenceSimulationQuery`
- **Entrées** : Paramètres de simulation en cours de saisie.
- **Comportement** : Évalue les 4 règles de cohérence CDC (taille/type, sens/catégorie, chronologie des dates, exclusivité Reefer pour branchement).
- **Sortie** : `ValidationCoherenceResultat` (`EstValide`, `Alertes[]`, `MessagesAide[]`).

---

## 4. CONTRATS D'USAGE ENR-05 (RENOUVELLEMENT BADGE & RAPPEL)

### Contrat `C13-08` : `RappelerExpirationBadgesCommand`
- **Entrées** : `DateReferenceUtc` (optionnel, par défaut `MaintenantUtc`).
- **Comportement** :
  - Identifie les dossiers d'enrôlement avec badge remis dont `DateExpirationUtc` arrive à échéance sous le délai de préavis configuré.
  - Publie l'événement `RappelExpirationBadge` dans `JournalEvenements` pour distribution via F009.
- **Sortie** : Nombre de rappels émis.

---

### Contrat `C13-09` : `DemanderRenouvellementBadgeCommand`
- **Entrées** : `AuteurId` (Guid), `DossierEnrolementId` (Guid).
- **Comportement** :
  - Vérifie l'existence du dossier d'enrôlement initial.
  - Crée un nouveau ticket de renouvellement dans le même `DossierId` sans duplication inutile.
  - Consigne l'audit `DemandeRenouvellementInitiee`.
- **Sortie** : `Ticket` créé.
