# Plan d'Exécution (Tasks) — Feature 006

> **Document** : `specs/006-customer-complaint/tasks.md`  
> **Date** : 30 Août 2026

## 1. Contrats Applicatifs C7

| Contrat | Exigence | Test | Implémentation | UI | Sécurité |
|:---|:---:|:---:|:---:|:---:|:---:|
| **C7-01 SoumettreReclamationCommand** | REC-01, REC-05 | T009 | T010 | T027 | REC-SEC-02, REC-SEC-03 |
| **C7-02 CloturerReclamationCommand** | REC-04 | T019 | T020 | T029 | REC-SEC-04 |
| **C7-03 EvaluerReclamationCommand** | REC-06 | T021 | T022 | T029 | REC-SEC-03, REC-SEC-06 |
| **C7-04 ListerCategoriesReclamationQuery**| REC-01, REC-02 | T011 | T012 | T027 | (Lecture authentifiée) |
| **C7-05 ModifierCategoriesReclamationCommand**| REC-02 | T030 | T030 | T030 | Autorisation (Admin) |

*(Les actions d'instruction, compléments et transferts réutilisent nativement les contrats existants F001 sans ajout).*

---

## 2. Découpage des Tâches (Phasage)

### PHASE 1 — Preflight & tests de domaine
- [X] T001 [P] Vérifier la baseline (432 tests verts, 0 échec) — `dotnet test`.
- [X] T002 Créer les tests unitaires de domaine pour `CategorieReclamation` (invariants de création) — `CategorieReclamationTests.cs` — REC-02.
- [X] T003 Créer les tests unitaires de domaine pour l'extension de `Ticket` (clôture avec/sans réponse formalisée, soumission d'évaluation, rejets des transitions interdites, garantie de non-régression sur les tickets non-CustomerComplaint) — `TicketTests.cs` — REC-04, REC-06.

### PHASE 2 — Extensions Domaine Customer Complaint
- [X] T004 Implémenter l'entité `CategorieReclamation` — `CategorieReclamation.cs`.
- [X] T005 Implémenter les extensions additives sur `Ticket` (`CategorieReclamationId`, `ReponseFormalisee`, module `EvaluationSatisfaction` et méthodes `CloturerAvecReponseFormalisee()`, `Evaluer()`) — `Ticket.cs`.

### PHASE 3 — Persistence EF Core & CategorieReclamation
- [X] T006 [P] Créer les tests d'intégration Repository pour garantir la sauvegarde et relecture des nouveaux champs et de la catégorie — `TicketRepositoryTests.cs`.
- [X] T007 Configurer EF Core : créer `CategorieReclamationConfiguration`, mettre à jour `TicketConfiguration` (extensions), et configurer les index — `ApplicationDbContext.cs` & Configurations.
- [X] T008 Mettre à jour `DevelopmentSeeder` pour injecter la file "Support Opérationnel", les permissions F002 nécessaires, et le jeu de catégories de base (REC-02).

### PHASE 4 — Soumission et consultation client
- [X] T009 Créer les tests pour `SoumettreReclamationCommand` (vérifications C7-01 : catégorie active, IDOR `SocieteId`, erreur sur données manquantes) — `SoumettreReclamationCommandTests.cs` — REC-01.
- [X] T010 Implémenter `SoumettreReclamationCommand` et son handler — C7-01.
- [X] T011 Créer les tests pour `ListerCategoriesReclamationQuery` — `ListerCategoriesReclamationQueryTests.cs`.
- [X] T012 Implémenter `ListerCategoriesReclamationQuery` — C7-04.
- [X] T013 Étendre le handler de `ObtenirDetailTicketQuery` (F001 existant) pour inclure et retourner `CategorieReclamationId` et `ReponseFormalisee` si le ticket est de type Customer Complaint.

### PHASE 5 — File agent / prise en charge / instruction
- [X] T014 [P] Créer les tests validant que seul un agent avec l'autorisation appropriée peut accéder à la file de support — `FileSupportTests.cs` — REC-03.
- [X] T015 Injecter/créer l'autorisation `Complaint.Instruire` dans le modèle d'identités (Claims).
- [X] T016 Créer un test d'intégration garantissant que `PrendreEnChargeTicketCommand` (existant) fonctionne parfaitement pour un Ticket Complaint et échoue pour un agent non affecté à la file — REC-03.

### PHASE 6 — Compléments, messages et réponse formalisée
- [X] T017 Écrire des tests fonctionnels pour valider que `MettreEnAttenteClientCommand` et `ReprendreTraitementTicketCommand` agissent correctement sur un ticket Complaint — REC-03.
- [X] T018 Implémenter le test de sécurité REC-SEC-05 : Créer une `NoteInterne` via `AjouterMessageCommand` (agent) et certifier que l'API de consultation client ne la retourne jamais.

### PHASE 7 — Résolution, clôture et évaluation
- [X] T019 Créer les tests pour `CloturerReclamationCommand` (vérification de la présence de la réponse formalisée, respect des états préalables, déclenchement audit/event COM-01) — `CloturerReclamationCommandTests.cs` — REC-04.
- [X] T020 Implémenter `CloturerReclamationCommand` et son handler — C7-02.
- [X] T021 Créer les tests pour `EvaluerReclamationCommand` (ticket clôturé uniquement, propriétaire uniquement, une seule évaluation possible) — `EvaluerReclamationCommandTests.cs` — REC-06.
- [X] T022 Implémenter `EvaluerReclamationCommand` et son handler — C7-03.

### PHASE 8 — ACR-01 / Dashboard / Fiche 360°
- [X] T023 Créer les tests fonctionnels REC-ACR-01/02/03 (apparition du statut d'attente client dans le widget ACR-01 du Dashboard, disparition à la réponse, cloisonnement multi-tenant).
- [X] T024 Adapter le modèle de vue et le traitement de `ObtenirDashboardClientQuery` pour lister explicitement les Customer Complaints en attente.
- [X] T025 Étendre le ViewModel `FicheTicketViewModel` (utilisé par la fiche 360°) pour exposer le texte de la réponse formalisée et l'évaluation existante le cas échéant.

### PHASE 9 — UI Blazor / Navigation / Administration catégories
- [ ] T026 [P] Créer les tests bUnit pour les composants de soumission et d'affichage (0 DbContext, 0 logique métier, mock du MediatR).
- [X] T027 Implémenter `NouvelleReclamation.razor` (formulaire de soumission client).
- [X] T028 Modifier `NavMenu.razor` pour inclure les liens Client (Mes réclamations, Nouvelle) et Agent (File Support).
- [X] T029 Adapter `FicheTicket.razor` avec des blocs conditionnels (`if (Ticket.Module == "CustomerComplaint")`) pour afficher la catégorie en haut, la réponse formalisée en bas (si clôturé), et le module d'étoiles interactives (si non évalué).
- [ ] T030 Implémenter l'administration UI (ModifCategories, C7-05) pour les administrateurs avec désactivation logique (soft-delete).

### PHASE 10 — REC-SCE-07 / Sécurité / Multi-Tenant / Non-régression
- [X] T031 Implémenter le scénario `RecSce07CustomerComplaintE2ETests.cs` couvrant intégralement la matrice métier (Étapes 1 à 6).
- [X] T032 Vérifier ou créer les tests de sécurité finaux (REC-SEC-01 à REC-SEC-06) via WebApplicationFactory (403 Forbid, Challenge 401).
- [X] T033 Implémenter les tests de flux métier marginaux (REC-FLOW) : validation des données obligatoires manquantes, états invalides.
- [X] T034 Générer la migration EF Core `AjoutSupportCustomerComplaint` (`dotnet ef migrations add ...`).
- [X] T035 Tâche finale : validation globale par compilation et exécution de l'intégralité de la suite de tests (`dotnet test`).

---

## 3. Matrice REC-SCE-07 (E2E)

| Étape REC-SCE-07 | Exigence | Tâche | Assertion E2E |
|:---|:---:|:---|:---|
| **1. Soumission / Catégorie** | REC-01, 02 | T031 | Soumission HTTP via API/Page -> Code 200, TicketId retourné en BDD avec sa Catégorie. |
| **2. Orientation Agent** | REC-05 | T031 | Requête sur la File Support -> Le ticket y figure (visibilité garantie). |
| **3. Demande de Complément**| REC-03 | T031 | Agent invoque `MettreEnAttente` -> Statut passe à `EnAttenteClient`. Le client le voit dans ACR-01. |
| **4. Réponse au Complément**| REC-03 | T031 | Client ajoute un document/message -> Reprise du traitement, disparition de l'ACR-01. |
| **5. Transfert (Orientation)**| REC-03 | T031 | Agent simule un transfert vers une autre file -> Succès. (Étape optionnelle du scénario, testée). |
| **6. Clôture + Réponse** | REC-04 | T031 | Agent invoque C7-02 avec réponse texte -> Statut Clôturé, Client voit la réponse formelle en consultant le ticket. |

---

## 4. Traçabilité des Exigences CDC

| Priorité | Exigence | Contrats C7 | Tâches Implémentation | Écran (UI) | REC-SCE-07 |
|:---|:---|:---|:---|:---|:---|
| **Must** | **REC-01** (Création) | C7-01 | T009, T010 | NouvelleReclamation.razor | Étape 1 |
| **Must** | **REC-02** (Catégories) | C7-04, C7-05 | T011, T012, T030 | AdminCategories (optionnel UI) | Étape 1 |
| **Must** | **REC-03** (Support / Instruction) | F001 existant | T014, T016, T017 | FicheTicket.razor | Étapes 2, 3, 4, 5 |
| **Must** | **REC-04** (Clôture / Réponse) | C7-02 | T019, T020 | Modale de clôture | Étape 6 |
| **Should** | **REC-05** (Orientation auto) | C7-01 (logique interne) | T009, T010 | (Backend) | Étape 2 |
| **Could** | **REC-06** (Enquête) | C7-03 | T021, T022 | Composant Étoiles | Post-Clôture |

---

## 5. Traçabilité des Clarifications (CLAR-REC)

| Clarification | Résumé | Tâche(s) concernée(s) | Artefact / Validation |
|:---|:---|:---|:---|
| **CLAR-REC-01** | Réponse formalisée (Texte riche, pas de PDF) | T005, T019, T029 | `Ticket.ReponseFormalisee` (Test + UI). |
| **CLAR-REC-02** | Enquête (Pas de sous-module, composant UI) | T005, T021, T029 | `Ticket.EvaluationSatisfaction` (Owned Entity). |
| **CLAR-REC-03** | Résultat métier (Statuts F001 : Clôturé/Rejeté) | T003, T019 | Assertions sur `StatutTicket.Cloture`. |
| **CLAR-REC-04** | Catégorisation (Uniquement la catégorie est obligatoire) | T009, T010 | Assertions de validation C7-01. |
| **CLAR-REC-05** | Rattachement (Optionnel) | T009, T010 | Assertions d'insertion réussie sans FK métier. |
| **CLAR-REC-06** | SLA (Configuration générique F001) | T008 | Seed de la file sans inventer de constante en dur. |
| **CLAR-REC-07** | Demande complément (Réutilisation de F001/ACR-01) | T017, T023 | Assertions sur `EnAttenteClient` (REC-ACR). |
| **CLAR-REC-08** | Pièces jointes (Mécanisme SOC-07 existant) | T010, T017 | Validation de la visibilité des PJ via F001. |

---

## 6. Contrôle Sécurité et Avoir/Exonération (AVE)

- **Vérification d'artefact AVE** : `[VÉRIFICATION MÉCANIQUE INTERNE : EFFECTUÉE]` — Aucune table `Avoir`, aucune logique financière n'a été insérée dans les tâches. F006 s'arrête strictement à la méthode métier `Ticket.CloturerAvecReponseFormalisee()`.
- **REC-SEC (Sécurité)** : Sécurités authentification (REC-SEC-01), IDOR (REC-SEC-02, REC-SEC-03), habilitations (REC-SEC-04), non-fuite des notes internes (REC-SEC-05), et bypass UI (REC-SEC-06) sont expressément couvertes dans les tâches **T018**, **T032**, et intégrées nativement aux tests des tâches **T009**, **T014**, **T019**, **T021**.

---

## 7. Réconciliation Mécanique Post-Remédiation (post-implementation-audit.md)

Chaque case `[X]` ci-dessus a été revalidée individuellement contre le code réel, les tests réels
exécutés, et (pour T034) la migration réelle appliquée — jamais un remplacement global. Preuve
détaillée par tâche : `implementation-report.md` §0 (colonne "Preuve").

- **T026** reste `[ ]` mais est désormais **COUVERTE AUTREMENT** avec un niveau de preuve renforcé :
  la remédiation finale UI (closure-review.md, section "MISE À JOUR — REMÉDIATION FINALE UI") a
  ajouté les **premiers bUnit véritablement interactifs** de ce dépôt (clic réel sur les éléments
  rendus, pas seulement une assertion sur `cut.Markup` statique) —
  `ReponseComplementDepuisFicheTicketTests.cs`, `MessagerieFicheTicketNonRegressionTests.cs` (2 cas),
  et l'étape 4 de `RecSce07CustomerComplaintE2ETests.cs` réécrite pour cliquer réellement sur
  `FicheTicket` plutôt que d'appeler la Command directement. Ne pas cocher : la tâche demandait des
  bUnit *par composant* (Nouvelle/Mes/File Réclamations n'en ont toujours pas individuellement),
  mais l'objectif de fond (prouver l'interaction UI réelle, pas seulement le rendu) est atteint pour
  le chemin le plus critique (réponse client).
- **T030** reste `[ ]` — **DETTE ACCEPTÉE, PRIORITÉ ÉLEVÉE** (révisé) : aucun écran d'administration
  des catégories. tasks.md §4 le marquait "optionnel UI" dès la conception, mais REC-02 (Must) exige
  littéralement "sans développement" — sans écran ni API exposée, un administrateur ne peut agir sur
  le référentiel sans l'aide d'un développeur, ce qui est en tension directe avec le texte CDC. Voir
  closure-review.md pour l'arbitrage complet (classé C avec réserve, pas E — la décision de
  conception d'origine n'est pas explicitement invalidée par le CDC, mais la tension est réelle et
  documentée, pas ignorée).
- Toutes les autres tâches ont une preuve directe : fichier de code, fichier de test passant, ou
  (T034) fichier de migration + application réelle vérifiée par `dotnet ef database update`.
- **Correctif transversal hors périmètre des 35 tâches** : le bug `FicheTicket.razor` →
  `PublierMessageClientAsync` (mauvais identifiant, cassait la réponse client sur TOUS les modules
  F001-F006) a été corrigé — une ligne, `_fiche!.SocieteId` au lieu de `_utilisateurId`, uniquement
  dans la branche client de `EnvoyerMessageAsync`. Aucune tâche T001-T035 ne couvrait ce défaut
  explicitement (il n'a été découvert que lors de la revue de clôture indépendante).

TOTAL TÂCHES : 35
TÂCHES RÉELLEMENT DÉMONTRÉES : 33/35 (T026, T030 restants)
TÂCHES [P] : 4
PHASES : 10
REC COUVERTES : 6/6
CONTRATS C7 COUVERTS : 5/5 (100%) — C7-03 corrigé en remédiation (Complaint.Repondre)
CLARIFICATIONS COUVERTES : 8/8
CAS REC-FLOW : Couverts (contenu obligatoire, catégorie sans file, transitions interdites)
CAS REC-SEC : REC-SEC-01→06 couverts (T018, T032 + tests négatifs par commande)
REC-SCE-07 : Couvert — `RecSce07CustomerComplaintE2ETests.cs` (remédiation)
BASELINE PROTÉGÉE : 479 → 496 tests verts, 0 régression
AVE : ABSENT (Hors périmètre respecté)
POINTS BLOQUANTS : AUCUN

VERDICT : REMÉDIATION COMPLETE POUR LES 4 ÉCARTS AUDITÉS — T026/T030 restent des dettes documentées, non bloquantes pour la fonction (backend C7-05 testé, sans écran).
