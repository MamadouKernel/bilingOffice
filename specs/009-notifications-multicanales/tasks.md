---
description: "Task list for Feature 009 — Notifications Multicanales & Centre de Notifications In-App"
---

# Tasks: Feature 009 — Notifications Multicanales & Centre de Notifications In-App

**Input** : `specs/009-notifications-multicanales/{spec.md, plan.md, research.md, data-model.md, contracts/use-cases.md, quickstart.md, checklists/requirements.md}`, `.specify/memory/constitution.md`, **code réel F001→F008 audité fichier par fichier**.

**Tests** : **INCLUS — TEST-FIRST obligatoire** (Constitution Principe XII ; demande explicite : *« appliquer TEST-FIRST à chaque comportement »*). Chaque comportement reçoit une paire **RED → GREEN**.

**Organisation** : Preflight → Fondations → User Stories (par priorité) → UI → Sécurité → REC-SCE-14 (volets F009) → Non-régression & contrôles finaux.

**Statut** : **GÉNÉRÉ ET REVU, NON EXÉCUTÉ.** `/speckit.implement` n'est pas lancé, aucune migration n'est créée, aucun code produit n'est modifié par ce document.

**Revue de cohérence du 1er septembre 2026** : 188 tâches auditées → **2 fusionnées** (T002, T096 — sépultures conservées) et **15 ajoutées** (T189→T203). **Total exécutable : 201.** Écarts corrigés : cycle de vie complet du curseur `DistribueLeUtc` (scénarios A/C/**D — concurrence, manque critique**), garantie de distribution **I9** énoncée, condition d'arrêt du retry, 3 cas limites de la mention, cantonnement DEV/TEST du seed, cantonnement des canaux mobiles, interdiction de coordonnée arbitraire, scan secrets/configuration, sélectivité Git.

**Priorités** : P1 = exigence Must · P2 = Should · P3 = Could (mapping de `spec.md` §2).

---

## Invariants de périmètre — opposables à chaque tâche

Toute tâche ci-dessous est **refusée** si elle viole l'un de ces huit invariants :

| # | Invariant | Source |
|:--|:---|:---|
| **I1** | **Aucun fournisseur** e-mail / SMS / WhatsApp n'est nommé, référencé, ni ajouté en dépendance | `spec.md` §8 · research.md D10 · Principe XI |
| **I2** | **Aucune valeur numérique de reprise** (nombre de tentatives, cadence, condition d'arrêt) n'est écrite dans le code | CLAR-NOT-02 · research.md D9 |
| **I3** | **Aucune règle de diffusion PC-17 n'est semée** — seules les règles de niveau **CDC-EXIGENCE** le sont | CLAR-COM-02 · data-model.md §7 |
| **I4** | **Aucune exigence CON-01→04 ni ERR-04**, aucune logique de rendez-vous, guichet, créneau ou continuité présentielle | `spec.md` §23 · Feature 010 |
| **I5** | **Migration strictement additive** — aucune suppression de table, colonne ou contrainte ; aucune donnée existante modifiée | data-model.md §7 · Principe VIII |
| **I6** | **Les 21 sites de publication d'`EvenementSocle` existants (19 fichiers) restent inchangés**, à la seule exception des 2 ajouts IMP-11 (T126, T127) | research.md D1, D7 |
| **I7** | **MSG-04 et ALE-01→03 ne sont jamais recomptées** dans les 9 exigences F009 | `spec.md` §27 · research.md D5, D6 |
| **I8** | **Aucun contenu de message journalisé**, aucun jeton dans un lien, aucun broadcast | `spec.md` §14, §16 · research.md D12 |
| **I9** | **Garantie de distribution : *at-least-once* au niveau du traitement, rendue *effectively-once* par idempotence.** Le terme *exactly-once* **ne doit apparaître nulle part** — il n'est pas démontrable ici | Ajouté par la revue de cohérence (§7) |

### I9 — Garantie de distribution, énoncée sans ambiguïté

Le moteur lit un journal persistant et marque un curseur : il ne peut donc pas garantir *exactly-once*. La garantie réellement visée, et la seule qui sera revendiquée :

| Niveau | Garantie | Mécanisme |
|:---|:---|:---|
| **Traitement d'un événement** | **at-least-once** | un événement dont le marquage n'a pas été committé sera relu au tour suivant |
| **Effet observable (notification créée)** | **effectively-once** | **index unique `(EvenementSourceId, DestinataireId)`** (T028) — une seconde distribution ne peut pas produire de doublon |
| **Envoi sur un canal externe** | **at-least-once** | ERR-02 exige la reprise ; un destinataire peut recevoir deux fois le même message si le tiers a abouti sans confirmer. **Le CDC ne l'interdit pas et aucune déduplication externe n'est inventée** |

**Le mot *exactly-once* est proscrit** de tout artefact et de tout commentaire de code de F009.

---

## Phase 1 : Preflight / Baseline (réelle, exécutée)

**But** : établir la baseline avant toute modification. Aucune tâche de code.

- [X] T001 **Baseline réelle exécutée le 1er septembre 2026** (PowerShell natif) :
  - `dotnet build` → **0 avertissement, 0 erreur**.
  - `CIT.Plateforme.Domain.Tests` → **86/86 verts**.
  - `CIT.Plateforme.Application.Tests` → **319/319 verts**.
  - `CIT.Plateforme.Infrastructure.Tests` → **67/67 verts**.
  - `CIT.Plateforme.Web.Tests` → **136/136 verts**.
  - **Total : 608/608 verts.**
  - `dotnet ef migrations has-pending-model-changes` → *« No changes have been made to the model since the last migration. »*
  - **Gate franchi** : baseline 100 % verte, aucune tolérance résiduelle, aucune remédiation nécessaire. Les tâches de code (Phase 2 et suivantes) peuvent démarrer.
- [X] T002 ~~Gate franchi~~ — **FUSIONNÉE DANS T001** par la revue de cohérence (tâche purement administrative, sans acte propre). Identifiant conservé comme sépulture pour préserver la numérotation et l'auditabilité.

**Checkpoint** : baseline 608/608. ✅ ATTEINT.

---

## Phase 2 : Fondations (bloquant — requis par toutes les User Stories)

**⚠️ CRITIQUE** : aucune tâche de User Story ne démarre avant la fin de cette phase.

### 2.1 Domaine — énumérations

- [X] T003 [P] Créer `src/CIT.Plateforme.Domain/Notifications/CanalNotification.cs` : énumération `InApp | Email | Sms | WhatsApp`. **Aucun autre canal** — ni push mobile, ni courrier, ni voix, ni webhook (`spec.md` §9, invariant I1).
- [X] T004 [P] Créer `src/CIT.Plateforme.Domain/Notifications/StatutEnvoi.cs` : énumération `EnAttente | Envoye | Echec | NonDelivrable`. **Aucun statut « abandonné »** — la condition d'arrêt du retry n'est pas spécifiée (CLAR-NOT-02, invariant I2).

### 2.2 Domaine — `Notification` (CDC §20 : destinataire, modèle)

- [X] T005 TEST RED — `tests/CIT.Plateforme.Domain.Tests/Notifications/NotificationTests.cs` : `Creer` renseigne `EstLue = false`, `CreeeLeUtc`, `DestinataireId`, `TicketId`, `ModeleId`, `Titre`, `Corps` ; `Creer` refuse un `DestinataireId` vide et un `Titre` vide. (NOT-01, data-model.md §3.1)
- [X] T006 TEST RED — même fichier : `MarquerCommeLue()` passe `EstLue` à `true` et renseigne `LueLeUtc` (**cas TEST-NOT-01-2**).
- [X] T007 TEST RED — même fichier : `MarquerCommeLue()` sur une notification **déjà lue** ne modifie ni `EstLue` ni `LueLeUtc` et **ne lève aucune exception** — idempotence (**cas TEST-NOT-01-3**, data-model.md §3.1 invariant 3).
- [X] T008 TEST RED — même fichier : `Titre` et `Corps` sont **figés après création** — aucune méthode publique ne permet de les modifier (data-model.md §3.1 invariant 2, prépare **TEST-NOT-02-3**).
- [X] T009 TEST RED — même fichier : le type `Notification` **n'expose aucune méthode de suppression ni d'archivage**, et `DestinataireId` n'a pas de setter public (CLAR-NOT-03, research.md D13 ; invariant d'immuabilité 1).
- [X] T010 Implémentation GREEN — `src/CIT.Plateforme.Domain/Notifications/Notification.cs` : propriétés de data-model.md §3.1, fabrique `Creer` validante, `MarquerCommeLue()` idempotente. **Aucune méthode `Supprimer`, aucun indicateur d'archivage.**

### 2.3 Domaine — `EnvoiNotification` (CDC §20 : canal, statut d'envoi)

- [X] T011 TEST RED — `tests/CIT.Plateforme.Domain.Tests/Notifications/EnvoiNotificationTests.cs` : `Creer` positionne `Statut = EnAttente`, `NombreTentatives = 0`, `Canal` renseigné.
- [X] T012 TEST RED — même fichier : `EnregistrerSucces()` → `Envoye` + `AboutiLeUtc` ; `EnregistrerEchec(motif)` → `Echec` + `MotifEchec` + `NombreTentatives` incrémenté + `DerniereTentativeUtc`.
- [X] T013 TEST RED — même fichier : `MarquerNonDelivrable()` → `NonDelivrable`, statut **distinct** de `Echec` (CLAR-COM-04, data-model.md §4).
- [X] T014 TEST RED — même fichier : `MotifEchec` **ne contient jamais** le corps du message — la méthode n'accepte qu'un motif technique et le test vérifie qu'aucun champ de contenu n'est copié (invariant I8, `spec.md` §16).
- [X] T015 Implémentation GREEN — `src/CIT.Plateforme.Domain/Notifications/EnvoiNotification.cs` : data-model.md §3.2. **Aucune méthode de bascule vers un autre canal** — l'isolation ERR-02 n'est pas une suppléance (`spec.md` §15).

### 2.4 Domaine — référentiels administrables (patron `CategorieReclamation`, research.md D4)

- [X] T016 [P] TEST RED — `tests/CIT.Plateforme.Domain.Tests/Notifications/ModeleNotificationTests.cs` : `Creer` valide `TypeEvenement`, `Canal`, `GabaritTitre`, `GabaritCorps` non vides ; `EstActif = true` ; `MettreAJour`, `Activer`/`Desactiver`. **Aucune suppression physique.** (COM-03)
- [X] T017 [P] TEST RED — `tests/CIT.Plateforme.Domain.Tests/Notifications/RegleDiffusionTests.cs` : `Creer` valide `TypeEvenement` et `Canal` ; `EstObligatoire` positionnable ; `EstActif = true` ; `MettreAJour`, `Activer`/`Desactiver`. (NOT-02)
- [X] T018 [P] TEST RED — `tests/CIT.Plateforme.Domain.Tests/Notifications/PreferenceCanalUtilisateurTests.cs` : `Creer` avec `EstActif = true` par défaut ; `Activer`/`Desactiver`. (COM-05)
- [X] T019 [P] Implémentation GREEN — `src/CIT.Plateforme.Domain/Notifications/ModeleNotification.cs` (data-model.md §3.3).
- [X] T020 [P] Implémentation GREEN — `src/CIT.Plateforme.Domain/Notifications/RegleDiffusion.cs` (data-model.md §3.4).
- [X] T021 [P] Implémentation GREEN — `src/CIT.Plateforme.Domain/Notifications/PreferenceCanalUtilisateur.cs` (data-model.md §3.5).
- [X] T022 Vérifier : `dotnet test tests/CIT.Plateforme.Domain.Tests` → **86 existants + nouveaux, tous verts**. Non-régression Domain confirmée.

### 2.5 Extension additive de `EvenementSocle` (research.md D2 — touche F001)

- [X] T023 TEST RED — `tests/CIT.Plateforme.Domain.Tests/Audit/EvenementSocleTests.cs` (ou extension de la suite existante) : `Creer` laisse `DistribueLeUtc` à `null` ; `MarquerDistribue(maintenant)` le renseigne ; un second appel **n'écrase pas** la première valeur (idempotence du curseur).
- [X] T024 Implémentation GREEN — `src/CIT.Plateforme.Domain/Audit/EvenementSocle.cs` : **ajouter la seule propriété `DistribueLeUtc` (`DateTime?`, nullable)** et la méthode `MarquerDistribue`. **Aucune propriété existante n'est modifiée ni supprimée** (invariant I5).
- [X] T025 **Non-régression F001 — Audit** : rejouer la suite Audit/Historique existante. Tout échec **bloque** la phase.

### 2.6 Sécurité / RBAC

- [X] T026 `src/CIT.Plateforme.Domain/Securite/NatureUtilisateur.cs` : ajouter `Permission.AdministrationNotifications = "administration.notifications"`. **Aucune autre permission** — pas de permission « consulter les notifications d'autrui », qui n'existe pas (`spec.md` §14 règle 1).

### 2.7 Persistance — configuration EF et migration (research.md D2, data-model.md §7)

> **Justification traçable exigée pour chaque objet de schéma.** Aucune table, aucune colonne, aucun index n'est créé sans exigence source.

- [X] T027 `src/CIT.Plateforme.Infrastructure/Persistence/PlateformeDbContext.cs` : ajouter les 5 `DbSet` — `Notifications`, `EnvoisNotification`, `ModelesNotification`, `ReglesDiffusion`, `PreferencesCanalUtilisateur`.
- [X] T028 Même fichier — configuration `Notification` : **justification** = CDC §20 (attributs *destinataire* et *modèle*) + NOT-01 (état Lu/Non lu). **Index unique `(EvenementSourceId, DestinataireId)`** — justification : idempotence de distribution (RSQ-02, research.md D2).
- [X] T029 Même fichier — configuration `EnvoiNotification` : **justification** = CDC §20 (attributs *canal* et *statut d'envoi*) + ERR-02 (isolation et reprise par canal). **Index `(Statut, DerniereTentativeUtc)`** — justification : sélection bornée des envois à reprendre (Principe XIII).
- [X] T030 Même fichier — configuration `ModeleNotification` : **justification** = COM-03 (*« modèles de messages paramétrables par module, par étape, par canal »*). **Index unique `(TypeEvenement, Canal)`** sur les modèles actifs.
- [X] T031 Même fichier — configuration `RegleDiffusion` : **justification** = NOT-02 (*« règles de diffusion paramétrables par événement et par canal »*) + CLAR-COM-03 (`EstObligatoire`). **Index unique `(TypeEvenement, Canal)`**.
- [X] T032 Même fichier — configuration `PreferenceCanalUtilisateur` : **justification** = COM-05 (*« préférences de canaux par client »*). **Index unique `(UtilisateurId, Canal)`**.
- [X] T033 Même fichier — configuration de la colonne `JournalEvenements.DistribueLeUtc` : **justification** = curseur de distribution (research.md D2), **nullable**, **indexée** pour borner la sélection des événements non traités.
- [X] T034 **Créer la migration additive** `AjoutNotifications` — `dotnet ef migrations add AjoutNotifications`. **Relire le fichier généré et vérifier ligne par ligne** : 5 `CreateTable`, 1 `AddColumn` nullable, les index attendus, et **aucun `DropTable`, `DropColumn`, `DropIndex`, `AlterColumn` destructif ni `Sql(...)` de mise à jour de données** (invariant I5). Toute occurrence destructive **annule la migration**.
- [X] T035 Vérifier : `dotnet ef migrations has-pending-model-changes` → **aucun changement en attente** après application.

### 2.8 Persistance — repositories

- [X] T036 `src/CIT.Plateforme.Application/Abstractions/Repositories.cs` : ajouter `INotificationRepository`, `IEnvoiNotificationRepository`, `IModeleNotificationRepository`, `IRegleDiffusionRepository`, `IPreferenceCanalRepository`. **`INotificationRepository` n'expose aucune méthode de suppression** (research.md D13).
- [X] T037 TEST RED — `tests/CIT.Plateforme.Infrastructure.Tests/Persistence/NotificationRepositoryTests.cs` (SQL Server réel, jamais InMemory) : l'index unique `(EvenementSourceId, DestinataireId)` **rejette** un second insert identique (idempotence).
- [X] T038 TEST RED — même fichier : `ListerParDestinataireAsync` ne retourne **que** les notifications du destinataire demandé, deux destinataires étant peuplés (cloisonnement au niveau du dépôt).
- [X] T039 TEST RED — même fichier : les index uniques `(TypeEvenement, Canal)` de `ModeleNotification` et `RegleDiffusion`, et `(UtilisateurId, Canal)` de `PreferenceCanalUtilisateur`, rejettent les doublons.
- [X] T040 Implémentation GREEN — `src/CIT.Plateforme.Infrastructure/Persistence/Repositories.cs` : les 5 repositories.
- [X] T041 Vérifier : `dotnet test tests/CIT.Plateforme.Infrastructure.Tests` → **67 existants + nouveaux, tous verts** sur SQL Server réel.

### 2.9 Abstractions applicatives

- [X] T042 [P] Créer `src/CIT.Plateforme.Application/Abstractions/ICanalNotification.cs` : expose `Canal` et `EnvoyerAsync(...)`. **Aucun nom de fournisseur, aucun paramètre spécifique à un protocole** (invariant I1, research.md D10).
- [X] T043 [P] Créer `src/CIT.Plateforme.Application/Abstractions/IResolveurDestinataires.cs` : retourne une **liste d'identifiants d'utilisateurs nommés**. La signature **n'admet ni file, ni groupe, ni rôle** comme destinataire (CLAR-NOT-04, invariant I8).
- [X] T044 TEST RED — `tests/CIT.Plateforme.Infrastructure.Tests/Notifications/CanalJournaliseTests.cs` : `CanalJournalise` enregistre la tentative et son résultat **sans contacter aucun tiers**.
- [X] T045 Implémentation GREEN — `src/CIT.Plateforme.Infrastructure/Notifications/CanalJournalise.cs`. **Aucune dépendance réseau, aucun client SMTP/HTTP, aucun SDK** (invariant I1).
- [X] T203 **Cantonnement des canaux mobiles — DIFFÉRÉ / AVANT PRODUCTION** (ajout de la revue de cohérence, §17) : `Sms` et `WhatsApp` sont des valeurs de l'énumération et des canaux **paramétrables**, servis par `CanalJournalise` comme l'e-mail. **Aucune tâche de F009 ne requiert un fournisseur réel.** TEST : le **MVP In-App + E-mail est intégralement démontrable avec les deux canaux mobiles désactivés** — aucun Must de F009 ne devient inatteignable (PC-16, `spec.md` §8, RSQ-04). Le branchement d'un fournisseur réel est **DIFFÉRÉ / AVANT PRODUCTION** et **hors périmètre de `/speckit.implement`**.

### 2.10 Résolveurs de destinataires (research.md D11)

- [X] T046 TEST RED — `tests/CIT.Plateforme.Application.Tests/Notifications/ResolveurClientDuDossierTests.cs` : résout le propriétaire du dossier (`Dossier.ClientId`).
- [X] T047 TEST RED — même fichier : inclut les collaborateurs actifs de la société **dont le `DroitConsultation` couvre les demandes de la société**, et **exclut** ceux en `PropresDemandesUniquement` — le droit existant est **lu, jamais réinterprété** (moindre privilège, Principe VII).
- [X] T048 TEST RED — même fichier : **ne résout jamais** un utilisateur d'une autre société (cloisonnement CPT-04 / SEC-01).
- [X] T049 TEST RED — `tests/CIT.Plateforme.Application.Tests/Notifications/ResolveurCollaborateurMentionneTests.cs` : lit `collaborateurId` dans la charge utile de `CollaborateurMentionne` et **ne retourne que lui** (**cas TEST-MSG-04-3**).
- [X] T050 [P] Implémentation GREEN — `src/CIT.Plateforme.Application/Notifications/Resolveurs/ResolveurClientDuDossier.cs`.
- [X] T051 [P] Implémentation GREEN — `src/CIT.Plateforme.Application/Notifications/Resolveurs/ResolveurCollaborateurMentionne.cs`.
- [X] T052 **Vérification d'abstention** — TEST : aucun résolveur « file », « tous les agents », « tous les habilités » ni **« superviseur »** n'existe dans `Resolveurs/`. Test d'inventaire par réflexion sur les implémentations de `IResolveurDestinataires` : **exactement 2** (research.md D6, D11 ; invariant I7).

**Checkpoint Phase 2** : 5 entités, 1 extension additive, 1 migration additive vérifiée, 5 repositories, 2 abstractions, 2 résolveurs. Domain + Infrastructure verts. ✅ Les User Stories peuvent démarrer.

---

## Phase 3 : US2 + US3 — Émission et lien sécurisé (Priorité P1 — Must) 🎯 MVP

**Contrats** : **C9-01** (distribuer), **C9-05** (accès au ticket)
**Exigences** : COM-01, COM-02, NOT-02, COM-03 + contrainte SEC-12
**Test d'indépendance** : un événement déclencheur configuré produit, pour le bon destinataire, une notification portant un lien vers le ticket — et rien d'autre.

### Tests RED — moteur de distribution (C9-01)

- [X] T053 [P] [US2] TEST RED — `tests/CIT.Plateforme.Application.Tests/Notifications/DistribuerNotificationsUseCaseTests.cs` : un événement **sans aucune `RegleDiffusion`** est marqué distribué **sans produire aucune notification** — le silence du paramétrage ne notifie pas (`spec.md` §10, invariant I3).
- [X] T054 [P] [US2] TEST RED — même fichier : un événement avec une règle `InApp` produit **une** `Notification` pour le destinataire résolu, `EstLue = false`, `TicketId` renseigné (**cas TEST-NOT-02-1** partie In-App).
- [X] T055 [P] [US2] TEST RED — même fichier : **aucun `EnvoiNotification` n'est créé pour le canal `InApp`** — la `Notification` **est** sa diffusion (research.md D3).
- [X] T056 [P] [US2] TEST RED — même fichier : une règle multi-canaux crée **un `EnvoiNotification` par canal externe**, chacun en `EnAttente` (**cas TEST-NOT-02-1**).
- [X] T057 [P] [US2] TEST RED — même fichier : **idempotence** — rejouer le même événement ne crée pas de doublon pour le même destinataire (index unique T028).
- [X] T058 [P] [US2] TEST RED — même fichier : `Titre`/`Corps` sont **rendus depuis le `ModeleNotification`** du couple `(TypeEvenement, InApp)` et **figés** à la création (**prépare TEST-NOT-02-3**).
- [X] T059 [P] [US2] TEST RED — même fichier : une règle `EstObligatoire = false` dont le canal est désactivé par la préférence du destinataire **ne crée pas** l'`EnvoiNotification` correspondant ; une règle `EstObligatoire = true` **le crée malgré** la préférence (**cas TEST-NOT-02-7**).
- [X] T060 [P] [US2] TEST RED — même fichier : **cas TEST-NOT-02-8** — canal présent au paramétrage mais non servable (coordonnée absente, ou canal non souscrit — PC-16) → `EnvoiNotification` en **`NonDelivrable`**, **tracé**, les **autres canaux aboutissent normalement**, et **aucun Must de F009 n'est rendu inatteignable** (CLAR-COM-04, `spec.md` §18 bis).
- [X] T061 [P] [US2] TEST RED — même fichier : **étanchéité des notes internes** — un événement `MessagePoste` de type note interne **ne résout jamais** un destinataire client et **ne produit aucune notification client**, sur aucun canal (MSG-02, `spec.md` §14 règle 3, invariant I8).
- [X] T062 [P] [US2] TEST RED — même fichier : un échec de distribution sur un événement **n'empêche pas** le traitement des événements suivants.
- [X] T063 [P] [US2] TEST RED — même fichier : **absence de broadcast** — aucun chemin ne produit de notification pour un destinataire non retourné par un résolveur ; un dossier à un seul propriétaire produit exactement une notification (CLAR-NOT-04, invariant I8).

### Implémentation

- [X] T064 [US2] Implémentation GREEN — `src/CIT.Plateforme.Application/Notifications/DistribuerNotificationsUseCase.cs` (C9-01) : lecture des événements `DistribueLeUtc is null` par ordre chronologique, résolution, application des règles et préférences, rendu du modèle, création de `Notification` + `EnvoiNotification`, marquage du curseur.

### Cycle de vie du curseur `DistribueLeUtc` — 4 scénarios (ajout de la revue de cohérence, §6)

> **Ces quatre scénarios sont indissociables** : sans eux, la garantie I9 n'est pas démontrée. Le scénario D était **absent** de la première version de ce document.

- [X] T189 [US2] TEST RED — **scénario A (chaîne complète)** : un `EvenementSocle` avec `DistribueLeUtc is null` et une règle applicable → les `Notification` (et `EnvoiNotification` externes) sont **persistées**, **puis** l'événement est marqué `DistribueLeUtc`. Vérifier **l'ordre** : le marquage ne précède jamais la persistance.
- [X] T190 [US2] TEST RED — **scénario C (échec avant persistance)** : si la création des notifications échoue, l'événement **reste `DistribueLeUtc is null`** et sera **relu au tour suivant** — marquage et persistance sont **dans la même transaction**, jamais committés séparément. Garantie **at-least-once** (I9).
- [X] T191 [US2] TEST RED — **scénario D (concurrence de deux workers)** : deux exécutions **simultanées** de la distribution sur le même événement ne produisent **jamais deux notifications** pour le même destinataire. Test d'intégration sur **SQL Server réel** (jamais InMemory), deux `IServiceScope` concurrents. La protection repose sur **deux lignes de défense cumulées** : (1) prise de l'événement par une opération **atomique** (mise à jour conditionnelle sur `DistribueLeUtc is null`), (2) **index unique `(EvenementSourceId, DestinataireId)`** (T028) qui rejette le doublon même si la première défense est franchie.
- [X] T192 [US2] Implémentation GREEN — appliquer les deux lignes de défense dans `DistribuerNotificationsUseCase` : prise atomique de l'événement, puis capture de la violation d'unicité comme **résultat normal** (l'événement est déjà distribué) et non comme erreur. **Documenter la garantie I9 dans le commentaire d'en-tête ; ne jamais écrire *exactly-once*.**
- [X] T193 [US2] TEST — **scénario B (second passage)** : rejouer entièrement la distribution ne crée aucun doublon et ne modifie aucune notification existante. *(Complète T057, qui teste l'idempotence au niveau du cas d'usage ; T193 la teste au niveau du traitement de fond complet.)*
- [X] T065 [US2] **Vérification d'abstention** : le use case **n'appelle aucun canal externe en ligne** — l'envoi est délégué à la reprise asynchrone (Phase 7). L'échec d'un canal **ne peut structurellement pas** remonter dans la transaction métier d'origine (research.md D1, `spec.md` §15).

### Tests RED — lien sécurisé (C9-05, SEC-12)

- [X] T066 [P] [US3] TEST RED — `tests/CIT.Plateforme.Application.Tests/Notifications/LienNotificationTests.cs` : la notification porte `TicketId`, **jamais une URL absolue stockée** (research.md D12).
- [X] T067 [P] [US3] TEST RED — même fichier : **aucun jeton d'accès, aucun lien signé, aucun paramètre de contournement** n'est produit, sur aucun canal (**cas TEST-NOT-01-4**, invariant I8).
- [X] T068 [P] [US3] TEST RED — même fichier : **aucun lien direct vers un document protégé** n'est généré — le lien conduit au ticket (SEC-12, `spec.md` §14 règle 4).
- [X] T069 [US3] Implémentation GREEN — résolution du lien relatif `/tickets/{id}` côté rendu, sans stockage d'URL.

**Checkpoint US2/US3** : le moteur émet des notifications correctes, cloisonnées, sans fuite de note interne, sans broadcast, avec un lien sans jeton. **MVP atteint.**

---

## Phase 4 : US1 + US4 — Centre de notifications, Lu/Non lu, compteur (Priorité P1 — Must)

**Contrats** : **C9-02** (consulter), **C9-03** (compter), **C9-04** (marquer lue)
**Exigences** : NOT-01 (Must), NOT-03 (Should)
**Test d'indépendance** : un utilisateur voit ses notifications, leur état, leur date, leur lien, et son compteur ; il ne voit jamais celles d'autrui.

- [X] T070 [P] [US1] TEST RED — `tests/CIT.Plateforme.Application.Tests/Notifications/ConsulterMesNotificationsQueryTests.cs` : la liste retourne **état, date, type, titre, corps et référence du ticket** (**cas TEST-NOT-01-1**, `spec.md` §6).
- [X] T071 [P] [US1] TEST RED — même fichier : **cloisonnement destinataire (anti-IDOR)** — l'utilisateur A ne reçoit jamais une notification adressée à B, **même en connaissant son identifiant**, et l'existence n'est pas divulguée (**cas TEST-NOT-01-6**).
- [X] T072 [P] [US1] TEST RED — même fichier : **cloisonnement inter-sociétés** — deux clients de sociétés différentes ne voient jamais les notifications l'un de l'autre (**cas TEST-NOT-01-7**, CPT-04 / SEC-01).
- [X] T073 [P] [US1] TEST RED — même fichier : un **administrateur fonctionnel n'a aucun accès** aux notifications d'autrui (`spec.md` §14 règle 1).
- [X] T074 [P] [US1] TEST RED — même fichier : l'identité est **dérivée du contexte authentifié** — la requête n'accepte pas d'identifiant de destinataire fourni par l'appelant (Principe VII).
- [X] T075 [P] [US1] TEST RED — `tests/CIT.Plateforme.Application.Tests/Notifications/CompterNonLuesQueryTests.cs` : compte exact des non-lues du seul utilisateur authentifié ; **zéro est un résultat normal** (NOT-03, **cas TEST-NOT-01-1**).
- [X] T076 [P] [US4] TEST RED — `tests/CIT.Plateforme.Application.Tests/Notifications/MarquerNotificationLueCommandTests.cs` : marquage → état `Lu`, compteur décrémenté, transition **auditée** (**cas TEST-NOT-01-2**).
- [X] T077 [P] [US4] TEST RED — même fichier : marquage d'une notification **déjà lue** → aucun changement, compteur inchangé, **aucune erreur** (**cas TEST-NOT-01-3**).
- [X] T078 [P] [US4] TEST RED — même fichier : marquage par un **non-destinataire** → refus **côté serveur**, sans divulguer l'existence (**cas TEST-NOT-01-6**).
- [X] T079 [US1] Implémentation GREEN — `ConsulterMesNotificationsQuery.cs` (C9-02) : **filtre serveur par égalité stricte** `DestinataireId == utilisateur authentifié`, appliqué **dans la requête**, jamais après chargement.
- [X] T080 [US1] Implémentation GREEN — `CompterNonLuesQuery.cs` (C9-03), même filtre.
- [X] T081 [US4] Implémentation GREEN — `MarquerNotificationLueCommand.cs` (C9-04) : idempotent, autorisation serveur, audit `NotificationLue`.
- [X] T082 [US4] **Vérification d'abstention ACR-01** — TEST : marquer une notification comme lue **ne modifie ni ne consulte** le bloc « Mes actions requises » ; aucune référence au mécanisme ACR-01 dans le code F009 (`spec.md` §11).

**Checkpoint US1/US4** : `TEST-NOT-01` cas 1, 2, 3, 6, 7 couverts (cas 4, 5, 8 en Phase 10).

---

## Phase 5 : US9 — Raccordement de la mention `@Nom` (Priorité P1 — capacité Must)

**Contrat** : **C9-11**
**Capacité tracée** : **NOT-01 (F009)** appliquée à un destinataire **agent**
**Exigence raccordée** : **MSG-04 (Should) — propriété de F001, NON RECOMPTÉE** dans les 9 exigences F009 (invariant I7, `spec.md` §27)
**Décision** : Option B validée (`spec.md` §13.1)

> **Contrainte de conception (research.md D5)** : ce raccordement se fait **par configuration seule**. **Si une modification de `MentionnerCollaborateurCommand.cs` ou de `Message.cs` s'avère nécessaire, c'est que la conception a dévié** — arrêter et réexaminer avant de coder.

- [X] T083 [P] [US9] TEST RED — `tests/CIT.Plateforme.Application.Tests/Notifications/RaccordementMentionTests.cs` : **cas TEST-MSG-04-1 (nominal)** — une note interne mentionnant `@C` produit **une notification In-App pour `C`**, `Non lu`, avec date, type, titre et **lien vers le ticket** ; le compteur de `C` est incrémenté.
- [X] T084 [P] [US9] TEST RED — même fichier : **cas TEST-MSG-04-2 (e-mail conditionnel)** — sans règle `(CollaborateurMentionne, Email)` active : **aucun e-mail**, In-App produite. Avec la règle active : e-mail **en plus**. **F009 n'active jamais ce canal par défaut.**
- [X] T085 [P] [US9] TEST RED — même fichier : **cas TEST-MSG-04-3 (destinataire unique)** — plusieurs agents habilités sur le ticket, **seul `C`** est notifié ; ni la file, ni les autres habilités.
- [X] T086 [P] [US9] TEST RED — même fichier : **cas TEST-MSG-04-4 (étanchéité client, MSG-02)** — **aucune notification vers le client**, sur aucun canal ; le contenu de la note interne n'apparaît dans aucune notification client.
- [X] T087 [P] [US9] TEST RED — même fichier : **cas TEST-MSG-04-5 (non-réaffectation, MSG-05)** — le ticket **reste affecté à son opérateur courant** ; le raccordement ne produit **aucun effet de workflow**.
- [X] T088 [P] [US9] TEST RED — même fichier : **cas TEST-MSG-04-6 (non-duplication)** — la note reste dans le **fil unique** du ticket (SOC-05 / COM-04) ; la notification la **signale sans la dupliquer** ; **aucun second journal de messages** (`spec.md` §12).
- [X] T089 [P] [US9] TEST RED — même fichier : **cas TEST-MSG-04-7 (cloisonnement de lecture)** — un autre agent `A`, connaissant l'identifiant, se voit **refuser** l'accès côté serveur ; la notification n'apparaît ni dans sa liste ni dans son compteur.
### Cas limites de la mention (ajout de la revue de cohérence, §9)

- [X] T194 [P] [US9] TEST RED — même fichier : **collaborateur inexistant ou désactivé** — `collaborateurId` ne correspond à aucun utilisateur actif → **aucune notification n'est créée**, le fait est **tracé**, et **la distribution des autres événements n'est pas interrompue** (robustesse du résolveur, T062).
- [X] T195 [P] [US9] TEST RED — même fichier : **destinataire hors périmètre** — un `collaborateurId` désignant un **compte client** ou un utilisateur d'une **autre société** → **aucune notification n'est produite** ; une note interne ne peut jamais devenir une notification client, même par un identifiant mal formé (MSG-02, CPT-04, invariant I8).
- [X] T196 [P] [US9] TEST RED — même fichier : **événement de mention rejoué** — redistribuer `CollaborateurMentionne` ne crée **aucun doublon** pour le collaborateur mentionné (index unique T028, garantie I9).

- [X] T090 [US9] Implémentation GREEN — enregistrer `ResolveurCollaborateurMentionne` pour le type `CollaborateurMentionne` ; **arranger** la `RegleDiffusion` `(CollaborateurMentionne, InApp)` et le `ModeleNotification` correspondant. **Chaque test de cette phase arrange sa propre règle en fixture** — il ne dépend pas du `DevelopmentSeeder` (Phase 13). **Aucune règle `Email` n'est ni arrangée par défaut, ni semée.**
- [X] T091 [US9] **Vérification de non-modification (invariant I6)** — `git diff --stat` doit montrer **0 ligne modifiée** dans `src/CIT.Plateforme.Application/Messagerie/MentionnerCollaborateurCommand.cs` et `src/CIT.Plateforme.Domain/Messagerie/Message.cs`.
- [X] T092 [US9] **Non-régression F001 — Messagerie** : rejouer intégralement la suite Messagerie existante (`PublierMessageCommandTests`, mention, notes internes). Tout échec **bloque**.

**Checkpoint US9** : `TEST-MSG-04` cas 1→7 couverts (cas 8 en Phase 6). MSG-04 raccordée, **non recomptée**.

---

## Phase 6 : ALE-03 — Point d'intégration fourni, non activé (Priorité P1 — abstention vérifiée)

**Décision** : Option B — *« F009 fournit seulement le contrat/point d'intégration… sans réimplémenter le workflow SLA de F001 »* (`spec.md` §13.1, research.md D6)

> **Cette phase ne produit aucun code applicatif.** Elle démontre par des tests que le point d'intégration **existe** et que F009 **s'abstient** de l'utiliser.

- [X] T093 [P] TEST — `tests/CIT.Plateforme.Application.Tests/Notifications/PointIntegrationSlaTests.cs` : **existence du point d'intégration** — `SurveillerEcheancesUseCase` (F001) publie bien un `EvenementSocle` de type `SlaBientotDepasse` / `SlaDepasse` lors de la création d'une alerte, et ces événements sont **visibles** par le moteur de distribution.
- [X] T094 [P] TEST — même fichier : **cas TEST-MSG-04-8 (absence de notification)** — un dépassement de SLA déclenchant une escalade **ne produit aucune notification F009**, tant qu'aucune `RegleDiffusion` n'est configurée pour ces types ; le mécanisme F001 s'exécute **inchangé**.
- [X] T095 [P] TEST — même fichier : **aucune `RegleDiffusion` n'est semée** pour `SlaBientotDepasse` ni `SlaDepasse` — assertion sur le contenu du seeder (invariant I3, I7).
- [ ] T096 ~~Aucun résolveur « superviseur » n'existe~~ — **FUSIONNÉE DANS T052** par la revue de cohérence : T052 teste déjà par réflexion qu'il existe **exactement 2** implémentations de `IResolveurDestinataires`, ce qui couvre intégralement cette assertion. Identifiant conservé comme sépulture.
- [X] T097 **Vérification de non-modification (invariant I6)** — `git diff --stat` doit montrer **0 ligne modifiée** dans `src/CIT.Plateforme.Application/Supervision/SurveillerEcheancesUseCase.cs` et `src/CIT.Plateforme.Domain/Supervision/Alerte.cs`.
- [X] T098 **Non-régression F001 — Supervision / SLA / Alertes** : rejouer intégralement les suites correspondantes. Tout échec **bloque**.

**Checkpoint ALE-03** : point d'intégration démontré présent, abstention démontrée effective, ALE-01→03 **non recomptées**.

---

## Phase 7 : ERR-02 — Reprise sur échec (Priorité P1 — Must transverse)

**Contrat** : **C9-08**
**Exigence** : ERR-02 (Must, transverse — **non comptée** dans les 9 exigences F009, `spec.md` §3, §27)

> **Invariant I2 opposable à chaque tâche de cette phase** : aucune valeur numérique de reprise n'est écrite dans le code.

- [X] T099 [P] TEST RED — `tests/CIT.Plateforme.Application.Tests/Notifications/ReessayerEnvoisNotificationUseCaseTests.cs` : **cas TEST-ERR-02-1 (isolation)** — trois canaux dont un indisponible : les deux disponibles **aboutissent**, l'échec du troisième **ne les empêche pas** et n'interrompt pas la boucle.
- [X] T100 [P] TEST RED — même fichier : **cas TEST-ERR-02-2 (statut par canal)** — chaque canal porte un **statut d'envoi distinct** (CDC §20).
- [X] T101 [P] TEST RED — même fichier : **cas TEST-ERR-02-3 (traçage)** — destinataire, canal, horodatage et résultat sont tracés ; **le contenu intégral du message n'est pas journalisé** (invariant I8).
- [X] T102 [P] TEST RED — même fichier : **cas TEST-ERR-02-4 (nouvelle tentative)** — une reprise est engagée **automatiquement**, sans action humaine ; `NombreTentatives` progresse. **Le test lit les paramètres depuis la configuration de test et n'affirme aucune valeur en dur** (invariant I2).
- [X] T103 [P] TEST RED — même fichier : **cas TEST-ERR-02-5 (transaction métier préservée)** — la transaction métier d'origine **reste valide et committée** ; l'échec ne l'annule ni ne la déroule.
- [X] T104 [P] TEST RED — même fichier : **cas TEST-ERR-02-6 (absence de fallback)** — **aucune substitution automatique** vers un autre canal (`spec.md` §15).
- [X] T105 TEST RED — même fichier : **absence de configuration → erreur de démarrage explicite**, jamais une valeur devinée (research.md D9 — le raccourci de `ParametresRelance` n'est **pas** reproduit).
- [X] T197 TEST RED — même fichier (ajout de la revue de cohérence, §8) : **condition d'arrêt** — lorsque `NombreTentatives` atteint le **plafond lu depuis la configuration**, l'envoi **cesse d'être sélectionné** par la reprise et **conserve son statut `Echec` tracé**. **Le test injecte le plafond par configuration de test et n'affirme aucune valeur en dur** (invariant I2). **Aucun statut « abandonné » n'est introduit** — le CDC ne spécifie pas de terminaison (CLAR-NOT-02, data-model.md §4).
- [X] T106 Implémentation GREEN — `src/CIT.Plateforme.Application/Notifications/ReessayerEnvoisNotificationUseCase.cs` (C9-08) + enregistrement `ParametresReessaiNotification` **lu depuis la configuration**.
- [X] T107 **Vérification I2** — recherche dans `src/CIT.Plateforme.Application/Notifications/` et `src/CIT.Plateforme.Infrastructure/Notifications/` : **aucun littéral numérique** de nombre de tentatives, de délai ou de cadence. **Aucun backoff exponentiel, aucun fallback, aucune alerte administrateur, aucun message d'échec au destinataire.**

**Checkpoint ERR-02** : `TEST-ERR-02` 6/6 couverts, comportement conforme, **aucune valeur inventée**.

---

## Phase 8 : US5 — Administration des modèles et des règles (Priorité P1 — Must)

**Contrats** : **C9-06** (modèles), **C9-07** (règles)
**Exigences** : COM-03, NOT-02

- [X] T108 [P] [US5] TEST RED — `tests/CIT.Plateforme.Application.Tests/Notifications/AdministrationNotificationsTests.cs` : **cas TEST-NOT-02-1** — ajouter un canal à une règle puis déclencher l'événement → diffusion **sur les deux canaux**, **sans redéploiement**.
- [X] T109 [P] [US5] TEST RED — même fichier : **cas TEST-NOT-02-2** — retirer un canal → **un seul** canal servi, aucun envoi créé pour le canal retiré.
- [X] T110 [P] [US5] TEST RED — même fichier : **cas TEST-NOT-02-3 (non-rétroactivité)** — modifier un modèle **ne modifie pas** les notifications déjà émises ; la nouvelle règle s'applique **aux événements suivants**.
- [X] T111 [P] [US5] TEST RED — même fichier : **cas TEST-NOT-02-4 (granularité)** — modifier la règle de E1 **n'affecte pas** E2.
- [X] T112 [P] [US5] TEST RED — même fichier : **cas TEST-NOT-02-5 (autorisation)** — un utilisateur sans `AdministrationNotifications` est **refusé côté serveur**, indépendamment de l'interface.
- [X] T113 [P] [US5] TEST RED — même fichier : **cas TEST-NOT-02-6 (traçabilité)** — modification tracée avec **auteur** et valeurs **avant / après** (SOC-13).
- [X] T114 [US5] Implémentation GREEN — `src/CIT.Plateforme.Application/Notifications/AdministrerModelesNotificationCommand.cs` (C9-06). **Aucune suppression physique** — `Activer`/`Desactiver` uniquement.
- [X] T115 [US5] Implémentation GREEN — `src/CIT.Plateforme.Application/Notifications/AdministrerReglesDiffusionCommand.cs` (C9-07), y compris le positionnement de `EstObligatoire`.

**Checkpoint US5** : `TEST-NOT-02` cas 1→6 couverts (cas 7 en Phase 9, cas 8 en Phase 7).

---

## Phase 9 : US7 — Préférences de canaux (Priorité P2 — Should)

**Contrat** : **C9-09** · **Exigence** : COM-05

- [X] T116 [P] [US7] TEST RED — `tests/CIT.Plateforme.Application.Tests/Notifications/PreferencesCanauxTests.cs` : **cas TEST-NOT-02-7** — canal désactivé + règle non obligatoire → **non transmis** ; règle obligatoire → **transmis malgré** la désactivation.
- [X] T117 [P] [US7] TEST RED — même fichier : le canal **In-App n'est pas débrayable** — le désactiver rendrait NOT-01 et NOT-03 inatteignables (contrats §C9-09).
- [X] T118 [P] [US7] TEST RED — même fichier : un utilisateur **ne modifie que ses propres** préférences (refus serveur sinon).
- [X] T119 [P] [US7] TEST RED — même fichier : **aucune préférence au-delà du canal** — ni fréquence, ni langue, ni type d'événement, ni plage horaire (`spec.md` §21).
- [X] T120 [US7] Implémentation GREEN — `src/CIT.Plateforme.Application/Notifications/GererPreferencesCanauxCommand.cs` (C9-09) + audit `PreferenceCanalModifiee`.

**Checkpoint US7** : `TEST-NOT-02` 8/8 couverts.

---

## Phase 10 : US6 — Étanchéité du fil de messages (Priorité P1 — Must, vérification)

**Exigence** : COM-04 (mécanisme **déjà livré par F001** via SOC-05 — F009 le **vérifie**, ne le redéveloppe pas)

- [X] T121 [P] [US6] TEST — `tests/CIT.Plateforme.Application.Tests/Notifications/EtancheiteFilMessagesTests.cs` : les messages du ticket restent **horodatés, conservés et consultables** ; **un seul journal de messages par ticket** (`spec.md` §20 US6).
- [X] T122 [P] [US6] TEST — même fichier : une notification **signale** un message sans **être** ce message ni le dupliquer ; le modèle F009 ne contient **aucune copie** du contenu conversationnel (`spec.md` §12).
- [X] T123 [US6] **Vérification de non-modification (invariant I6)** — `git diff --stat` doit montrer **0 ligne modifiée** dans `src/CIT.Plateforme.Domain/Messagerie/` et `src/CIT.Plateforme.Application/Messagerie/`.

---

## Phase 11 : IMP-11 — Événements manquants (research.md D7 — touche F003 et F004)

> **Deux lignes au total.** Sans elles, IMP-11 (*« édite la facture… ; le client est notifié »*, **CDC-EXIGENCE**) resterait **inatteignable** par le moteur — omission silencieuse d'un Must interdite par le Principe XVI.

- [X] T124 TEST RED — `tests/CIT.Plateforme.Application.Tests/BillingImport/EditerFactureCommandTests.cs` (extension de la suite existante) : l'édition d'une facture publie un `EvenementSocle` de type `FactureEditee` portant `ticketId`.
- [X] T125 TEST RED — `tests/CIT.Plateforme.Application.Tests/BillingExport/EditerFactureExportCommandTests.cs` (extension) : idem pour l'export.
- [X] T126 Implémentation GREEN — `src/CIT.Plateforme.Application/BillingImport/EditerFactureCommand.cs` : **ajouter une seule ligne** `PublierEvenementAsync("FactureEditee", …)`, strictement calquée sur `TransfererTicketCommand`. **Aucune autre modification** — ni signature, ni règle métier, ni statut.
- [X] T127 Implémentation GREEN — `src/CIT.Plateforme.Application/BillingExport/EditerFactureExportCommand.cs` : idem.
- [X] T128 TEST — la distribution produit bien une notification client pour `FactureEditee` avec la règle semée (IMP-11 atteignable de bout en bout).
- [X] T129 **Non-régression F003 — BillingImport** : rejouer **intégralement** la suite BillingImport. Tout échec **bloque**.
- [X] T130 **Non-régression F004 — BillingExport** : rejouer **intégralement** la suite BillingExport. Tout échec **bloque**.
- [X] T131 **Vérification I6** — `git diff --stat src/CIT.Plateforme.Application/` : **exactement 2 fichiers** de commandes métier modifiés (`EditerFactureCommand.cs`, `EditerFactureExportCommand.cs`), **+1 ligne chacun**. Les **19 autres fichiers** producteurs d'`EvenementSocle` doivent afficher **0 modification**.
- [X] T132 **Vérification I3** — **aucun autre événement n'est ajouté** : « preuve de paiement reçue », « paiement validé », « documents de sortie disponibles » et « ticket clôturé » restent **PC-17-PROPOSITION** et ne reçoivent **ni publication d'événement, ni règle semée**.

---

## Phase 12 : Traitements de fond (research.md D8 — patron F001)

- [X] T133 [P] TEST RED — `tests/CIT.Plateforme.Infrastructure.Tests/TraitementsDeFond/DistributionNotificationsServiceTests.cs` : `TraiterUneFoisAsync(ct)` est appelable **sans attendre le minuteur** et distribue les événements en attente.
- [X] T134 [P] TEST RED — `tests/CIT.Plateforme.Infrastructure.Tests/TraitementsDeFond/ReessaiEnvoisNotificationServiceTests.cs` : idem pour la reprise.
- [X] T135 [P] Implémentation GREEN — `src/CIT.Plateforme.Infrastructure/TraitementsDeFond/DistributionNotificationsService.cs` : `BackgroundService` **ne portant aucune règle métier**, intervalle **configurable**, méthode `TraiterUneFoisAsync` exposée.
- [X] T136 [P] Implémentation GREEN — `src/CIT.Plateforme.Infrastructure/TraitementsDeFond/ReessaiEnvoisNotificationService.cs` : idem. **Aucune valeur de reprise en dur** (invariant I2).
- [X] T137 **Vérification de non-modification (invariant I6)** — `git diff --stat` doit montrer **0 ligne modifiée** dans `RelancesAbsenceReponseService.cs`, `SurveillanceSlaService.cs` et `DistributionAutomatiqueService.cs` : F009 en réutilise **le patron**, pas les capacités (`spec.md` §17 rectifié).
- [X] T138 `src/CIT.Plateforme.Infrastructure/DependencyInjection.cs` : enregistrer les use cases, repositories, résolveurs, canaux et les 2 `BackgroundService`. **Aucune dépendance externe ajoutée** (invariant I1).

---

## Phase 13 : Seed de développement (data-model.md §7 — strictement borné)

- [X] T139 TEST RED — `tests/CIT.Plateforme.Infrastructure.Tests/Persistence/SeedNotificationsTests.cs` : les règles semées sont **exactement** celles de niveau **CDC-EXIGENCE** — `TicketCree` (InApp + Email, `EstObligatoire = true`), `ComplementDemande` (InApp + Email), `FactureEditee` (InApp + Email), `TicketTransfere` (InApp + Email), `CollaborateurMentionne` (**InApp seul**).
- [X] T140 TEST RED — même fichier : **aucune règle PC-17-PROPOSITION n'est semée** (invariant I3) ; **aucune règle SLA n'est semée** (invariant I7) ; **aucune règle `(CollaborateurMentionne, Email)` n'est semée**.
- [X] T141 TEST RED — même fichier : `EstObligatoire = true` **uniquement** pour les couples où le CDC nomme l'événement **et** son canal — SOC-01 / IMP-03 (`spec.md` §21.1).
- [X] T142 Implémentation GREEN — `src/CIT.Plateforme.Infrastructure/Persistence/DevelopmentSeeder.cs` : méthode `SeedNotificationsAsync` + gabarits de modèles **explicitement marqués comme administrables et non contractuels** (CLAR-NOT-05).
- [X] T198 **Cantonnement DEV/TEST du seed** (ajout de la revue de cohérence, §16) : `SeedNotificationsAsync` vit dans `DevelopmentSeeder` et **n'est exécutée qu'en environnement de développement et de test** — vérifier qu'aucun chemin de démarrage de production ne l'invoque. Chaque règle et chaque gabarit semés portent un commentaire **« donnée de démarrage DEV/TEST — non contractuelle, à remplacer par le paramétrage validé par le métier (PC-17, CLAR-NOT-05) »**. **Aucune règle semée ne constitue une vérité métier**, y compris les 5 règles CDC-EXIGENCE : elles reflètent une exigence, mais leurs **canaux** restent à valider pour les 3 dont le CDC ne les nomme pas (IMP-05, IMP-11, TRF-05).

---

## Phase 14 : Interface (research.md D14 — câblage de l'existant)

- [X] T143 [P] TEST RED — `tests/CIT.Plateforme.Web.Tests/Pages/Notifications/CentreNotificationsTests.cs` (bUnit) : la liste affiche état, date, type, titre et lien ; `WaitForState` avant assertion (patron F006/F008).
- [X] T144 [P] TEST RED — même fichier : le marquage comme lue met à jour l'affichage **et** le badge.
- [X] T145 [P] TEST RED — `tests/CIT.Plateforme.Web.Tests/Layout/HeaderBarNotificationsTests.cs` (bUnit) : le badge de la cloche affiche le **compteur exact** de non-lues et retombe à zéro (NOT-03).
- [ ] T146 [P] TEST RED — `tests/CIT.Plateforme.Web.Tests/Pages/Notifications/AdministrationNotificationsTests.cs` (bUnit) : écrans de modèles et de règles ; un utilisateur non habilité **ne peut pas** agir. **NON RÉALISÉE — dette D4** (rapport d'implémentation §20) : les écrans existent et leurs commandes sont testées côté Application, mais aucun test bUnit dédié n'a été écrit.
- [X] T147 [US1] Implémentation GREEN — `src/CIT.Plateforme.Web/Components/Pages/Notifications/CentreNotifications.razor`. **Aucun `DbContext` ni repository dans le Razor, aucun GUID saisi à la main.**
- [X] T148 [US4] Implémentation GREEN — `src/CIT.Plateforme.Web/Components/Layout/HeaderBar.razor` : **câbler la cloche et le badge déjà présents** (`cit-notification-btn`, `cit-notification-badge`) — gestionnaire de clic + compteur. **Ne pas recréer le composant.**
- [X] T149 [P] [US7] Implémentation GREEN — `src/CIT.Plateforme.Web/Components/Pages/Notifications/MesPreferencesNotification.razor`. **FAIT.**
- [X] T150 [P] [US5] Implémentation GREEN — **ÉCART ASSUMÉ** : les écrans d'administration des modèles et des règles ont été **fusionnés en une seule page** `Components/Pages/Notifications/AdministrationNotifications.razor` (deux sections), au lieu des deux fichiers prévus. Les deux contrats C9-06 et C9-07 restent distincts côté Application. Écart d'ergonomie, sans effet sur le périmètre.
- [X] T151 [P] [US5] Implémentation GREEN — voir T150 (page unique `AdministrationNotifications.razor`).
- [X] T152 `src/CIT.Plateforme.Web/Components/Layout/NavMenu.razor` : liens vers le centre, les préférences et l'administration, selon les habilitations.
- [X] T153 **Non-régression F003.5 — UI** : rejouer intégralement `CIT.Plateforme.Web.Tests` (136 tests existants). Tout échec **bloque**.

---

## Phase 15 : Sécurité — cas négatifs (quickstart.md parcours 6)

- [X] T154 [P] TEST — **cas TEST-NOT-01-5** : un utilisateur **non authentifié** accédant au centre ou à un lien de notification est conduit au contrôle d'identité ; **aucun détail du dossier, aucune pièce protégée** n'est exposé (SEC-12).
- [X] T155 [P] TEST — **cas TEST-NOT-01-4** : le lien d'une notification conduit au **ticket** pour un destinataire habilité ; **aucun accès direct à un document**.
- [X] T156 [P] TEST — **cas TEST-NOT-01-8** : ticket devenu inaccessible (habilitation retirée, rattachement modifié) → refus **propre**, **sans divulguer** l'existence, le contenu ni l'appartenance du ticket.
- [X] T157 [P] TEST — **cloisonnement société** de bout en bout (Application + Web) : aucune notification d'une autre société n'apparaît dans la liste ni dans le compteur.
- [X] T158 [P] TEST — **destinataire nommé** : toute notification produite dans les tests a un `DestinataireId` correspondant à un utilisateur réel ; **aucune notification sans destinataire, aucune adressée à une file ou à un rôle** (CLAR-NOT-04).
- [X] T159 [P] TEST — **notes internes jamais exposées au client** : synthèse de T061 et T086, vérifiée au niveau Application **et** Web.
- [X] T160 [P] TEST — **absence de broadcast** : synthèse de T063 — aucun chemin de code ne produit N notifications pour un événement à destinataire unique.
- [X] T199 [P] TEST — **aucune coordonnée arbitraire** (ajout de la revue de cohérence, §14) : ni l'Application ni l'UI n'exposent de point d'entrée permettant de saisir une adresse e-mail ou un numéro de téléphone de destination, ni de désigner un tiers destinataire, ni d'ajouter un destinataire à une notification existante. Le destinataire est **toujours dérivé du contexte métier** par un résolveur (`spec.md` §13, invariant I8).

**Checkpoint Sécurité** : `TEST-NOT-01` **8/8** couverts.

---

## Phase 16 : REC-SCE-14 — volets F009 uniquement

> **Ne pas prétendre rejouer le scénario complet.** Deux volets (recherche guichet, rendez-vous) dépendent de CON-01/02/03 et relèvent de la **Feature 010** (`spec.md` §18.1, invariant I4).

- [ ] T161 TEST E2E bUnit — `tests/CIT.Plateforme.Web.Tests/Pages/Notifications/RecSce14NotificationsE2ETests.cs` : volet *« notifications sur chaque canal configuré »* — sur les canaux **effectivement configurés** (portail + e-mail), sans faire échouer COM-01 en l'absence de canal mobile (PC-16). **NON RÉALISÉE — dette D3** : aucun test E2E dédié. Les volets F009 sont couverts par les tests Application et UI, mais pas rejoués de bout en bout.
- [ ] T162 TEST E2E — même fichier : volet *« …avec lien vers le ticket »*. **NON RÉALISÉE — dette D3.**
- [ ] T163 TEST E2E — même fichier : volet *« contrôle des habilitations »* appliqué aux notifications (client A / client B, agent rôle 1 / agent rôle 2). **NON RÉALISÉE — dette D3.**
- [ ] T164 TEST E2E — même fichier : volet *« journal d'audit »* appliqué aux notifications (émissions, échecs, actes d'administration). **NON RÉALISÉE — dette D3.**
- [ ] T165 **Vérification d'abstention (invariant I4)** — le fichier de test **ne contient aucune** étape de recherche guichet, de prise de rendez-vous, de créneau ni de continuité présentielle, et **aucune numérotation d'étape inventée** (`spec.md` §18.1). **NON RÉALISÉE — dette D3** (l'abstention CON est néanmoins vérifiée par `AucuneLogiqueDeContinuitePresentielle_NEstIntroduiteParFeature009`).

---

## Phase 17 : COM-06 — Relances (Priorité P3 — Could, conditionnel)

**Contrat** : **C9-10** · **Exigence** : COM-06

> Si COM-06 est différée (précédent ENR-04/ENR-05 en F008), cette phase n'est pas exécutée et l'écart est **tracé comme dette**, jamais silencieux.

- [ ] T166 [P] [US8] TEST RED — `tests/CIT.Plateforme.Application.Tests/Notifications/RelancesNotificationTests.cs` : **cas TEST-COM-06-1** — l'événement `RelanceEmise` (**déjà publié par F001**) avec une règle configurée produit une notification de rappel **tracée**. **NON RÉALISÉE — dette D2** : COM-06 (Could) différée, conformément au précédent ENR-04/ENR-05 (F008). La capacité fonctionne dès qu'une règle est configurée pour `RelanceEmise`, mais aucun test ne l'exerce.
- [ ] T167 [P] [US8] TEST RED — même fichier : **cas TEST-COM-06-2** — modifier les canaux → la relance suivante emprunte **les nouveaux canaux**, sans redéploiement. **NON RÉALISÉE — dette D2.**
- [ ] T168 [P] [US8] TEST RED — même fichier : **cas TEST-COM-06-3** — F009 **ne crée aucun second mécanisme de relance** ; il ne modifie ni la cadence, ni le nombre de relances, ni la suggestion de clôture (CYC-04 reste propriété de F001). **NON RÉALISÉE — dette D2.**
- [ ] T169 [P] [US8] TEST RED — même fichier : **cas TEST-COM-06-4** — **sans aucun canal configuré**, le mécanisme CYC-04 **s'exécute normalement** (relance puis suggestion de clôture) ; l'absence de canal **ne le bloque pas**. **NON RÉALISÉE — dette D2.**
- [ ] T170 [US8] Implémentation GREEN — aucune commande nouvelle : semer la `RegleDiffusion` pour `RelanceEmise` **uniquement si COM-06 est retenue au périmètre**. **Vérifier que `RelancesAbsenceReponseUseCase.cs` affiche 0 modification** (invariant I6). **NON RÉALISÉE — dette D2** : aucune règle `RelanceEmise` n'est semée.

**Checkpoint COM-06** : `TEST-COM-06` 4/4 couverts, **ou** dette explicitement tracée.

---

## Phase 18 : Non-régression complète et contrôles finaux

- [X] T171 **Non-régression F001** — Tickets, Workflow, Messagerie, SLA, Alertes, Supervision, Affectation, Audit : suites intégrales vertes.
- [X] T172 **Non-régression F002** — Comptes, Sociétés, Habilitations, Cloisonnement : suites intégrales vertes.
- [X] T173 **Non-régression F003** — Billing Office IMPORT (touchée par T126) : suite intégrale verte.
- [X] T174 **Non-régression F004** — Billing Office EXPORT (touchée par T127) : suite intégrale verte.
- [X] T175 **Non-régression F005** — Simulation Proforma : suite intégrale verte.
- [X] T176 **Non-régression F006** — Customer Complaint : suite intégrale verte.
- [X] T177 **Non-régression F008** — Enrôlement Transporteurs : suite intégrale verte.
- [X] T178 **Suite complète** : `dotnet test` → **608 tests existants + nouveaux, 0 échec**. Aucun test existant supprimé, ignoré ni affaibli.
- [X] T179 **Build final** : `dotnet build --no-incremental` → **0 avertissement, 0 erreur**.
- [X] T180 **`dotnet ef migrations has-pending-model-changes`** → *« No changes have been made to the model since the last migration. »* Tout écart **bloque la clôture**.
- [X] T181 **Contrôle de migration additive (invariant I5)** — relire le fichier de migration : **aucun `DropTable`, `DropColumn`, `DropIndex`, `DropForeignKey`**, aucun `AlterColumn` réduisant une contrainte, aucun `Sql(...)` modifiant des données existantes.
- [X] T182 **Scan des dépendances interdites (invariant I1)** — vérifier qu'aucun `.csproj` n'a gagné de `PackageReference`, et rechercher dans `src/` : `Twilio`, `SendGrid`, `SmtpClient`, `MailKit`, `Azure.Communication`, `Infobip`, `Vonage`, `Nexmo`, `MailGun`, `Amazon.SimpleEmail`, `WhatsApp`, `graph.facebook.com`. **Zéro occurrence hors commentaires d'interdiction.**
- [X] T183 **Scan des valeurs de retry (invariant I2)** — aucun littéral numérique de tentatives, délai ou cadence dans le code F009 ; aucun `backoff`, aucun `fallback` inter-canaux.
- [X] T184 **Contrôle AVE / Feature 007** — aucune référence à AVE-01→04, aucun workflow d'avoir ou d'exonération, aucune notification de décision Avoir/Exonération. F007 reste **BLOCKED** et **non absorbée** (`spec.md` §24).
- [X] T185 **Contrôle CON / Feature 010 (invariant I4)** — recherche dans `src/` et `specs/009-*/` : aucune entité, commande, page ni test de rendez-vous, créneau, agenda, recherche guichet, file d'attente physique ou continuité présentielle. **CON-01→04 et ERR-04 : 0/5 absorbées.**
- [X] T186 **Contrôle de non-recomptage (invariant I7)** — le rapport d'implémentation revendique **exactement 9 exigences** (COM-01→06, NOT-01→03). **MSG-04, ALE-01→03, ERR-02, SEC-12 et ACR-01 ne sont pas comptées.**
- [X] T187 **Contrôle des producteurs d'événements (invariant I6)** — `git diff --stat src/` : parmi les **19 fichiers** publiant un `EvenementSocle`, **exactement 2** sont modifiés (`EditerFactureCommand.cs`, `EditerFactureExportCommand.cs`), **+1 ligne chacun**. Les 17 autres : **0 modification**.
- [ ] T188 **Rejeu manuel** de `quickstart.md` parcours 1 à 6 (7 si COM-06 retenue), plus un parcours F003 et un parcours F006 pour confirmer l'absence de régression fonctionnelle. **NON RÉALISÉE** : le rejeu manuel des parcours de `quickstart.md` suppose une exécution interactive de l'application, hors de portée de cette session automatisée. Les parcours sont couverts par les suites automatisées, sans substitution au rejeu manuel avant recette.
- [X] T200 **Scan secrets et configuration** (ajout de la revue de cohérence, §22) : aucun secret, aucune chaîne de connexion, aucune clé d'API, aucun identifiant de compte n'est introduit dans `appsettings*.json`, dans le code ou dans les tests. La section de configuration de F009 ne contient **que** les paramètres de reprise et les intervalles de scrutation — **aucune valeur de fournisseur** (invariants I1, I2). Vérifier également qu'aucune valeur par défaut n'a été glissée dans `appsettings.json` pour contourner l'échec de démarrage exigé par T105.
- [X] T201 **Garantie I9 vérifiée dans le code livré** (ajout de la revue de cohérence, §7) : le terme **`exactly-once` n'apparaît nulle part** — ni en commentaire, ni en nom de symbole, ni en documentation. La garantie documentée est **at-least-once au traitement, effectively-once à l'effet observable**.
- [X] T202 **Sélectivité Git avant commit** (ajout de la revue de cohérence, §23) : l'arbre de travail contient, **avant F009**, des modifications non commitées étrangères à cette feature — refonte UI préexistante (nombreux `.razor`), écrans de connexion / double authentification, et divers ajustements Applicatifs et Infrastructure. **Le commit F009 doit être sélectif** : `git status --short` puis `git add` **fichier par fichier**, en n'incluant que les fichiers listés dans `plan.md` §Structure plus les 2 fichiers IMP-11. **Ne jamais utiliser `git add -A` ni `git add .`.** Produire dans le rapport d'implémentation la liste exacte des fichiers commités et la liste de ceux volontairement laissés hors du commit.

---

## Traçabilité

### Contrats C9-01 → C9-11

| Contrat | Exigence | Phase | Tâches | TEST-SPEC |
|:---|:---|:---:|:---|:---|
| **C9-01** Distribuer | COM-01, NOT-02 | 3 | T053→T065, **T189→T193** (curseur, I9) | `TEST-NOT-02` 1→4 |
| **C9-02** Consulter | NOT-01 | 4 | T070→T074, T079 | `TEST-NOT-01` 1, 5, 6, 7 |
| **C9-03** Compter | NOT-03 | 4 | T075, T080 | `TEST-NOT-01` 1, 2 |
| **C9-04** Marquer lue | NOT-01 | 4 | T076→T078, T081, T082 | `TEST-NOT-01` 2, 3, 6 |
| **C9-05** Accès au ticket | COM-02 + SEC-12 | 3, 15 | T066→T069, T154→T156 | `TEST-NOT-01` 4, 5, 8 |
| **C9-06** Modèles | COM-03 | 8 | T108→T111, T114 | `TEST-NOT-02` 5, 6 |
| **C9-07** Règles | NOT-02 | 8 | T108→T113, T115 | `TEST-NOT-02` 1→6 |
| **C9-08** Reprise | ERR-02 | 7 | T099→T107, **T197** (arrêt) | `TEST-ERR-02` 1→6 |
| **C9-09** Préférences | COM-05 | 9 | T116→T120 | `TEST-NOT-02` 7 |
| **C9-10** Relances | COM-06 | 17 | T166→T170 | `TEST-COM-06` 1→4 |
| **C9-11** Mention `@Nom` | NOT-01 / MSG-04 (F001) | 5 | T083→T092, **T194→T196** (cas limites) | `TEST-MSG-04` 1→8 |

**11/11 contrats couverts.**

### TEST-SPEC — 5 scénarios, 34 cas

| TEST-SPEC | Cas | Tâches couvrantes | Total |
|:---|:---|:---|:---:|
| **`TEST-NOT-01`** | 1 → 8 | T070, T075 (1) · T076 (2) · T077 (3) · T155 (4) · T154 (5) · T071, T078 (6) · T072 (7) · T156 (8) | **8** |
| **`TEST-NOT-02`** | 1 → 8 | T108 (1) · T109 (2) · T110 (3) · T111 (4) · T112 (5) · T113 (6) · T116 (7) · T060 (8) | **8** |
| **`TEST-MSG-04`** | 1 → 8 | T083 (1) · T084 (2) · T085 (3) · T086 (4) · T087 (5) · T088 (6) · T089 (7) · T094 (8) | **8** |
| **`TEST-ERR-02`** | 1 → 6 | T099 (1) · T100 (2) · T101 (3) · T102 (4) · T103 (5) · T104 (6) | **6** |
| **`TEST-COM-06`** | 1 → 4 | T166 (1) · T167 (2) · T168 (3) · T169 (4) | **4** |
| | | **TOTAL** | **34** |

### Exigences → User Stories → Phases

| Exigence | Priorité | US | Phase | Comptée dans les 9 ? |
|:---|:---:|:---|:---:|:---:|
| COM-01 | Must | US2 | 3 | ✅ |
| COM-02 | Must | US2, US3 | 3, 15 | ✅ |
| COM-03 | Must | US5 | 8 | ✅ |
| COM-04 | Must | US6 | 10 (vérification) | ✅ |
| COM-05 | Should | US7 | 9 | ✅ |
| COM-06 | Could | US8 | 17 (conditionnel) | ✅ |
| NOT-01 | Must | US1, US4, US9 | 4, 5 | ✅ |
| NOT-02 | Must | US5 | 8 | ✅ |
| NOT-03 | Should | US1, US4 | 4, 14 | ✅ |
| **MSG-04** | Should | **US9** | **5** | ❌ **propriété F001 — raccordement seul** |
| **ALE-01→03** | Must | — | **6** (abstention) | ❌ **propriété F001 — point d'intégration seul** |
| **ERR-02** | Must | — | 7 | ❌ contrainte transverse |
| **SEC-12** | Must | US3 | 3, 15 | ❌ contrainte transverse |
| **ACR-01** | Must | — | 4 (T082, abstention) | ❌ déjà livrée en F003.5 |
| **IMP-11** | Must | — | 11 | ❌ exigence F003/F004 — événement manquant fourni |

**Total revendiqué par F009 : 9 exigences** (6 Must, 2 Should, 1 Could) — inchangé.

### Objets de migration → justification

| Objet | Justification | Tâche |
|:---|:---|:---|
| Table `Notifications` | CDC §20 (*destinataire*, *modèle*) + NOT-01 (état Lu/Non lu) | T028 |
| Index unique `(EvenementSourceId, DestinataireId)` | Idempotence de distribution (RSQ-02) | T028 |
| Table `EnvoisNotification` | CDC §20 (*canal*, *statut d'envoi*) + ERR-02 (isolation par canal) | T029 |
| Index `(Statut, DerniereTentativeUtc)` | Sélection bornée des reprises (Principe XIII) | T029 |
| Table `ModelesNotification` | COM-03 (modèles paramétrables par canal) | T030 |
| Table `ReglesDiffusion` | NOT-02 (règles par événement et par canal) + CLAR-COM-03 | T031 |
| Table `PreferencesCanalUtilisateur` | COM-05 (préférences par canal) | T032 |
| Colonne `JournalEvenements.DistribueLeUtc` | Curseur de distribution — **nullable, additive** (research.md D2) | T033 |

**5 tables, 1 colonne, 5 index. Aucune suppression.**

**Contrôle de nécessité (revue de cohérence, §5)** — aucun objet n'existe pour simple commodité technique :

| Objet | Exigence | Contrat | Test | Tâche migration |
|:---|:---|:---:|:---|:---:|
| `Notifications` | CDC §20 · NOT-01 | C9-01, C9-02, C9-04 | T005→T010, T070, T076 | T028 |
| Index unique `(EvenementSourceId, DestinataireId)` | RSQ-02 · garantie **I9** | C9-01 | T037, T057, T191, T196 | T028 |
| `EnvoisNotification` | CDC §20 · ERR-02 | C9-01, C9-08 | T011→T015, T099→T104 | T029 |
| Index `(Statut, DerniereTentativeUtc)` | ERR-02 · Principe XIII | C9-08 | T102, T197 | T029 |
| `ModelesNotification` | COM-03 · CDC §20 (*modèle*) | C9-06 | T016, T110 | T030 |
| `ReglesDiffusion` | NOT-02 · CLAR-COM-03 | C9-07, C9-01 | T017, T108, T111 | T031 |
| `PreferencesCanalUtilisateur` | COM-05 | C9-09 | T018, T116, T117 | T032 |
| Colonne `DistribueLeUtc` | Curseur de distribution · **I9** | C9-01 | T023, T189, T190, T191 | T033 |

**Verdict** : chaque table, chaque index et la colonne portent **au moins une exigence CDC ou un invariant nommé**, **au moins un contrat** et **au moins un test**. Aucun objet de pure commodité.

---

## Dépendances et ordre d'exécution

```
Phase 1 (baseline) ─► Phase 2 (fondations, BLOQUANT)
                            │
        ┌───────────────────┼───────────────────┬──────────────┐
        ▼                   ▼                   ▼              ▼
   Phase 3 (US2/US3)   Phase 11 (IMP-11)   Phase 6 (ALE-03)  Phase 13 (seed)
     MVP — moteur       indépendante         abstention        │
        │                                                      │
        ├──► Phase 4 (US1/US4) ──┬──► Phase 14 (UI) ──► Phase 16 (REC-SCE-14)
        ├──► Phase 5 (US9) ──────┤
        ├──► Phase 7 (ERR-02) ───┤
        ├──► Phase 8 (US5) ──────┤
        ├──► Phase 9 (US7) ──────┘
        ├──► Phase 10 (US6, vérification)
        └──► Phase 17 (COM-06, conditionnel)
                            │
                            ▼
                  Phase 12 (traitements de fond)
                            │
                            ▼
                  Phase 15 (sécurité) ──► Phase 18 (non-régression et contrôles finaux)
```

**Parallélisme** : les tâches `[P]` d'une même phase touchent des fichiers distincts. Après la Phase 3, les Phases 4, 5, 7, 8, 9 sont largement parallélisables ; la Phase 6 (abstention) et la Phase 11 (IMP-11) sont indépendantes du moteur et peuvent démarrer dès la fin de la Phase 2.

---

## Stratégie d'implémentation

### MVP — Phases 1 à 4

Fondations + moteur d'émission (US2/US3) + centre de notifications (US1/US4). À ce stade, COM-01, COM-02, NOT-01 et NOT-03 sont démontrables, avec cloisonnement et lien sécurisé. **STOP et VALIDER** avant d'aller plus loin.

### Incréments suivants

1. **Phase 5** — raccordement MSG-04 (valeur immédiate pour les agents, coût quasi nul).
2. **Phase 6** — abstention ALE-03 démontrée (aucun code, uniquement des tests).
3. **Phases 7 et 8** — ERR-02 puis administration : la feature devient exploitable sans développement.
4. **Phases 9 à 14** — préférences, IMP-11, traitements de fond, seed, UI.
5. **Phases 15 à 18** — sécurité, REC-SCE-14, COM-06 conditionnel, non-régression.

### Points d'arrêt

- **Phase 2** : si la migration générée en T034 contient la moindre opération destructive → **arrêter**, l'invariant I5 est violé.
- **Phase 5** : si `MentionnerCollaborateurCommand.cs` doit être modifiée → **arrêter**, la conception a dévié de research.md D5.
- **Phase 7** : si une valeur de reprise doit être écrite en dur pour faire passer un test → **arrêter**, CLAR-NOT-02 n'est pas résolue.
- **Phase 11** : si la non-régression F003/F004 échoue → **arrêter et revenir**, l'ajout doit être strictement additif.

---

## Notes

- `[P]` = fichiers distincts, aucune dépendance sur une tâche incomplète.
- Chaque paire **RED → GREEN** est indissociable : le test doit **échouer** avant l'implémentation.
- Les tâches marquées **« Vérification d'abstention »** (T052, T065, T082, T091, T093→T095, T097, T107, T123, T132, T137, T165, T182→T187, T198→T203) démontrent ce que F009 **ne fait pas** — elles sont aussi contraignantes que les autres.
- **T002 et T096 sont des sépultures** : fusionnées par la revue de cohérence, leur identifiant est conservé pour préserver la numérotation et l'auditabilité. **Elles ne sont pas à exécuter.**
- **T189→T203 sont des ajouts de la revue de cohérence** du 1er septembre 2026. Chacune indique sa phase de rattachement et le point de revue qui l'a motivée. Leur numérotation suit la dernière tâche existante afin de ne pas renuméroter les 188 tâches d'origine ni invalider les références croisées ; **l'ordre d'exécution est celui de leur phase**, pas celui de leur identifiant.
- **`/speckit.implement` n'est pas lancé par ce document. Aucune migration n'est créée. Aucun code produit n'est modifié.**
