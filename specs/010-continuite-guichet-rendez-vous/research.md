# Recherche Technique & Décisions d'Architecture — Feature 010
## Continuité Guichet & Rendez-vous

> **Document** : `specs/010-continuite-guichet-rendez-vous/research.md`  
> **Statut** : Validé pour implémentation  
> **Date** : 1er septembre 2026  
> **Branche** : `master`  
> **Auteur** : Antigravity (IA Pair Programmer)

---

## 1. Analyse Préalable du Code Réel (Preflight Code)

### 1.1 Invariants du Socle Existant (F001 à F009)
1. **`Ticket` & `Dossier` (F001/F003)** :
   - Le `Ticket` possède un identifiant technique (`Id` Guid) et une référence métier unique générée transactionnellement (`Numero` string, ex. `CIT-IMP-20260901-0001`).
   - Le `Dossier` regroupe jusqu'à 5 tickets (`Numero` string, ex. `DOS-20260901-0001`).
   - L'entité `Ticket` gère son propre jeton de concurrence optimiste `VersionConcurrence` (RowVersion).
2. **`EntreeHistorique` & `JournalAudit` (F001)** :
   - Toute transition ou prise en charge génère une `EntreeHistorique` liée au `TicketId`.
   - Les opérations sensibles sont journalisées de manière immuable via `EntreeJournalAudit` (`Operation`, `AuteurId`, `DetailsJson`).
3. **`EvenementSocle` & Notifications (F009)** :
   - L'abonnement aux événements se fait via `EvenementSocle` (table `JournalEvenements`) avec curseur `DistribueLeUtc`.
   - F010 n'a besoin que d'appeler `EvenementSocle.Creer(...)` et de l'ajouter au DbContext.
4. **Habilitations & Cloisonnement (F002)** :
   - La table `HabilitationUtilisateur` associe un `UtilisateurId` à un `PermissionCode` et éventuellement un `FileId`.
   - `ContexteUtilisateur` extrait les permissions de l'utilisateur authentifié côté serveur.

---

## 2. Décisions Techniques d'Architecture (D1 à D12)

### D1 — Recherche Guichet : Normalisation et Indexation
- **Contexte** : CON-02 exige une recherche rapide au guichet sur N° ticket, N° dossier, N° BL, N° booking et nom du client.
- **Décision** :
  - La recherche est exécutée côté serveur via LINQ / EF Core avec projection légère `ResultatRechercheGuichetDto`.
  - Normalisation : chaîne nettoyée (`Trim()`), insensible à la casse.
  - Les requêtes tirent parti des index existants (`IX_Tickets_Numero`, `IX_Dossiers_Numero`, `IX_Tickets_NumeroBlOuBooking`).
  - Plafond de résultats fixé par configuration (`ParametresRendezVous.LimiteResultatsRecherche`, défaut 50) pour garantir un temps de réponse sous 200 ms.
  - **Aucun moteur de recherche plein texte externe** (Lucene, Elasticsearch) n'est introduit.

### D2 — Entité Domaine `RendezVous` : Source de Vérité Unique
- **Contexte** : CON-03 exige la prise de rendez-vous rattachée au ticket/dossier.
- **Décision** :
  - Création de l'entité `RendezVous` avec `TicketId` comme clé étrangère pivot obligatoire et `DossierId` optionnel.
  - Le `DemandeurId` est dérivé côté serveur depuis l'identité authentifiée (`ClaimsPrincipal`).
  - Strictement 4 états métier : `Reserve`, `Honore`, `Annule`, `NonPresente`.
  - Invariant d'unicité : un ticket ne peut posséder qu'un seul rendez-vous à l'état `Reserve` à la fois (garanti par index unique filtré en base).

### D3 — Gestion de la Concurrence et Capacité des Créneaux
- **Contexte** : Deux utilisateurs réservant simultanément le dernier créneau disponible doivent aboutir à exactement un succès et un conflit métier propre.
- **Décision** :
  - La réservation s'exécute dans une transaction isolée (`IExecuteurTransactionnel` ou transaction DbContext).
  - La vérification du nombre de réservations actives (`Statut == StatutRendezVous.Reserve`) pour le couple `(Date, HeureDebut)` est évaluée atomiquement.
  - En cas de dépassement de la capacité configurée (`ParametresRendezVous.CapaciteParCreneau`), une exception `CreneauCompletException` est levée.
  - Le test de concurrence sur **SQL Server réel** est exigé pour valider le comportement multi-threads.

### D4 — Paramétrage des Créneaux sans Valeurs en Dur
- **Contexte** : Les plages horaires, durées de créneau et capacités ne doivent pas être figées dans le code domaine.
- **Décision** :
  - Création de l'objet de configuration `ParametresRendezVous` injecté via `IConfiguration` (section `Guichet:RendezVous`).
  - Valeurs de développement / défaut :
    - `CapaciteParCreneau` : 5 usagers
    - `DureeCreneau` : 30 minutes
    - `HeureOuverture` : 08:00
    - `HeureFermeture` : 16:30
    - `JoursOuvrables` : Lundi à Vendredi

### D5 — Annulation et Report Atomique
- **Contexte** : Annulation et déplacement de créneau.
- **Décision** :
  - L'annulation (`AnnulerRendezVousCommand`) passe le statut à `Annule` et libère instantanément la place.
  - Le report (`ReporterRendezVousCommand`) s'exécute en une seule opération atomique : vérification de la disponibilité du nouveau créneau, mise à jour des champs `Date`, `HeureDebut`, `HeureFin` du rendez-vous existant, horodatage `DateModificationUtc` et trace dans l'historique. Si le nouveau créneau est complet, rollback immédiat.

### D6 — Constatation de Présence (`Honore`) et Non-Présentation (`NonPresente`)
- **Contexte** : Traitement au guichet par l'agent.
- **Décision** :
  - L'agent de guichet habilité valide la présence via `MarquerRendezVousHonoreCommand`. Le statut passe à `Honore` et l'événement `RendezVousHonore` est émis.
  - Si l'usager ne s'est pas présenté, l'agent ou le superviseur clôture le RDV via `MarquerRendezVousNonPresenteCommand`.
  - **Aucun BackgroundService automatique de clôture** n'est introduit arbitrairement.

### D7 — Reprise sans Ressaisie (CON-01) : Zéro Duplication
- **Contexte** : La démarche commencée en ligne doit pouvoir se poursuivre au guichet sans ressaisie.
- **Décision** :
  - L'agent ouvre la fiche du ticket existant (`ConsulterDossierGuichetQuery` ou écran ticket existant).
  - Le ticket conserve son `Id`, son `Numero`, sa liasse `Document` et son `FilMessages`.
  - Aucun clone de ticket n'est créé.
  - L'intervention physique ajoute une `EntreeHistorique` (« Prise en charge guichet par [Agent] »).

### D8 — Alternative Présentielle Billing Import (IMP-15)
- **Contexte** : À l'issue du contrôle BL, le client peut choisir de finaliser physiquement.
- **Décision** :
  - La commande `BasculerAlternativePresentielleCommand` associe la démarche au flux guichet et émet `BasculePresentielleEffectuee`.
  - L'UI propose un lien direct vers la prise de RDV (`/rendez-vous/prendre/{ticketId}`).

### D9 — Mode Dégradé Documenté (ERR-04) : Zéro Offline PWA
- **Contexte** : En cas de panne portail, le travail continue au guichet.
- **Décision** :
  - Procédure organisationnelle externe.
  - Après rétablissement du portail, l'agent utilise `RegulariserModeDegradeCommand` pour inscrire l'opération effectuée en mode dégradé avec mention spécifique dans l'audit.

### D10 — Découplage Événementiel avec la Feature 009
- **Contexte** : F010 doit notifier les usagers sans réimplémenter les canaux.
- **Décision** :
  - F010 publie uniquement des `EvenementSocle` (`RendezVousReserve`, `RendezVousAnnule`, `RendezVousHonore`, `BasculePresentielleEffectuee`).
  - F009 prend en charge la diffusion multicanale selon ses règles et modèles semés.

### D11 — Permissions RBAC Dédiées
- **Contexte** : Sécurité et habilitations.
- **Décision** :
  - Ajout dans `NatureUtilisateur.cs` de :
    - `Permission.GuichetRecherche = "guichet.recherche"` (Recherche et reprise au guichet)
    - `Permission.RendezVousGerer = "rendezvous.gerer"` (Gestion administrative et constatation de présence)
  - Les clients authentifiés ont le droit naturel de gérer leurs propres rendez-vous (vérification de propriété serveur `DemandeurId == UtilisateurId`).

### D12 — Clôture Complète du Scénario Contractuel REC-SCE-14
- **Contexte** : Dernier scénario de recette global non encore entièrement couvert.
- **Décision** :
  - Implémentation du test d'intégration et de bout en bout `RecSce14E2ETests` couvrant les 14 étapes du scénario de la section 22 du CDC.
