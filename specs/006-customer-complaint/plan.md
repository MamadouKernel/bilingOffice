# Plan d'Implémentation Technique — Feature 006

> **Document** : `specs/006-customer-complaint/plan.md`  
> **Date** : 30 Août 2026

Ce document détaille les phases d'implémentation de la Feature 006, selon le principe de **Test-First**.

---

## Phase 1 — Baseline & Extensions Minimales Domaine

**Objectif : Mettre en place le modèle de données et les règles métier.**

1. Création de l'entité `CategorieReclamation` dans le module métier `Tickets` ou `Reclamations`.
2. Extension de l'entité `Ticket` (propriétés `CategorieReclamationId`, `ReponseFormalisee`, `EvaluationNote`, `EvaluationCommentaire`).
3. Création de la méthode `CloturerAvecReponseFormalisee(string reponse, MotifClotureType motif, DateTime date)` sur `Ticket` (ajout de l'invariant qui vérifie la non-nullité de la réponse).
4. Création de la méthode `Evaluer(int note, string? commentaire, DateTime date)` sur `Ticket`.
5. **Tests** : 
   - `TicketTests` : Tester les invariants de clôture avec réponse vide (doit échouer).
   - `TicketTests` : Tester l'évaluation (succès, refus si déjà évalué, refus si non clôturé).

## Phase 2 — Persistance / Migration

**Objectif : Intégration à EF Core et base de données réelle.**

1. Configuration de l'entité `CategorieReclamation` dans `ApplicationDbContext` (DbSet, IEntityTypeConfiguration).
2. Configuration de l'extension de `Ticket` dans `TicketConfiguration` (Mapping des nouvelles colonnes).
3. (Hors périmètre de `/plan` : Génération de la migration `AjoutSupportCustomerComplaint`).
4. **Tests** :
   - `TicketRepositoryTests` : Créer un ticket CustomerComplaint, le sauvegarder, le recharger, valider que les nouveaux champs sont bien persistés.

## Phase 3 — Soumission & Consultation Client (Application)

**Objectif : Couvrir la création et la lecture client (C7-01).**

1. Créer `SoumettreReclamationCommand` et son handler.
   - Valide l'existence de la catégorie.
   - Détermine la file cible depuis la catégorie.
   - Instancie un `Ticket` et l'ajoute au repository.
2. Adapter (ou créer) `ObtenirDetailReclamationQuery` pour inclure la catégorie et la réponse.
3. Modifier l'autorisation pour inclure `Complaint.Soumettre`.
4. **Tests** :
   - Tests d'intégration : Un client soumet une réclamation avec des données valides -> Succès.
   - Tests IDOR : Un client tente de consulter la réclamation d'un autre -> `AccesRefuseException`.

## Phase 4 — File Agent & Prise en Charge (Application)

**Objectif : Routage et prise en charge.**

1. Seed : Ajout de la file "Support Opérationnel" dans `DevelopmentSeeder`.
2. Seed : Création des catégories de base liées à cette file.
3. Pas de nouveaux contrats pour la prise en charge (utilisation de `PrendreEnChargeTicketCommand`).
4. Autorisations : Ajouter `Complaint.Instruire` au rôle Support.
5. **Tests** : 
   - Test workflow : Création ticket -> Agent Support prend en charge -> Ticket En Cours.

## Phase 5 — Compléments / Messages (Application)

**Objectif : Échanges client-agent.**

1. Réutilisation stricte de `AjouterMessageCommand`, `MettreEnAttenteClientCommand` et `ReprendreTraitementTicketCommand`.
2. Rien à développer côté métier, uniquement vérification des permissions (si `Ticket.Module == "CustomerComplaint"`, vérifier `Complaint.Instruire`).
3. **Tests** :
   - Test intégration pour garantir que le moteur passe bien le ticket en "Attente Client" sous le module Complaint.

## Phase 6 — Résolution / Clôture (Application)

**Objectif : Couvrir C7-02 et C7-03.**

1. Créer `CloturerReclamationCommand` et son handler.
   - Récupère le ticket, appelle la méthode métier `CloturerAvecReponseFormalisee`.
2. Créer `EvaluerReclamationCommand` et son handler.
3. **Tests** :
   - Test intégration clôture par l'agent.
   - Test intégration IDOR sur l'évaluation (seul le propriétaire peut évaluer).

## Phase 7 — ACR-01 / Dashboard

**Objectif : Notification des actions requises.**

1. S'assurer que le service qui lit l'ACR-01 (souvent `ObtenirDashboardClientQuery`) inclut les tickets `Module == "CustomerComplaint"` en statut `EnAttenteClient`.
2. **Tests** :
   - Test fonctionnel : L'ACR apparaît quand le statut change, disparaît quand le client répond.

## Phase 8 — UI Blazor / Navigation

**Objectif : Intégration front-end dans F003.5.**

1. Créer `NouvelleReclamation.razor` (Formulaire avec combo box catégories).
2. Ajouter le bouton "Nouvelle Réclamation" dans le Sidebar / Header.
3. Créer ou adapter `FicheReclamation.razor` (ou ajouter des `if` dans `FicheTicket.razor`).
   - Affichage de la catégorie.
   - Affichage du composant de réponse formalisée si clôturé.
   - Affichage du module d'évaluation (étoiles) pour le client.
4. UI Agent : Formulaire de clôture modal étendu avec un champ `TextArea` pour la réponse officielle.

## Phase 9 — REC-SCE-07 E2E

**Objectif : Prouver la conformité contractuelle.**

1. Créer `RecSce07CustomerComplaintE2ETests.cs`.
2. Implémenter les 6 étapes définies dans `quickstart.md` au travers de l'API (WebApplicationFactory).

## Phase 10 — Non-Régression & Audit

**Objectif : Sécurité finale.**

1. Lancement global de la suite de tests (432 tests existants + nouveaux tests).
2. Vérification que l'Audit `SOC-13` a bien intercepté la nouvelle commande `CloturerReclamationCommand`.
3. Livraison finale prête pour la PR.
