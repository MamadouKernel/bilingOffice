# Architecture & schéma global du workflow YABA (swimlanes)

> **Document** : `docs/108-architecture-workflow-swimlanes.md`
> **Objet** : cartographie par couloirs de responsabilité (swimlanes) du cycle de vie de chaque
> ticket, du dépôt client jusqu'à la clôture/archivage.
> **Canaux de communication** : **Portail web (In-App)** + **e-mail transactionnel** — pas de
> SMS/WhatsApp (décision produit du 25/09/2026 ; `CanalNotification` = In-App + Email).
> **Conformité code** : schéma vérifié contre l'implémentation (voir § 4).

---

## 1. Schéma global (swimlanes)

```
=============================================================================================================================================
 COULOIR 1 : ESPACE CLIENT & TRANSPORTEURS (Actions externes)
=============================================================================================================================================
       │ [Choix du module]
       ├─────────────────────────────────┬────────────────────────────────┬──────────────────────────┬───────────────────────────────┐
       ▼                                 ▼                                ▼                          ▼                               ▼
 [1. SIMULATION PROFORMA]     [2. PARCOURS IMPORT]              [3. PARCOURS EXPORT]       [4. CUSTOMER COMPLAINT]        [5. ENRÔLEMENT GATE]
  Saisie conteneurs            Formulaire (1 à 5 BL)             Formulaire (1 à 5 Bookings) Choix catégorie réclamation  Formulaire Chauffeur
  (Taille, type, poids, etc.)  + Dépôt pièces jointes            + Dépôt pièces jointes      + Description & pièces       + Pièces d'identité/permis
       │                                 │                                │                          │                               │
  Calcul indicatif                       ▼                                ▼                          ▼                               ▼
  immédiat (non engageant)      [Soumission Dossier]              [Soumission Dossier]        [Soumission Réclamation]      [Soumission Enrôlement]
                                         │                                │                          │                               │
=========================================│================================│==========================│===============================│=======
 COULOIR 2 : SOCLE YABA (Moteur de workflow, SLA, Unicité, Horodatage & Notifications)               │                               │
=========================================│================================│==========================│===============================│=======
                                         ▼                                ▼                          │                               │
                                [Contrôle d'unicité]             [Contrôle d'unicité]                │                               │
                                 Existe déjà en actif ?           Existe déjà en actif ?             │                               │
                                  ├─ OUI ──► Blocage & Renvoi      ├─ OUI ──► Blocage & Renvoi       │                               │
                                  │          vers ticket existant  │          vers ticket existant   │                               │
                                  └─ NON                           └─ NON                            │                               │
                                         ▼                                ▼                          │                               │
                                [Génération Tickets]             [Génération Tickets]                │                               │
                                 1 Ticket / BL (séparés)          1 Ticket / Booking                 │                               │
                                 Regroupés sous 1 N° Dossier      Regroupés sous 1 N° Dossier        │                               │
                                         │                                │                          │                               │
                                         ├────────────────────────────────┴──────────────────────────┴───────────────────────────────┤
                                         ▼                                                                                           ▼
                                [Horodatage & Calcul SLA CIT] (Déclenchement alertes : 50%, 80%, 100% dépassement / Escalade)         │
                                [Notifications Automatiques]  (Email transactionnel + Notification dans le portail client)           │
                                         │                                │                          │                               │
=========================================│================================│==========================│===============================│=======
 COULOIR 3 : FRONT OFFICE (Pilote Manifeste) & BACK OFFICE (Export)       │                          │                               │
=========================================│================================│==========================│===============================│=======
                                         ▼                                │                          │                               │
                               [File Pilote Manifeste]                    │                          │                               │
                                 - En attente prise en charge             │                          │                               │
                                 - Prise en charge par agent              │                          │                               │
                                 - Contrôle liasse BL                     │                          │                               │
                                         │                                │                          │                               │
                        ┌────────────────┼────────────────┐               │                          │                               │
                        ▼                ▼                ▼               │                          │                               │
                   [Rejet motivé]  [Complément]       [Validé]            │                          │                               │
                        │                │                │               │                          │                               │
                        │                ▼                │               ▼                          │                               │
                        │        [Attente client]         │     [File Back Office Export]            │                               │
                        │        (Suspension SLA)         │       - Codification sous IPAKI          │                               │
                        │                │                │       - Validation / Rejet / Complément  │                               │
                        │        Client notifié           │               │                          │                               │
                        │        Dépôt pièces ────► File  │               ▼ [Codifié]                │                               │
                        │                                 │               │                          │                               │
========================│=================================│===============│==========================│===============================│=======
 COULOIR 4 : FACTURATION (Pilote Facturation Terre)       │               │                          │                               │
========================│=================================│===============│==========================│===============================│=======
                        │                                 │               ▼                          │                               │
                        │                                 │      [Création Draft IN IPAKI]           │                               │
                        │                                 │               │                          │                               │
                        │                                 ▼               ▼                          │                               │
                        │                        [File Pilote Facturation Terre]                     │                               │
                        │                          - Contrôle tarifaire                              │                               │
                        │                          - Édition facture dans IPAKI                      │                               │
                        │                          - Dépôt facture dans le Ticket                    │                               │
                        │                                         │                                  │                               │
========================│=========================================│==================================│===============================│=======
 AIGUILLAGE PAR PROFIL CLIENT (Selon le compte paramétré au départ)                                  │                               │
========================│============================================================================│===============================│=======
                        │         ┌───────────────────────┬──────────────────────┬───────────────────┤                               │
                        │         ▼                       ▼                      ▼                   ▼                               │
                        │   [CLIENT NORMAL]       [CLIENT EN COMPTE]     [CLIENT DEPOSIT]     [CLIENT IES]                           │
                        │         │                       │                      │                   │                               │
                        │         ▼                       │                      │                   ▼                               │
                        │   Client notifié                │                      │         [Traitement Parallèle]                    │
                        │   Télécharge facture            │                      │         Pilote Manifeste                          │
                        │   Paiement Wave/Banque          │                      │                 +                                 │
                        │   Dépôt reçu paiement           │                      │         Pilote Contrôle Doc                       │
                        │         │                       │                      │                   │                               │
                        │         ▼                       │                      │                   ▼                               │
========================│=========│=======================│======================│===================│===============================│=======
 COULOIR 5 : FINANCE & COMPTABILITÉ                       │                      │                   │                               │
========================│=========│=======================│======================│===================│===============================│=======
                        │         ▼                       │                      ▼                   │                               │
                        │   [File Caisse/Finance]         │            [File Comptabilité Client]    │                               │
                        │   Contrôle de la preuve         │            Lettrage sous IRIS            │                               │
                        │   Validation paiement           │            Confirmation dans le ticket   │                               │
                        │         │                       │                      │                   │                               │
========================│=========│=======================│======================│===================│===============================│=======
 COULOIR 6 : BACK OFFICE (Pilote Contrôle Documentaire)   │                      │                   │                               │
========================│=========│=======================│======================│===================│===============================│=======
                        │         │                       │                      │                   │                               │
                        │         └───────────────────────┴──────────────────────┘                   │                               │
                        │                                 │                                          │                               │
                        │                                 ▼                                          │                               │
                        │                [File Pilote Contrôle Documentaire]                         │                               │
                        │                  - Contrôles finaux dans systèmes externes                 │                               │
                        │                  - Dépôt Bon de Sortie (BS) + BADT (Import)                │                               │
                        │                    OU dépôt Bon d'Entrée Export (BEE)                      │                               │
                        │                                 │                                          ▼                               │
                        │                                 │                                 [Synchronisation IES]                    │
                        │                                 │                                 Dépôt pièces ──► Suite sur IES           │
                        │                                 │                                          │                               │
========================│=================================│==========================================│===============================│=======
 COULOIR 7 : SERVICES SPÉCIALISÉS (Support Opérationnel & Agents Gate)                               │                               │
========================│=================================│==========================================│===============================│=======
                        │                                 │                                          │   [File Support Opérationnel] │
                        │                                 │                                          │     - Instruction réclamation │
                        │                                 │                                          │     - Échanges dans le ticket │
                        │                                 │                                          │     - Réponse formelle        │
                        │                                 │                                          │               │               │
                        │                                 │                                          │               │ [File Gate]   ▼
                        │                                 │                                          │               │ Instruction pièces
                        │                                 │                                          │               │        │
                        │                                 │                                          │               │ ┌──────┴──────┐
                        │                                 │                                          │               │ ▼             ▼
                        │                                 │                                          │               │ [Incomplet]  [Validé]
                        │                                 │                                          │               │ Attente doc  Convocation
                        │                                 │                                          │               │              Photo/Badge
                        │                                 │                                          │               │               │
                        │                                 │                                          │               │               ▼
                        │                                 │                                          │               │      [Venue physique]
                        │                                 │                                          │               │      Photo + Remise
                        │                                 │                                          │               │      badge d'accès
                        │                                 │                                          │               │               │
========================│=================================│==========================================│===============│===============│=======
 DÉNOUEMENT & SORTIE DE LA PLATEFORME YABA                                                                                           │
========================│=================================│==========================================│===============│===============│=======
                        ▼                                 ▼                                          ▼               ▼               ▼
                 [TICKET REJETÉ]                                       [TICKET CLÔTURÉ & ARCHIVÉ]
                 - Motif obligatoirement consigné                      - Documents disponibles au téléchargement sur le portail
                 - Notifié par Email + Portail                         - Notifié par Email + Portail
                 - Traçabilité audit trail                             - Audit trail inaltérable & verrouillage
```

---

## 2. Tableau de synthèse des parcours clients

| Profil client | Étape 1 : Contrôle | Étape 2 : Facturation | Étape 3 : Paiement / Comptabilité | Étape 4 : Délivrance finale | Clôture YABA |
| --- | --- | --- | --- | --- | --- |
| **Normal** | Contrôle BL validé | Facture éditée et déposée | Preuve de paiement déposée + validée Finance | Bon de Sortie (BS) + BADT | Archivé |
| **En compte** | Contrôle BL validé | Facture éditée et déposée | *Étape sautée* (règlement différé mensuel) | Facture + BS + BADT (direct) | Archivé |
| **Deposit** | Contrôle BL validé | Facture éditée et déposée | Lettrage sous IRIS par la Comptabilité | Facture + BS + BADT | Archivé |
| **IES** | Traitement parallèle Manifeste + Contrôle Doc | *Hors YABA* (portail IES) | *Hors YABA* (portail IES) | Dépôt des documents requis sur YABA | Clôturé |
| **Export (tous)** | Codification booking IPAKI | Draft IN + Facturation | Selon statut (Normal : paiement ; autres : direct) | **BEE (Bon d'Entrée Export)** | Archivé |
| **Réclamations** | Instruction Support Op. | S/O | S/O | Réponse motivée et archivée | Résolu |
| **Enrôlement Gate** | Vérification des pièces à distance | S/O | S/O | Convocation → Photo & remise badge | Badge actif |

---

## 3. Dérogations, SLA & supervision

1. **Calcul des SLA & horodatage**
   - **Temps interne CIT** : décompté tant que le ticket est dans une file d'agent.
   - **Temps client** : le chronomètre CIT est **suspendu** dès le statut « En attente client »
     (demande de complément ou attente de paiement).
   - **Seuils d'alerte** : 50 %, 80 %, puis alerte d'escalade au superviseur à 100 % de dépassement.

2. **Transferts & réaffectations**
   - Transfert entre files selon une **matrice paramétrable**, avec **motif obligatoire** (sans faire
     recréer un ticket au client).
   - Réaffectation d'un agent à un autre au sein d'une file.
   - Le superviseur dispose d'**actions de masse** (notamment en cas d'absence d'un opérateur).

3. **Double nature des messages**
   - **Messages publics** : visibles par le client et les agents.
   - **Notes internes** : réservées aux agents CIT (support des tags `@collaborateur`) pour les
     arbitrages métier, sans visibilité externe.

---

## 4. Conformité du schéma au code (vérifié)

| Élément du schéma | Statut |
| --- | --- |
| Canaux **Portail + Email**, pas de SMS/WhatsApp | ✅ `CanalNotification` = In-App + Email |
| Contrôle d'**unicité BL/booking actif** + renvoi au ticket existant | ✅ UNI-01 |
| **1 ticket / BL (ou booking)** regroupés sous 1 dossier | ✅ |
| Aiguillage profil (Normal→Validation Paiement ; En compte→Contrôle Doc direct ; Deposit→Comptabilité ; IES→parallèle File IES) | ✅ `ValiderTicketImportCommand` + `EditerFactureCommand` |
| Export : Back Office (codif IPAKI) → Draft IN → Facturation → **BEE** | ✅ commandes BillingExport |
| SLA suspendu « en attente client », seuils 50/80/100 + escalade | ✅ SLA-02/03 |
| Transfert (matrice + motif) / réaffectation / actions de masse | ✅ |
| Notes internes `@collaborateur` vs messages publics | ✅ MSG-02/04 |
| Support Opérationnel (réclamation) · Gate (enrôlement photo/badge) | ✅ |
| Création de ticket **via l'assistant** tracée (origine « Assistant ») | ✅ `SoumettreReclamationCommand` (historique + audit + événement) |

**Écart connu** (déjà reflété par le schéma) : canaux mobiles (SMS/WhatsApp) absents — l'abstraction
existe mais aucun fournisseur n'est arbitré (décision DSI/achat).
