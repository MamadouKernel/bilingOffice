# Spécification Fonctionnelle Clarifiée — Feature 007
## Module Avoir & Exonération

> **Statut** : **SPÉCIFICATION CLARIFIÉE & VALIDÉE**  
> **Date** : 1er septembre 2026  
> **Source de référence** : Cahier des Charges Fonctionnel CIT v1.0 (§11.6, §4, §25), `docs/15-preflight-revise-feature-007.md`  
> **Discipline** : Strict respect du CDC. Découplage clair entre l'orchestration portail et la chaîne interne Finance hors portail. Zéro code durci pour les étapes/intervenants.

---

## 1. Contexte & Périmètre

### 1.1 Contexte Métier
Le Module 1 du Cahier des Charges Fonctionnel de Côte d'Ivoire Terminal (Billing Office virtuel) comporte 4 volets :
1. Parcours Client IMPORT (§11.1 - §11.2)
2. Parcours Client EXPORT (§11.3)
3. Simulation de Proforma (§11.5)
4. **Avoir et Exonération (§11.6)**

Les demandes d'avoirs (demandes de remboursement, rectification de facturation, contestations tarifaires) et les demandes d'exonération (application d'exonération fiscale, douanière, franchise diplomatique ou conventionnelle) sont dématérialisées sur la plateforme CIT afin de supprimer les flux physiques dispersés et garantir la traçabilité complète de l'instruction jusqu'à la décision finale.

### 1.2 Frontière Portail / Hors-Portail (Dissociation Fondamentale — CLAR-AVE-01)
Conformément au principe d'autonomie de la plateforme (CDC §4) et aux clarifications de cadrage :
- **Dans le portail CIT (Feature 007)** :
  - Soumission en ligne par le client d'une demande d'Avoir ou d'Exonération (`AVE-01`).
  - Dépôt et sécurisation des pièces justificatives (`AVE-01`, `SOC-07`).
  - Génération d'un ticket unique rattaché à un dossier (`SOC-01`).
  - Liaison optionnelle vers un ticket parent de facturation ou de parcours (`AVE-04`, `SOC-10`).
  - Gestion des cycles de vie configurables Avoir vs Exonération (`AVE-02`).
  - Prise en charge, échanges, demandes de compléments documentaires (`AttenteClient`, `ACR-01`).
  - Enregistrement de la décision finale : **ACCORD** (avec document joint) ou **REJET MOTIVÉ** (motif obligatoire + document éventuel) (`AVE-03`).
  - Émission d'événements socles pour notification multicanale client via F009 (`AVE-03`, `COM-01`).
  - Clôture et archivage du ticket.
- **Hors du portail CIT (Direction Financière & Systèmes Métiers)** :
  - Les contrôles comptables approfondis, les écritures d'avoir dans l'ERP (IRIS/autre), les signatures hiérarchiques et les décharges financières physiques sont exécutés en dehors de la plateforme.
  - La plateforme ne comporte aucun moteur ERP interne ni générateur automatisé d'écritures comptables d'avoir.

---

## 2. Exigences Fonctionnelles Traitées

| Exigence | Priorité CDC | Résumé Fonctionnel | Couverture dans F007 |
| :--- | :---: | :--- | :--- |
| **`AVE-01`** | **Must** | Le client peut soumettre une demande d'avoir ou une demande d'exonération, avec les justificatifs exigés ; chaque demande génère un ticket. | Formulaire de soumission unique avec sélection du `TypeDemande` (`Avoir` / `Exoneration`), téléversement de liasse justificative, création du ticket et du dossier. |
| **`AVE-02`** | **Must** | Chaque type de demande suit son propre workflow de validation, avec étapes et intervenants paramétrables. | Moteur d'étapes et de transitions autorisées distinctes par type de demande, files de traitement configurables, permissions RBAC dédiées. |
| **`AVE-03`** | **Must** | La décision (accord, rejet motivé) et les documents associés sont déposés dans le ticket et notifiés au client. | Enregistrement de la décision d'accord ou de rejet avec motif obligatoire, dépôt du document justificatif de décision, émission d'événement `EvenementSocle` vers F009. |
| **`AVE-04`** | **Should** | Une demande d'avoir/exonération peut être liée au ticket de parcours client concerné. | Sélecteur optionnel d'un ticket existant (Billing Import, Export ou Réclamation) appartenant à la même société, tracé via `LiaisonTicket`. |

---

## 3. Rôles & Permissions

Conformément à la résolution de **PC-03** et **CLAR-AVE-11**, les intervenants ne sont pas des postes figés dans le marbre organisationnel mais des habilitations fonctionnelles paramétrables :

| Rôle / Entité | Permissions Requises | Responsabilités Métier |
| :--- | :--- | :--- |
| **Client / Déclarant** | Authentifié (`NatureUtilisateur.Client`) | Soumettre une demande d'avoir ou d'exonération, joindre des justificatifs, lier un ticket parent, répondre aux demandes de complément, consulter la décision et télécharger les documents finaux. |
| **Agent Instructeur Avoir/Exo** | `Permission.AvoirExonerationInstruire` | Consulter la file des demandes, prendre en charge un ticket, vérifier la liasse, demander des compléments d'information, consigner des notes internes. |
| **Agent Décideur Avoir/Exo** | `Permission.AvoirExonerationDecider` | Enregistrer la décision d'accord ou de rejet motivé, déposer les documents formels de décision (note d'avoir émise par Finance, attestation visée), clôturer le ticket. |
| **Superviseur / Administrateur** | `Permission.AdministrationFiles`, `Permission.SupervisionConsulter` | Consulter la charge des files Avoir/Exo, réaffecter les tickets, paramétrer les catégories de pièces et les étapes. |

---

## 4. User Stories & Critères d'Acceptation

### US1 — Soumission d'une Demande d'Avoir ou d'Exonération (Client)
**En tant que** client de la plateforme CIT,  
**Je souhaite** créer une demande d'avoir ou une demande d'exonération en téléversant les pièces justificatives et en liant éventuellement un ticket de facturation existant,  
**Afin de** faire instruire ma demande par les services de CIT sans avoir à me déplacer.

#### Critères d'acceptation :
1. Le client accède à l'écran de soumission `/billing-office/avoir-exoneration/demande`.
2. Le client sélectionne obligatoirement le type de demande : **Avoir** ou **Exonération** (`CLAR-AVE-02`).
3. Le client renseigne les données descriptives (`CLAR-AVE-03`) :
   - Objet / Motif de la demande (obligatoire, max 200 car.)
   - Description détaillée / Explications (obligatoire, max 2000 car.)
   - Montant contesté / estimé en XOF (optionnel)
   - Numéro de référence métier (BL, Booking, Numéro de Facture contestée si applicable, optionnel).
4. Le client peut sélectionner un ticket parent parmi la liste de ses tickets existants (tous modules confondus appartenant à sa société) pour lier sa demande (`AVE-04`, `CLAR-AVE-09`).
5. Le client téléverse un ou plusieurs fichiers justificatifs (format PDF/JPEG/PNG, limite de taille unitaire 10 Mo, `CLAR-AVE-04`).
6. À la validation :
   - Un `Dossier` et un `Ticket` sont créés avec le module `"AvoirExoneration"` et le type approprié (`"Avoir"` ou `"Exoneration"`).
   - Un numéro de ticket unique formaté `SOC-01` est généré (ex: `AVO-2026-00001` ou `EXO-2026-00001`).
   - Les documents sont persistés et rattachés au ticket.
   - Si un ticket parent est sélectionné, un enregistrement `LiaisonTicket` est créé.
   - Un événement `EvenementSocle` (`DemandeAvoirSoumise` ou `DemandeExonerationSoumise`) est publié (`CLAR-AVE-12`).
   - Le ticket est positionné au statut initial `Cree` dans la file d'instruction.

---

### US2 — Instruction et Demande de Complément (Agent Instructeur)
**En tant qu'** agent instructeur habilité (`Permission.AvoirExonerationInstruire`),  
**Je souhaite** consulter les demandes en attente, analyser les pièces justificatives et demander des précisions au client si nécessaire,  
**Afin de** constituer un dossier complet avant transmission à la décision financière.

#### Critères d'acceptation :
1. L'agent accède à la file `/billing-office/avoir-exoneration/file`.
2. La file affiche les tickets en attente avec filtres par type (Avoir / Exonération), statut, date et société.
3. L'agent clique sur « Prendre en charge » : le ticket passe au statut `EnCours` et l'agent devient responsable de l'instruction.
4. L'agent consulte la liasse documentaire et l'historique complet.
5. Si le dossier est incomplet (`CLAR-AVE-10`) :
   - L'agent utilise l'action « Demander un complément » en indiquant le motif textuel.
   - Le ticket passe au statut `EnAttenteClient`.
   - Une action requise client (`ACR-01`) est générée sur le tableau de bord du client.
   - Le client est notifié et peut déposer les documents manquants ou répondre par message.
   - Dès réponse du client, le ticket repasse automatiquement au statut `EnCours`.
6. L'agent peut ajouter des notes internes (visibles uniquement par les agents de CIT).

---

### US3 — Enregistrement de la Décision Finale (Agent Décideur)
**En tant qu'** agent habilité à décider (`Permission.AvoirExonerationDecider`),  
**Je souhaite** enregistrer la décision finale (Accord ou Rejet motivé) issue des vérifications financières et y adjoindre le document officiel,  
**Afin de** notifier formellement le client et clôturer l'instruction.

#### Critères d'acceptation :
1. **Cas ACCORD (`CLAR-AVE-06`)** :
   - L'agent sélectionne « Prononcer un Accord ».
   - L'agent saisit un commentaire de validation et le montant final accordé (si Avoir).
   - L'agent dépose optionnellement ou obligatoirement le document justificatif formel (ex: note de crédit, attestation d'exonération visée, `CLAR-AVE-08`).
   - Le ticket passe au statut `Valide` (affiché *Accordé*) puis `Cloture`.
   - L'événement `AvoirAccorde` ou `ExonerationAccordee` est publié pour notification client.
2. **Cas REJET (`CLAR-AVE-07`)** :
   - L'agent sélectionne « Rejeter la demande ».
   - L'agent renseigne **obligatoirement** un motif de rejet circonstancié (`AVE-03`).
   - L'agent peut joindre un document explicatif (optionnel).
   - Le ticket passe au statut `Rejete` puis `Cloture`.
   - L'événement `AvoirRejete` ou `ExonerationRejetee` est publié pour notification client.
3. L'historique et le journal d'audit (`JournalAudit`) consignent l'identité de l'agent décideur, la date/heure UTC exacte, la décision et le motif.

---

### US4 — Suivi et Consultation Client
**En tant que** client émetteur ou collaborateur de la même société,  
**Je souhaite** suivre en temps réel l'avancement de ma demande, consulter la décision finale et télécharger les pièces,  
**Afin de** connaître l'issue de mon dossier en toute transparence.

#### Critères d'acceptation :
1. Le client retrouve ses demandes dans son espace dédié `/billing-office/avoir-exoneration/mes-demandes` et dans `/client/demandes`.
2. Le détail du ticket affiche le statut actuel, la chronologie des étapes, les échanges et les pièces déposées.
3. Lorsque la décision est prononcée :
   - Le badge de statut affiche clairement *« Accordé »* (vert) ou *« Rejeté »* (rouge).
   - En cas de rejet, le motif rédigé par l'agent est affiché de façon proéminente.
   - Le document de décision déposé par CIT est téléchargeable dans l'onglet *Documents*.

---

## 5. Matrice des Clarifications Décisionnelles (CLAR-AVE-01 à CLAR-AVE-12)

| Code Clarification | Sujet / Thème | Décision Démontrable | Impact Spec / Plan |
| :--- | :--- | :--- | :--- |
| **`CLAR-AVE-01`** | **Chaîne interne Finance** | Traitement interne (écritures comptables, visas hiérarchiques) réalisé **HORS APPLICATION**. Le portail orchestre uniquement le suivi, les documents et la décision. | **RÉSOLU** |
| **`CLAR-AVE-02`** | **Typage des demandes** | Exactement 2 types : `Avoir` et `Exoneration`. Aucun 3ème type non sourcé. | **RÉSOLU** |
| **`CLAR-AVE-03`** | **Données formulaire soumission** | Formulaire unifié avec `TypeDemande`, `Objet` (obligatoire), `Description` (obligatoire), `MontantEstime` (optionnel), `ReferenceMetier` (optionnel), justificatifs et liaison ticket. | **RÉSOLU** |
| **`CLAR-AVE-04`** | **Paramétrabilité documentaire** | Mécanisme d'upload standard F001 (PDF/JPEG/PNG <= 10 Mo). Aucune liste documentaire n'est durcie comme règle contractuelle. | **RÉSOLU** |
| **`CLAR-AVE-05`** | **Workflow et statuts portail** | Cycle standard F001 : `Cree` -> `EnCours` -> `EnAttenteClient` -> `Valide` (Accordé) / `Rejete` -> `Cloture`. Les libellés d'affichage sont personnalisés sans altérer l'enum de base. | **RÉSOLU** |
| **`CLAR-AVE-06`** | **Enregistrement Accord** | Décision favorable par agent habilité, dépôt de document de décision, notification client, clôture immédiate. | **RÉSOLU** |
| **`CLAR-AVE-07`** | **Enregistrement Rejet** | Motif circonstancié textuel obligatoire, rejet propre si motif vide, dépôt éventuel de pièce, notification client, clôture immédiate. | **RÉSOLU** |
| **`CLAR-AVE-08`** | **Documents de décision** | Déposés manuellement par l'agent décideur dans le ticket. Zéro générateur automatique de note d'avoir comptable. | **RÉSOLU** |
| **`CLAR-AVE-09`** | **Liaison ticket (AVE-04)** | Réutilisation directe de `LiaisonTicket` (F001 `SOC-10`). Cloisonnement strict à la même société. Aucun impact sur le ticket parent. | **RÉSOLU** |
| **`CLAR-AVE-10`** | **Demandes de compléments** | Réutilisation directe de `AttenteClientCommand`, `Message` et `ACR-01`. | **RÉSOLU** |
| **`CLAR-AVE-11`** | **Habilitations RBAC** | `Permission.AvoirExonerationInstruire` et `Permission.AvoirExonerationDecider`. Aucune hiérarchie d'acteurs inventée. | **RÉSOLU** |
| **`CLAR-AVE-12`** | **Événements F009** | Émission de `DemandeAvoirSoumise`, `DemandeExonerationSoumise`, `AvoirAccorde`, `AvoirRejete`, `ExonerationAccordee`, `ExonerationRejetee`, `AvoirComplementDemande`. Zéro canal direct. | **RÉSOLU** |

---

## 6. Cycles de Vie des Tickets Avoir & Exonération

```
 [Client Soumet]
       │
       ▼
    [ Cree ] ──────────────────────────┐
       │ (Prise en charge agent)       │ (Rejet immédiat)
       ▼                               │
   [ EnCours ] ◄─────────────────┐     │
       │                         │     │
       ├─► (Demande complément)  │     │
       │   [ EnAttenteClient ] ──┘     │
       │                               │
       ├─► (Décision Accord)           │
       │   [ Valide (Accordé) ]        │
       │        │                      │
       │        ▼                      │
       │   [ Cloture ]                 │
       │                               │
       └─► (Décision Rejet)            │
           [ Rejete ] ◄────────────────┘
                │
                ▼
           [ Cloture ]
```

---

## 7. Sécurité, Cloisonnement & Anti-IDOR

1. **Isolation Multi-Société (SEC-01)** : Un client ne peut voir que les demandes Avoir/Exo créées par sa société. Toute tentative d'accès à un ticket ou document d'un tiers lève `AccesRefuseException`.
2. **Dérivation d'Identité Serveur** : Toutes les commandes utilisent l'identifiant extrait du contexte authentifié (`IContexteUtilisateurWeb`), aucun Guid demandeur n'est accepté depuis le navigateur.
3. **Contrôle RBAC sur la Décision** : Seuls les agents porteurs de `Permission.AvoirExonerationDecider` peuvent enregistrer un accord ou un rejet.
4. **Protection Documentaire** : Les pièces jointes et documents de décision sont scannés, protégés contre l'écrasement et téléchargeables uniquement par les ayants droit.

---

## 8. Cas d'Usage et Contrats Applicatifs (C7-01 à C7-08)

| Contrat | Type | Description | Permissions |
| :--- | :---: | :--- | :--- |
| **`C7-01`** | Command | `SoumettreDemandeAvoirCommand` / `SoumettreDemandeExonerationCommand` | Client Authentifié |
| **`C7-02`** | Query | `ListerMesDemandesAvoirExoQuery` | Client Authentifié (cloisonné société) |
| **`C7-03`** | Query | `ConsulterFileAvoirExonerationQuery` | `Permission.AvoirExonerationInstruire` |
| **`C7-04`** | Query | `ObtenirDetailDemandeAvoirExoQuery` | Propriétaire ou `Permission.AvoirExonerationInstruire` |
| **`C7-05`** | Command | `PrendreEnChargeAvoirExoCommand` | `Permission.AvoirExonerationInstruire` |
| **`C7-06`** | Command | `DemanderComplementAvoirExoCommand` | `Permission.AvoirExonerationInstruire` |
| **`C7-07`** | Command | `EnregistrerDecisionAvoirExoCommand` (Accord / Rejet motivé) | `Permission.AvoirExonerationDecider` |
| **`C7-08`** | Command | `LierTicketParcoursCommand` (AVE-04) | Propriétaire ou `Permission.AvoirExonerationInstruire` |

---

## 9. Stratégie de Recette & Scénarios TEST-SPEC

- **`AVE-SPEC-01`** : Soumission nominale d'une demande d'Avoir avec justificatifs -> Ticket créé avec préfixe et statut `Cree` (`AVE-01`).
- **`AVE-SPEC-02`** : Soumission nominale d'une demande d'Exonération avec attestation fiscale (`AVE-01`).
- **`AVE-SPEC-03`** : Liaison optionnelle à un ticket Billing Import existant (`AVE-04`).
- **`AVE-SPEC-04`** : Prise en charge par l'agent et consultation de la liasse (`AVE-02`).
- **`AVE-SPEC-05`** : Demande de complément d'information et bascule en attente client (`ACR-01`).
- **`AVE-SPEC-06`** : Fourniture du complément par le client et reprise de l'instruction.
- **`AVE-SPEC-07`** : Enregistrement d'un accord avec dépôt de document de décision (`AVE-03`).
- **`AVE-SPEC-08`** : Enregistrement d'un rejet avec motif obligatoire -> Rejet si motif vide (`AVE-03`).
- **`AVE-SPEC-09`** : Émission et vérification des événements de notification pour F009.
- **`AVE-SPEC-10`** : Cloisonnement strict multi-société et rejet des tentatives IDOR (`SEC-01`).
