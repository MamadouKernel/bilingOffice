# Preflight — Feature 010 (Continuité Guichet & Rendez-vous)

> **Document** : `docs/14-preflight-feature-010.md`  
> **Date** : 1er septembre 2026  
> **Nature** : Audit de cadrage et de préparation pré-spécification. Aucun `specs/010-*` créé, aucun code écrit, aucune feature clôturée modifiée, aucun commit, aucun push.  
> **Méthode** : Reconstruction indépendante depuis le CDC source (`docs/CDC_Plateforme_Digitale_CIT_v3.docx`, `docs/cdc_text.txt`) et ses analyses de référence (`docs/00` à `docs/04`, `docs/13-preflight-feature-009.md`, `.specify/memory/constitution.md`).

---

## 1. État du Projet et Couverture CDC

- **Dernière feature clôturée** : Feature 009 — Notifications Multicanales & Centre In-App (commit `d2369f6`, verdict *CLOSE WITH ACCEPTED DEBT*).
- **Statut des features à date** :
  - **F001** (Socle Tickets & Workflows) : `CLOSED`
  - **F002** (Comptes Clients & Sociétés) : `CLOSED`
  - **F003** (Billing Office IMPORT) : `CLOSED`
  - **F004** (Billing Office EXPORT) : `CLOSED`
  - **F005** (Simulation Proforma) : `CLOSED`
  - **F006** (Customer Complaint) : `CLOSED WITH ACCEPTED DEBT`
  - **F007** (Avoir & Exonération) : `BLOCKED` (attente atelier métier Finance PC-03 / PC-15)
  - **F008** (Enrôlement Transporteurs) : `CLOSED WITH ACCEPTED DEBT`
  - **F009** (Notifications Multicanales) : `CLOSED WITH ACCEPTED DEBT`
- **Couverture CDC certifiée actuelle** : **142 / 188 exigences (≈ 75,5 %)**
- **Baseline technique certifiée** : **747 / 747 tests verts (100 %)**, 0 avertissement, 0 pending model change.

---

## 2. Reconstitution Exacte du Périmètre Candidat

Le périmètre de la **Feature 010** cible les exigences de la famille **Continuité Digital / Présentiel (`CON`)**, son instanciation métier (`IMP-15`) et l'exigence transverse de résilience associée (`ERR-04`).

### Matrice des Exigences Candidates

| ID | Priorité | Famille | Acteur | Déclencheur | Parcours | Données clés | Reprise | Dépendances | PC associé |
|:---|:---:|:---:|:---|:---|:---|:---|:---|:---|:---|
| **CON-01** | **Must** | CON | Client · Agent Guichet | Présentation physique au Billing Office | Reprise du dossier initié en ligne (ou inversement) | N° Ticket, N° Dossier, historique, pièces | Reprise directe sans ressaisie de la liasse | `SOC-01` [E] | `PC-03`, `PC-10` |
| **CON-02** | **Must** | CON | Agent Guichet | Arrivée usager au comptoir physique | Écran de recherche rapide multi-critères | N° Ticket, Nom/Raison sociale client, N° BL, N° Booking | Accès instantané à la fiche d'instruction | `SOC-01` [D] | `PC-03` |
| **CON-03** | **Must** | CON | Client | Nécessité de passage physique (paiement caisse, dépôt, assistance) | Prise de créneau de RDV sur le portail | Date, Heure/Créneau, N° Ticket/Dossier, Motif | RDV rattaché au ticket, donne accès au bureau | `SOC-01` [D] | `PC-10` |
| **CON-04** | **Should** | CON | Système / Client | Arrivée sur site CIT | Système externe de file d'attente physique distinct | N° Ticket portail vs Ticket borne physique | Redirection / consigne d'information (API différée) | Hors interfaçage | — |
| **IMP-15** | **Should** | IMP | Client · Agent Guichet | Issue du contrôle BL Import | Choix d'alternative présentielle / RDV physique | N° BL, N° Ticket, choix bascule | Ticket porte la trace de la bascule présentielle | `CON-01` [D] | `PC-10` |
| **ERR-04** | **Should** | ERR | Client · Agent Guichet | Indisponibilité / Panne portail web | Accueil physique au Billing Office | N° Ticket (ou pièces physiques) | Mode dégradé organisationnel documenté | `CON-01` [D] | — |

**Synthèse du périmètre candidat :**
- **Must** : 3 (CON-01, CON-02, CON-03)
- **Should** : 3 (CON-04, IMP-15, ERR-04)
- **Total exigences analysées** : **6 exigences**

---

## 3. Analyse du Concept de Continuité

La continuité digital/présentiel telle que définie par le CDC repose sur un principe clair : **aucun usager ne doit être bloqué par le numérique, et aucune démarche commencée en ligne ne doit être recommencée à zéro au guichet physique**.

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                        FLUX DE CONTINUITÉ DIGITAL / PRÉSENTIEL                         │
├────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                        │
│   [ ESPACE CLIENT EN LIGNE ]                        [ BILLING OFFICE PHYSIQUE ]        │
│   1. Client soumet son dossier Import/Export/Enrôl.                                    │
│   2. Contrôle documentaire / Facture émise                                             │
│   3. Client choisit de finaliser sur place (IMP-15) ──► 4. Arrivée au guichet CIT      │
│      OU présence obligatoire (photo/badge ENR-03)          avec son N° de ticket       │
│   5. Client réserve un créneau de RDV (CON-03)  ──────► 6. Agent recherche le dossier  │
│                                                            en 1 clic (CON-02)          │
│   8. Mise à jour instantanée du statut portail  ◄────── 7. Agent valide sur place      │
│      (historique & audit synchronisés CON-01)              (caisse, badge, conformité) │
│                                                                                        │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

### Ce que la Continuité EST :
1. **Unicité du dossier** : Le ticket et son numéro unique (`SOC-01`) sont la clé de voûte partagée entre le web et le guichet.
2. **Reprise sans ressaisie** : L'agent de guichet ouvre le ticket et dispose immédiatement de toutes les métadonnées saisies et pièces déjà déposées par le client.
3. **Traçabilité de la bascule** : L'historique et le journal d'audit consignent l'instruction présentielle avec l'identifiant de l'agent de guichet intervenant.
4. **Accès et Rendez-vous** : Le client dispose d'un module pour sélectionner une date/heure de passage facilitant son accueil au bureau de facturation.

### Ce que la Continuité N'EST PAS (Interdictions de sur-spécification) :
- **AUCUN mode offline PWA / synchronisation déconnectée complexe** : La plateforme est une application web hébergée côté serveur (Principe P3).
- **AUCUN moteur d'agenda externe lourd** (pas d'intégration Exchange, Google Calendar, Outlook) : Gestion des créneaux en base de données SQL Server locale.
- **AUCUNE liaison matérielle avec les bornes d'accueil physiques** (CON-04 stipule expressément que les deux systèmes restent distincts et que l'interfaçage API est une évolution future non requise).

---

## 4. Analyse Détaillée des Exigences

### 4.1 CON-01 — Reprise sans Ressaisie (Must)
- **Objet repris** : Le `Ticket` (ou le `Dossier` multi-BL) créé en ligne.
- **Acteur** : Agent habilité au guichet (Agent Billing, Agent Gate, Agent Accueil).
- **Données conservées** : Liasse documentaire complète, métadonnées BL/booking, montants de facturation, historique des échanges et des décisions intermédiaires.
- **Action au guichet** : L'agent consulte, ajoute éventuellement une pièce scannée sur place, valide le paiement en caisse physique ou valide l'étape en cours. Le ticket progresse dans son workflow sans créer de doublon.

### 4.2 CON-02 — Recherche Rapide au Guichet (Must)
- **Acteur** : Agent disposant de la responsabilité guichet (`Permission.GuichetRecherche` ou agents des files existantes).
- **Critères de recherche** :
  - Numéro de ticket (recherche exacte ou préfixée)
  - Numéro de BL ou numéro de Booking
  - Nom / Raison sociale du client
  - Numéro de Dossier multi-BL
- **Cloisonnement et Performance** : Requête optimisée côté serveur, respectant les habilitations de l'agent (ne divulgue pas les dossiers hors périmètre opérationnel).

### 4.3 CON-03 — Prise de Rendez-vous (Must)
- **Acteur** : Client authentifié (ou créé par l'agent lors d'un accueil physique).
- **Fonctionnement** :
  - Choix d'un créneau disponible (date, plage horaire parmi les créneaux configurés).
  - Rattachement obligatoire au `TicketId` ou `DossierId`.
  - Motif du rendez-vous (ex. *Paiement caisse physique*, *Assistance dépôt*, *Enrôlement photo/badge*).
  - Statut du RDV : `Reserve`, `Honore`, `Annule`, `NonPresente`.
  - Confirmation visuelle sur le portail et émission d'un événement métier (`RendezVousReserve`).

### 4.4 CON-04 — Système de File d'Attente Externe (Should)
- **Texte CDC** : *« La plateforme et le système de gestion de file d'attente existant restent deux systèmes distincts ; une redirection est possible ; une intégration API constitue une évolution future et n'est pas requise. »*
- **Portée F010** : Affichage clair des consignes d'accueil physique (ex. *« Munissez-vous de votre numéro de ticket [IMP-xxx] et présentez-vous à la borne d'accueil du Billing Office »*). Aucun connecteur logiciel avec les automates de tickets papier.

### 4.5 IMP-15 — Alternative Présentielle Billing Import (Should)
- **Déclencheur** : À l'étape Contrôle BL validé / Facture émise, le client choisit l'option « Finaliser au guichet ».
- **Comportement** : Le ticket passe à l'état `EnAttentePassageGuichet` (ou conserve `EnAttentePaiement` avec tag présentiel) et invite le client à réserver un RDV via CON-03.

### 4.6 ERR-04 — Mode Dégradé en cas d'Indisponibilité (Should)
- **Principe** : Si le portail client est inaccessible (coupure réseau client, maintenance), le client se présente physiquement au guichet avec ses pièces papier ou sa référence de ticket.
- **Traitement** : Les agents instruisent la demande via l'accès local et enregistrent les opérations dès rétablissement, garantissant l'absence de rupture de service maritime.

---

## 5. Réutilisation des Fondations Existantes (F001 à F009)

| Composant existant | Niveau de réutilisation pour F010 | Nature de l'adaptation |
|:---|:---:|:---|
| **`Ticket` & `Dossier` (F001/F003)** | **100 % réutilisable** | Support naturel de l'historique et des statuts de reprise |
| **`FilMessages` / `JournalEvenements`** | **100 % réutilisable** | Traçabilité des actions présentielles et publication d'événements |
| **`JournalisationService` & Audit (SOC-13)** | **100 % réutilisable** | Audit des recherches guichet et des prises/modifications de RDV |
| **`EvenementSocle` & Notifications (F009)** | **100 % réutilisable** | Déclenchement automatique des notifications de confirmation RDV |
| **Habilitations & Contexte Utilisateur (F001/F002)**| **100 % réutilisable** | Contrôle d'accès serveur pour la recherche guichet et la gestion RDV |
| **Entité `RendezVous`** | **Nouvelle entité** | Entité Domaine + Table EF Core `RendezVous` + Créneaux |
| **Écran de Recherche Rapide Guichet** | **Nouvelle UI** | Page Blazor optimisée `/guichet/recherche` pour les agents |
| **Formulaire Prise de RDV Client** | **Nouvelle UI** | Modal / composant intégré sur la fiche ticket et le dashboard |

---

## 6. Découplage F010 (Continuité) vs F009 (Notifications)

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                    DÉCOUPLAGE ÉVÉNEMENTIEL F010 ──► F009                        │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│   FEATURE 010 (Continuité & RDV)                FEATURE 009 (Notifications)     │
│   • Crée l'entité RendezVous                    • Reçoit l'événement via        │
│   • Gère la réservation de créneau                DistribuerNotificationsUseCase│
│   • Publie RendezVousReserve / Annule           • Applique les règles semées    │
│     sur EvenementSocle                            (Portail + E-mail + SMS)      │
│                                                                                 │
│   ► AUCUN appel direct de service d'envoi dans F010                             │
│   ► F010 ignore la technologie des canaux (SMS/Email/Portail)                   │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## 7. Complétude du Scénario Contractuel REC-SCE-14

Avec la livraison de la Feature 010, l'intégralité des volets du scénario contractuel **`REC-SCE-14`** (Section 22 du CDC) sera couverte :

| Volet REC-SCE-14 | Module source | Statut après F009 | Statut après F010 |
|:---|:---:|:---:|:---:|
| Notifications sur chaque canal avec lien vers ticket | F009 | ✅ Couvert | ✅ Couvert |
| Contrôle des habilitations & étanchéité des files | F001 / F002 | ✅ Couvert | ✅ Couvert |
| Journal d'audit et traçabilité des opérations | F001 | ✅ Couvert | ✅ Couvert |
| **Recherche de ticket au guichet par critères multiples** | **F010 (CON-02)** | ❌ Manquant | ✅ **Couvert (F010)** |
| **Prise de rendez-vous donnant accès au bureau** | **F010 (CON-03)** | ❌ Manquant | ✅ **Couvert (F010)** |
| **Continuité et reprise sans ressaisie** | **F010 (CON-01)** | ❌ Manquant | ✅ **Couvert (F010)** |

> [!IMPORTANT]
> **Clôture de REC-SCE-14 :** La Feature 010 permettra de concevoir et d'exécuter le test de bout en bout global `RecSce14E2ETests`, clôturant le dernier grand scénario transverse contractuel du CDC.

---

## 8. Analyse des Points à Clarifier (PC)

| Point à Clarifier | Intitulé | Statut pour Spec Kit | Décision d'architecture recommandée |
|:---|:---|:---:|:---|
| **PC-03** | Rôle applicatif "Agent de guichet" | **NON BLOQUANT SPEC** | Porté par une permission explicite `Permission.GuichetRecherche` attribuée aux agents Billing, Gate et Accueil. |
| **PC-10** | Statuts de bascule présentielle (IMP-15) | **NON BLOQUANT SPEC** | Le ticket conserve son cycle standard avec un indicateur / statut `EnAttentePassageGuichet` ou tag de RDV rattaché. |
| **PC-12** | Plages d'ouverture et créneaux RDV | **NON BLOQUANT SPEC** | Créneaux paramétrables (par défaut : 08h00-16h00 par tranches de 30 min, jours ouvrés). |

---

## 9. Analyse du Découpage Fonctionnel

- **Option retenue** : **Feature 010 unique — Continuité Guichet & Rendez-vous** (`CON-01` à `CON-04`, `IMP-15`, `ERR-04`).
- **Justification** :
  - Périmètre très resserré et homogène (4 exigences CON + 2 exigences associées).
  - Forte cohésion : le module de rendez-vous (`CON-03`) et l'écran de recherche rapide (`CON-02`) constituent ensemble les deux faces (Client / Agent) du même parcours de continuité.
  - Débloque 100% de `REC-SCE-14`.

---

## 10. Risques Techniques et Mesures d'Atténuation

| Risque | Impact | Mesure d'atténuation |
|:---|:---:|:---|
| **RSQ-01 : Surbooking / Conflit de créneau** | Moyen | Concurrence optimiste ou contrainte d'unicité par créneau/guichet pour éviter qu'un même créneau ne soit réservé deux fois. |
| **RSQ-02 : IDOR / Recherche guichet trop permissive** | Critique | Filtre de sécurité côté serveur limitant la recherche aux dossiers accessibles selon les habilitations de l'agent. |
| **RSQ-03 : Complexité calendrier externe** | Élevé | Interdiction d'intégrer une API d'agenda tierce : stockage relationnel standard des créneaux dans EF Core. |

---

## 11. Décision Finale et Conditions d'Entrée

- **Périmètre fonctionnel** : Parfaitement délimité (6 exigences au total).
- **Dépendances** : F001, F002, F003, F008 et F009 sont 100% closes et certifiées.
- **Baseline technique** : 747/747 tests verts, 0 pending model change.
- **Aucun obstacle bloquant pour le lancement de la spécification.**

---

## 12. Verdict Final

```
================================================================================
VERDICT PREFLIGHT : GO FOR /speckit.specify
================================================================================
FEATURE CANDIDATE : 010 — Continuité Guichet & Rendez-vous (CON)
STATUT : ÉLIGIBLE AU LANCEMENT DE SPÉCIFICATION
================================================================================
```
