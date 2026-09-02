# PREFLIGHT RÉVISÉ — FEATURE 007 (Avoir & Exonération)

> **Document** : `docs/15-preflight-revise-feature-007.md`  
> **Date** : 1er septembre 2026  
> **Nature** : Preflight d'analyse et de cadrage fonctionnel révisé. Aucun `specs/007-*` créé, aucun code produit, aucune migration, aucun commit.  
> **Objectif** : Réévaluer l'éligibilité de la Feature 007 à la spécification (/speckit.specify) à la lumière du périmètre réel du CDC et de la frontière portail/hors-portail.

---

## 1. Contexte Projet & Levée de l'Objection Historique

### État Certifié du Projet
- **Features clôturées** : F001, F002, F003, F004, F005, F006, F008, F009, F010.
- **Baseline technique** : 777/777 tests verts (Domain: 139, Application: 411, Infrastructure: 82, Web: 145), 0 avertissement de build, 0 pending model changes.
- **Couverture CDC certifiée** : **148 / 188 exigences conformes (≈ 78,7 %)**.

### Réévaluation de l'Ancien Blocage (PC-03 / PC-15)
L'analyse historique (`docs/11-preflight-feature-007.md`) avait classé la Feature 007 en *NO-GO* sous l'hypothèse que la plateforme devait modéliser informatiquement l'intégralité de la chaîne décisionnelle et hiérarchique interne de la Direction Financière de CIT.

Une relecture rigoureuse du CDC (§1, §4, §11.6, §23, §25) infirme cette hypothèse :
1. **Principe de Fonctionnement Autonome (CDC §4)** : La plateforme ne comporte aucune interface automatisée avec les ERP comptables (IRIS, etc.) et ne remplace pas les processus physiques d'approbation financière interne.
2. **Périmètre du Portail (CDC §11.6 & AVE-01..04)** : La plateforme assure l'accueil de la demande du client, le dépôt des pièces justificatives, l'assignation dans les files d'instruction, la traçabilité des échanges/compléments, l'enregistrement de la décision finale (accord ou rejet motivé avec document justificatif) et la notification proactive du client.
3. **Traitement Interne Finance** : Le circuit de visa physique/comptable s'opère hors portail par les acteurs habilités, puis l'agent consigne le résultat dans le ticket.

En conséquence, **PC-03 et PC-15 ne constituent pas un obstacle à la spécification et à l'implémentation** d'un module de gestion des demandes d'avoirs et d'exonérations paramétrable et autonome.

---

## 2. Reconstitution Exacte des Exigences AVE

| ID | Priorité | Texte Exact du CDC | Acteur | Déclencheur | Résultat Attendu | Dépendances | Ambiguïté | Statut Preflight |
| :--- | :---: | :--- | :--- | :--- | :--- | :--- | :--- | :---: |
| **`AVE-01`** | **Must** | *« Le client peut soumettre une demande d'avoir ou une demande d'exonération, avec les justificatifs exigés ; chaque demande génère un ticket. »* | Client | Soumission formulaire Avoir ou Exonération | Création d'un Ticket avec Dossier, typage `Avoir` ou `Exoneration`, dépôt des pièces, horodatage `SOC-01`. | `SOC-01` | Liste exacte des pièces à rendre configurable | **PRÊT** |
| **`AVE-02`** | **Must** | *« Chaque type de demande suit son propre workflow de validation, avec étapes et intervenants paramétrables. »* | Admin / Agent | Évolution du ticket | Cycles de vie distincts Avoir vs Exonération, étapes et files configurables sans code durci. | `WKF-01`, `SOC-02` | Rôles et transitions paramétrables | **PRÊT** |
| **`AVE-03`** | **Must** | *« La décision (accord, rejet motivé) et les documents associés sont déposés dans le ticket et notifiés au client. »* | Agent habilité | Décision finale instruite | Enregistrement Accord ou Rejet (motif obligatoire), dépôt de document de décision, émission d'événement pour F009. | `COM-01`, `SOC-07` | Motif rejet obligatoire, document décisionnel | **PRÊT** |
| **`AVE-04`** | **Should** | *« Une demande d'avoir/exonération peut être liée au ticket de parcours client concerné. »* | Client / Agent | Choix lors de la soumission ou de l'instruction | Liaison bidirectionnelle vers un ticket Billing Import, Export ou Réclamation existant via `LiaisonTicket`. | `SOC-10` | Aucune (réutilisation pure F001) | **PRÊT** |

---

## 3. Cadrage Métier & Frontière Portail / Hors-Portail

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│ DANS LE PORTAIL CIT (Feature 007)                                                      │
│                                                                                        │
│  [Client]                                                                              │
│     │                                                                                  │
│     ├─► Soumet Demande (Avoir ou Exonération) + Justificatifs                          │
│     ├─► (Optionnel) Lie au ticket initial de facturation (AVE-04)                      │
│     │                                                                                  │
│  [Ticket F007 généré] (Statut: Créé / En attente de prise en charge)                   │
│     │                                                                                  │
│  [Agent Habilité / Instructeur Avoir-Exo]                                              │
│     ├─► Prend en charge le ticket (Statut: En cours d'instruction)                     │
│     ├─► Demande de complément documentaire si besoin (Statut: En attente client)       │
│     ├─► Transmet pour décision (Statut: En attente décision Finance)                   │
│                                                                                        │
└─────────────────────────────────────────┬──────────────────────────────────────────────┘
                                          │
                                          ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│ HORS PORTAIL (Circuit interne Finance CIT)                                             │
│                                                                                        │
│  - Contrôle comptable interne / vérification des écritures                             │
│  - Visa hiérarchique physique / électronique (Chef Comptable, DAF, etc.)              │
│  - Génération de l'avoir ou de l'attestation dans les logiciels métiers (IRIS, etc.)   │
│  - Émission de la décision formelle (Accord ou Rejet motivé)                           │
│                                                                                        │
└─────────────────────────────────────────┬──────────────────────────────────────────────┘
                                          │
                                          ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│ DANS LE PORTAIL CIT (Finalisation & Notification F007)                                 │
│                                                                                        │
│  [Agent Habilité / Décideur]                                                           │
│     ├─► Si ACCORD : Enregistre Accord + Dépose Document Avoir / Exonération            │
│     │   └─► Statut Ticket: "Accordé" -> "Clôturé"                                      │
│     │                                                                                  │
│     └─► Si REJET : Enregistre Motif Rejet Obligatoire (+ Document explicatif éventuel) │
│         └─► Statut Ticket: "Rejeté" -> "Clôturé"                                       │
│                                                                                        │
│  [Événements Socle]                                                                    │
│     ├─► Publication `AvoirTraite` / `ExonerationTraitee`                               │
│     └─► Notification automatique du client via F009 (Email, In-App, etc.)             │
│                                                                                        │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Analyse de PC-15 et PC-03

### PC-15 (Circuit de validation interne Finance)
- **Texte CDC §25** : *"statut exact du parcours Avoir et Exonération (circuits de validation internes) à formaliser avec la Finance."*
- **Impact réel** : Le circuit financier détaillé concerne les règles d'approbation internes à la direction financière (qui signe, à quel montant).
- **Conséquence pour le portail** : Le portail n'a pas besoin de figer en dur ces règles internes. Il lui suffit d'offrir des étapes configurables et une file de traitement dédiée où l'agent habilité peut marquer l'état d'instruction et consigner la décision finale.
- **Verdict PC-15** : **NON BLOQUANT pour la conception et l'implémentation du portail.**

### PC-03 (Intervenants Avoir / Exonération)
- **Impact réel** : Le CDC ne nomme pas les intitulés de postes précis (ex. "Comptable N2", "Responsable Avoirs").
- **Solution démontrable (AVE-02)** : L'exigence impose que les intervenants soient **paramétrables**. En introduisant des permissions RBAC dédiées (`Permission.AvoirExonerationInstruire`, `Permission.AvoirExonerationDecider`), le système s'adapte à n'importe quelle affectation opérationnelle sans inventer de rôles organisationnels rigides.
- **Verdict PC-03** : **NON BLOQUANT.**

---

## 5. Workflow de Suivi Portail

Le cycle de vie du ticket Avoir / Exonération s'appuie sur le moteur de workflow F001 :

1. **Créé (`Cree`)** : Demande soumise par le client avec pièces justificatives.
2. **En cours d'instruction (`EnCours`)** : Pris en charge par un agent instructeur.
3. **En attente client (`EnAttenteClient`)** : Demande de pièces ou d'explications complémentaires.
4. **En attente décision externe (`EnAttenteValidationFinance`)** : Dossier complet transmis pour visa interne Finance.
5. **Accordé (`Valide` / `Accorde`)** : Décision favorable enregistrée + document joint.
6. **Rejeté (`Rejete`)** : Décision défavorable enregistrée avec motif obligatoire.
7. **Clôturé (`Cloture`)** : Clôture formelle et archivage du ticket.

---

## 6. Gestion des Pièces Justificatives et Documents de Décision

### Justificatifs à la Soumission (AVE-01)
- Formulaire paramétrable permettant le dépôt de documents types :
  - *Pour Avoir* : Facture contestée, preuve de paiement/lettrage, lettre de réclamation/explication, document de transport (BL/Booking).
  - *Pour Exonération* : Attestation fiscale/douanière, décision ministérielle, agrément d'exonération, copie de déclaration.
- Le moteur documentaire F001 (`Document.Deposer`) assure le stockage sécurisé, le contrôle MIME/taille et l'immuabilité des pièces déposées.

### Documents de Décision (AVE-03)
- Lors de la validation de la décision :
  - En cas d'accord : Dépôt manuel par l'agent de la note d'avoir générée par la Finance ou de l'attestation d'exonération visée.
  - En cas de rejet : Consignation du motif circonstancié et dépôt optionnel d'une note de rejet.
- **Pas de génération automatique d'avoir financier comptable inventée** : Le document est produit par les services financiers hors portail et déposé dans le ticket.

---

## 7. Raccordement F009 & Réutilisation du Socle

| Capacité F007 | Composant Existant F001-F006 / F009 | Statut |
| :--- | :--- | :---: |
| Création de ticket & dossier | `Ticket.Creer`, `Dossier.Creer` (F001) | Réutilisable 100% |
| Gestion des pièces jointes | `DocumentRepository`, `IDocumentStockage` (F001) | Réutilisable 100% |
| Fil d'échanges & notes internes | `Message`, `FilMessages.razor` (F001) | Réutilisable 100% |
| Attente client & relance | `AttenteClientCommand`, `ACR-01` (F001/F006) | Réutilisable 100% |
| Liaison au ticket parent (AVE-04) | `LiaisonTicket`, `ILiaisonTicketRepository` (F001) | Réutilisable 100% |
| Notifications multicanales | `EvenementSocle` (`PublierEvenementAsync`) (F009) | Réutilisable 100% |
| Sécurité RBAC & Anti-IDOR | `IAutorisationService`, `IContexteUtilisateurWeb` (F002) | Réutilisable 100% |

---

## 8. Intégration dans le Module Billing Office

- Conformément à la structure du CDC (Section 11.6), Avoir & Exonération constitue le 4ème sous-module du **Billing Office** (après Import, Export et Simulation).
- Navigation proposée :
  - Espace Client : `/billing-office/avoir-exoneration/demande` ou `/avoir-exoneration/nouvelle-demande`
  - Espace Agent : File dédiée `/billing-office/avoir-exoneration/file` accessible aux agents porteurs de `Permission.AvoirExonerationInstruire`.

---

## 9. Analyse de la Recette Contractuelle

- **Audit `docs/04-matrice-recette.md`** : Aucun des 14 scénarios contractuels transverses (REC-SCE-01..14) n'est explicitement nommé "Avoir/Exonération".
- **Discipline de test** : Le plan de test F007 devra introduire une suite exhaustive de cas contractuels `TEST-SPEC` couvrant :
  1. Soumission d'une demande d'avoir avec justificatifs et lien vers ticket Billing (AVE-01, AVE-04).
  2. Soumission d'une demande d'exonération avec attestation légale (AVE-01).
  3. Instruction par l'agent, demande de complément et reprise après réponse client (AVE-02, SOC-06).
  4. Enregistrement d'un accord avec dépôt de document et notification (AVE-03).
  5. Enregistrement d'un rejet avec motif obligatoire et notification (AVE-03).
  6. Cloisonnement strict des demandes entre sociétés (SEC-01).

---

## 10. Matrice des Points Restants

| Point / Paramètre | Nature | Impact Spec | Impact Code | Traitement Recommandé |
| :--- | :--- | :---: | :---: | :--- |
| **Circuit interne Finance** | Organisationnel externe | Nul | Nul | Traité hors portail, statut d'attente générique dans le portail |
| **Rôles internes Finance** | Habilitations | Nul | Nul | Rôles génériques configurables via RBAC |
| **Liste exhaustive des pièces** | Paramétrage documentaire | Nul | Nul | Types de pièces configurables à la soumission |
| **Modèles de notifications** | Événementiel F009 | Nul | Nul | Événements socles émis (`AvoirTraite`, `ExonerationTraitee`) |
| **Liaison ticket de facturation** | Fonctionnel (AVE-04) | Précisé | Minime | Utilisation directe de `LiaisonTicket` existant |

---

## 11. Décision GO / NO-GO & Conclusion

La Feature 007 — Avoir & Exonération est **entièrement spécifiable et implémentable** sans ambiguïté :
- Le cadre architectural et fonctionnel est parfaitement circonscrit.
- Les 4 exigences (`AVE-01`, `AVE-02`, `AVE-03`, `AVE-04`) sont directes et s'appuient sur les mécanismes déjà livrés et éprouvés (F001, F002, F009).
- Les points PC-03 et PC-15 sont levés par le principe de dissociation entre l'orchestration portail et la validation financière hors portail.

**VERDICT : GO FOR /speckit.specify**

---

## 12. Bloc de Synthèse Prescrit

FEATURE CANDIDATE : 007 — Avoir & Exonération

AVE-01 : PRÊT (Soumission demande Avoir/Exonération avec justificatifs, génération ticket)
AVE-02 : PRÊT (Workflow propre à chaque type, étapes et intervenants paramétrables)
AVE-03 : PRÊT (Décision accord/rejet motivé, documents associés, notification client)
AVE-04 : PRÊT (Liaison optionnelle au ticket de facturation parent via LiaisonTicket)

AVE : 4/4 analysées

MUST / SHOULD / COULD : 3 Must (AVE-01, 02, 03) / 1 Should (AVE-04) / 0 Could

TRAITEMENT FINANCE DANS APP :
NON (Dissociation portail / chaîne financière interne hors portail)

TRAITEMENT FINANCE HORS APP :
Contrôles comptables, visas hiérarchiques et écritures ERP réalisés hors application par la Finance, consignés ensuite dans le ticket.

SOUMISSION CLIENT :
Formulaire dédié (choix Avoir ou Exonération), dépôt de justificatifs, liaison optionnelle au ticket de parcours concerné.

JUSTIFICATIFS :
Configurables à la soumission (facture contestée, attestation d'exonération, preuve de paiement, etc.).

WORKFLOW PORTAIL :
Créé -> En instruction -> En attente client -> En attente décision Finance -> Accordé / Rejeté -> Clôturé.

CIRCUIT FINANCE EXACT :
Externe au portail, non bloquant pour le flux applicatif.

PC-03 :
RÉSOLU par intervenants et permissions paramétrables (Permission.AvoirExonerationInstruire / Decider).

PC-15 :
RÉSOLU par frontière portail (suivi/décision) vs hors-portail (calculs et visas internes).

ACCORD :
Enregistrement décision favorable + dépôt du document de décision + notification client.

REJET MOTIVÉ :
Motif obligatoire circonstancié + dépôt éventuel de document + notification client.

DOCUMENTS DÉCISION :
Déposés dans le ticket par l'agent habilité (aucun générateur d'avoir financier automatisé inventé).

NOTIFICATION CLIENT :
Émission d'EvenementSocle vers F009 (Email, notifications portail).

RÉUTILISATION F001 :
Ticket, Dossier, DocumentJoint, Message, AttenteClient, LiaisonTicket, JournalAudit.

RÉUTILISATION F009 :
Publication exclusive d'événements socles pour distribution multicanale asynchrone.

INTÉGRATIONS EXTERNES :
Aucune (autonomie complète de la plateforme).

POINTS BLOQUANTS SPEC :
0 point bloquant

POINTS BLOQUANTS PLAN :
0 point bloquant

POINTS BLOQUANTS IMPLEMENTATION :
0 point bloquant

POINTS BLOQUANTS RECETTE :
0 point bloquant (scénarios TEST-SPEC AVE dédiés)

BASELINE :
777/777 tests verts (100%)

PENDING MODEL CHANGES :
0

COUVERTURE CDC ACTUELLE :
148/188 (78.7 %)

COUVERTURE PROJETÉE APRÈS F007 :
152/188 (80.8 %)

GO / NO-GO :
GO

VERDICT :
GO FOR /speckit.specify
