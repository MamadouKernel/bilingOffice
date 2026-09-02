# Preflight — Feature 009 (Notifications Multicanales & Continuité Digital/Présentiel)

> **Document** : `docs/13-preflight-feature-009.md`  
> **Date** : 31 août 2026  
> **Nature** : Audit de préparation et de cadrage pré-spécification. Aucun `specs/009-*` créé, aucun code écrit, aucune feature clôturée modifiée, aucun commit, aucun push.  
> **Méthode** : Reconstruction indépendante depuis le CDC source (`docs/CDC_Plateforme_Digitale_CIT_v3.docx`, `docs/cdc_text.txt`) et ses analyses de référence (`docs/00` à `docs/04`, `docs/05-roadmap-features.md`, `.specify/memory/constitution.md`).

---

## 1. État du Projet et Couverture CDC

- **Dernière feature clôturée** : Feature 008 — Enrôlement Transporteurs (commit `c4f7c87`, verdict officiel *CLOSE WITH ACCEPTED DEBT*).
- **Statut global des features** :
  - **F001** (Socle Tickets & Workflows) : `CLOSED`
  - **F002** (Comptes Clients & Sociétés) : `CLOSED`
  - **F003** (Billing Office IMPORT) : `CLOSED`
  - **F004** (Billing Office EXPORT) : `CLOSED`
  - **F005** (Simulation Proforma) : `CLOSED`
  - **F006** (Customer Complaint) : `CLOSED WITH ACCEPTED DEBT`
  - **F007** (Avoir & Exonération) : `BLOCKED` (attente atelier métier Finance PC-03 / PC-15)
  - **F008** (Enrôlement Transporteurs) : `CLOSED WITH ACCEPTED DEBT`
- **Couverture CDC certifiée** : **134 / 188 exigences (≈ 71,3 %)**
- **Baseline technique certifiée** : **598 / 598 tests verts**, 0 warning, 0 pending model change.

---

## 2. Reconstitution Exacte du Périmètre Candidat

Le périmètre examiné couvre les exigences des modules **Communication et Notifications (COM / NOT)** et **Continuité Digital / Présentiel (CON)**, ainsi que les exigences transverses directement couplées (`SEC-12`, `ACR-01`, `ERR-02`, `ERR-04`).

### Matrice Détaillée des Exigences Candidates

| ID | Priorité | Famille | Acteur | Déclencheur | Canal | Destinataire | Contenu minimal | État / Dépendances | PC associé |
|:---|:---:|:---:|:---|:---|:---|:---|:---|:---|:---|
| **COM-01** | **Must** | COM | Système | Événement significatif du workflow | E-mail, Portail, WhatsApp et/ou SMS | Client | Notification proactive avec invitation à consulter | Actif · SOC-01 [D] | PC-16, PC-17 |
| **COM-02** | **Must** | COM | Système | Émission d'une notification | Tous canaux externes | Client | Lien d'accès direct au ticket (avec authentification obligatoire) | Actif · SEC-12 [E] | — |
| **COM-03** | **Must** | COM | Admin fonctionnel | Paramétrage initial / administration | Modèles de messages | Destinataires configurés | Événements déclencheurs et modèles éditables par module/étape/canal | Actif · F001 | PC-17 |
| **COM-04** | **Must** | COM | Client / Agents | Échange dans le ticket | Chat / Fil de messages du ticket | Client & Agents habilités | Messages horodatés et historisés (réaffirmation SOC-05) | Couvert F001 (socle) | — |
| **COM-05** | **Should** | COM | Client | Modification de son profil | Préférences de notification | Client | Activation/désactivation par canal (hors notifications obligatoires) | Actif · COM-01 [D] | — |
| **COM-06** | **Could** | COM | Système (fond) | Ticket en attente client > délai | Canaux configurés | Client | Relances automatiques avant suggestion clôture (dimension canal de CYC-04) | Actif · CYC-04 [D] | PC-02 |
| **NOT-01** | **Must** | NOT | Client / Agents | Réception d'un événement | Centre de notifications portail | Utilisateur connecté | Notification avec statut Lu / Non Lu et lien vers ticket | Actif · COM-01 [D] | — |
| **NOT-02** | **Must** | NOT | Admin fonctionnel | Paramétrage règles de diffusion | Moteur de dispatch | Destinataires | Règles par événement et canal (ex. Facture → Portail+Email ; Complément → +WhatsApp) | Actif · COM-03 [E] | PC-17 |
| **NOT-03** | **Should** | NOT | Système | Chargement / mise à jour portail | Badge interface Header | Utilisateur connecté | Compteur visuel temps réel des notifications non lues | Actif · NOT-01 [D] | — |
| **CON-01** | **Must** | CON | Client / Guichet | Arrivée physique au Billing Office | Portail / Guichet | Agent & Client | Reprise du dossier en cours par numéro de ticket sans ressaisie | Actif · SOC-01 [E] | PC-03, PC-10 |
| **CON-02** | **Must** | CON | Agent Guichet | Présentation client au guichet | Écran recherche rapide | Agent Guichet | Recherche multicritère rapide (N° ticket, client, BL, booking) | Actif · SOC-01 [D] | PC-03 |
| **CON-03** | **Must** | CON | Client | Besoin de présence physique | Formulaire portail | Client & CIT | Prise de RDV rattachée au ticket/dossier, accès bureau facturation | Actif · SOC-01 [D] | PC-10 |
| **CON-04** | **Should** | CON | Système | Arrivée physique client | Système externe file d'attente | Client | Système file d'attente physique distinct ; redirection possible, API future | Actif · Hors interfaçage | — |
| **SEC-12** | **Must** | SEC | Système | Clic sur lien de notification | Portail web | Client | Aucun accès direct sans contrôle d'identité : redirection après login | Actif · COM-02 [E] | — |
| **ACR-01** | **Must** | ACR | Client | Connexion portail client | Dashboard « Mes actions requises » | Client | Bloc mettant en évidence les actions bloquantes attendues du client | Actif · SOC-08 [D] | — |
| **ERR-02** | **Must** | ERR | Système | Échec d'envoi externe (réseau/tiers) | Moteur de notification | Système / Journal | Retry automatique, traçage, isolation de panne entre canaux | Actif · COM-01 [D] | — |
| **ERR-04** | **Should** | ERR | Tous | Panne / indisponibilité portail | Guichet physique | Client & Agents | Mode dégradé physique documenté, reprise par N° de ticket | Actif · CON-01 [D] | — |

**Synthèse du périmètre candidat initial :**
- **Must** : 11 (COM-01, COM-02, COM-03, COM-04, NOT-01, NOT-02, CON-01, CON-02, CON-03, SEC-12, ACR-01, ERR-02)
- **Should** : 4 (COM-05, NOT-03, CON-04, ERR-04)
- **Could** : 1 (COM-06)
- **Total exigences analysées** : **17 exigences**

---

## 3. Analyse des 3 Familles Distinctes

Le regroupement historique « Notifications & Continuité » associe 3 familles fonctionnelles aux logiques très différentes :

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│                             ANALYSE DES 3 FAMILLES                               │
├────────────────────────────────┬────────────────────────┬────────────────────────┤
│ A. FAMILLE COM                 │ B. FAMILLE NOT         │ C. FAMILLE CON         │
│ Communication Multicanale      │ Centre de Notifications│ Continuité Présentielle│
├────────────────────────────────┼────────────────────────┼────────────────────────┤
│ • Modèles de messages param.   │ • Boîte de réception   │ • Prise de Rendez-vous │
│ • Connecteurs Email / SMS / WA │   in-app (Portail)     │   rattachée au ticket  │
│ • Dispatching d'événements     │ • Statuts Lu / Non Lu  │ • Recherche rapide     │
│ • Préférences client           │ • Compteur badge Header│   guichet (N°, BL...)  │
│ • Retry & résilience canaux    │ • Accès direct ticket  │ • Bascule digitale →   │
│ • Sécurité des liens (SEC-12)  │ • Actions requises ACR │   physique sans perte  │
└────────────────────────────────┴────────────────────────┴────────────────────────┘
```

### Constat d'indépendance fonctionnelle :
1. **Famille COM & NOT** : Forment un bloc cohérent et homogène de **gestion des notifications et de l'information proactive client**. Elles s'abonnent aux événements du socle F001 (`EvenementSocle`) et fournissent le centre de notifications in-app et les passerelles d'envoi externe.
2. **Famille CON** : Concerne le **parcours physique au guichet du Billing Office** et la **gestion de créneaux de rendez-vous**. Elle est fonctionnellement liée aux étapes de facturation/caisse physique (`IMP-15`) et n'a aucune dépendance technique envers les passerelles SMS/WhatsApp.

---

## 4. Inventaire des Canaux & Statut des Fournisseurs

| Canal | Exigence source | Obligatoire ? | Fournisseur défini dans CDC ? | Configuration disponible ? | Bloque Spec ? | Bloque Plan ? | Bloque Prod ? |
|:---|:---:|:---:|:---|:---|:---:|:---:|:---:|
| **Portail In-App** | COM-01, NOT-01 | **OUI (Must)** | Autonome (Plateforme CIT) | OUI (BDD SQL Server) | NON | NON | NON |
| **E-mail** | COM-01, NOT-02 | **OUI (Must)** | Non spécifié (SMTP / API d'entreprise) | NON (Simulation locale / Mock en dev) | NON | NON | OUI (PC-13/SMTP) |
| **WhatsApp** | COM-01, ENR-04 | Optionnel ("WhatsApp **et/ou** SMS") | **AUCUN** (Sujet à souscription / coût, PC-16) | NON | NON (si abstraction) | NON | **OUI (PC-16)** |
| **SMS** | COM-01, ENR-04 | Optionnel ("WhatsApp **et/ou** SMS") | **AUCUN** (Sujet à souscription opérateur local, PC-16) | NON | NON (si abstraction) | NON | **OUI (PC-16)** |

> [!CAUTION]
> **Interdiction d'inventer des prestataires externes :**  
> Conformément à la Constitution, il est strictement interdit de coder en dur ou d'imposer un prestataire tiers (ex. Twilio, Meta Cloud API, SendGrid, Infobip, Azure Communication Services, Orange SMS CI) sans décision formelle du projet.
> 
> **Règle d'architecture requise :** Le système doit définir des abstractions pures dans la couche Application (`IEmailService`, `ISmsService`, `IWhatsAppService` ou un dispatcher unifié `INotificationChannel`) avec une implémentation locale simulée/tracée en base de données pour les tests et le développement, permettant de brancher le fournisseur retenu sans modifier le code métier.

---

## 5. Réutilisation des Capacités Existantes (F001 à F008)

| Besoin Feature 009 | Capacité existante dans le socle | Statut de réutilisation | Nature de l'adaptation |
|:---|:---|:---:|:---|
| **Événements déclencheurs** | `EvenementSocle` (F001), `JournalAudit` | **Réutilisable à 100%** | Abonnement du service de notification aux événements |
| **Historique des échanges** | `MessageTicket` / `FilMessages` (F001) | **Réutilisable à 100%** | COM-04 déjà couvert par le chat du ticket |
| **Badge & cloche notifications** | `HeaderBar.razor` (bouton cloche + badge CSS) | **Réutilisable à 80%** | Câblage dynamique du composant avec le compte de non-lus |
| **Relances absence réponse** | `RelancesAbsenceReponseUseCase` | **Existant (F001)** | `COM-06` : Ajout de l'envoi de notification lors d'une relance |
| **Surveillance SLA / Alertes** | `SurveillanceSlaService` / `Alerte` | **Existant (F001)** | Routage des alertes vers les canaux paramétrés |
| **Événements Convocation Chauffeur** | `DeclencherConvocationCommand` (F008) | **Existant (F008)** | Notification de convocation prête à être émise |
| **Centre de notifications in-app** | *Aucune entité `NotificationUtilisateur`* | **Nouvelle capacité** | Entité Domaine + Table EF Core + Query / Command |
| **Gestion des Rendez-vous (CON-03)**| *Aucune entité `RendezVous`* | **Nouvelle capacité** | Entité Domaine + Table EF Core + Agenda / Créneaux |
| **Recherche rapide Guichet (CON-02)**| `ConsulterFileQuery` / Recherche tickets | **Extension** | Endpoint optimisé de recherche multicritère guichet |

---

## 6. Notifications Métier & Matrice de Déclenchement

Conformément à `docs/02-points-a-clarifier.md` (PC-17), la liste initiale des événements notifiables et leurs canaux cibles par défaut est structurée comme suit :

| Événement déclencheur | Module source | Destinataire | Canaux par défaut | Contenu minimal | Arrêt / Fréquence |
|:---|:---:|:---:|:---:|:---|:---:|
| **Ticket créé** | Socle / Tous | Client | Portail + E-mail | N° ticket, intitulé, accusé de réception | Unique |
| **Complément requis** | Tous | Client | Portail + E-mail + WhatsApp | Motif de la demande, liste des pièces | Jusqu'à réponse client |
| **Dossier rejeté** | Tous | Client | Portail + E-mail | Motif du rejet obligatoire | Unique |
| **Facture disponible** | Billing Import/Export | Client | Portail + E-mail | Montant, référence, invitation paiement | Unique |
| **Paiement validé** | Billing Normal | Client | Portail + E-mail | Confirmation encaissement | Unique |
| **Documents finaux prêts (BS, BADT, BEE)** | Billing Office | Client | Portail + E-mail + WhatsApp | Confirmation mise à disposition | Unique |
| **Ticket transféré** | Socle | Client | Portail + E-mail | Information sur réorientation (TRF-05) | Unique |
| **Convocation photo/badge** | Enrôlement | Chauffeur/Transporteur | Portail + SMS + WhatsApp | Date/heure, lieu Gate, pièces physiques | Unique |
| **Badge disponible** | Enrôlement | Chauffeur/Transporteur | Portail + SMS + WhatsApp | Notification de mise à disposition badge | Unique |
| **Relance absence réponse** | Socle | Client | E-mail + WhatsApp | Rappel du délai restant avant clôture | Configurable (ex. J+3, J+7) |
| **Rendez-vous confirmé** | Continuité | Client | Portail + SMS + E-mail | Date, créneau, N° ticket, instructions | Unique + Rappel J-1 |

---

## 7. Continuité Présentielle & Analyse de CON (CON-01 à CON-04)

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                        PARCOURS DE CONTINUITÉ GUICHET (CON)                           │
├────────────────────────────────────────────────────────────────────────────────────────┤
│ 1. Client initie son dossier en ligne (ex. BL Import ou Demande d'Enrôlement)         │
│ 2. Étape nécessitant une présence physique (Paiement caisse physique, photo/badge,    │
│    ou choix client d'assistance au guichet IMP-15)                                     │
│ 3. Client réserve un créneau de Rendez-vous sur le portail (CON-03)                   │
│ 4. Arrivée au guichet CIT → L'agent scanne/saisit le N° de ticket ou BL (CON-02)     │
│ 5. L'agent accède instantanément à l'historique complet et poursuit l'instruction     │
│ 6. Validation sur place → Mise à jour en temps réel du statut sur le portail (CON-01)  │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

### Tableau d'Analyse des Situations Présentielles

| Situation métier | Acteur | Action présentielle | Trace sur le portail | Reprise digitale | Ambiguïté / Dépendance |
|:---|:---|:---|:---|:---|:---|
| **Bascule volontaire guichet (IMP-15)** | Client | Choix de finaliser au Billing Office | Statut "En attente passage guichet" | Agent reprend le dossier via le N° ticket | PC-10 (statut exact de bascule) |
| **Paiement Caisse physique** | Client & Caisse | Paiement en espèces/chèque au guichet | Agent Caisse valide la transaction | Ticket passe à "Payé confirmé" | Rôle Caisse/Finance (PC-03) |
| **Enrôlement Chauffeur (Photo/Badge)** | Chauffeur & Agent Gate | Prise de photo et remise physique badge | Agent Gate confirme la remise | Ticket clôturé et badge activé | Conforme F008 (`ConstaterRemiseBadge`) |
| **Recherche dossier au guichet** | Agent Guichet | Recherche par N° ticket, BL, booking | Consultation instantanée de la liasse | Reprise directe sans ressaisie | CON-02 (Recherche rapide) |

---

## 8. Mode Dégradé & Résilience (ERR-02, ERR-04)

1. **Isolation des pannes de canaux (ERR-02)** :
   - L'indisponibilité ou l'échec d'un canal tiers (ex. passerelle SMS en timeout) ne doit **jamais faire échouer la transaction métier principale** ni bloquer les autres canaux (ex. notification portail et email délivrés avec succès).
   - Les échecs d'envoi externe sont journalisés dans la table d'audit avec un statut d'erreur explicite.
2. **Mode dégradé en cas d'indisponibilité de la plateforme (ERR-04)** :
   - En cas d'incident réseau ou d'indisponibilité portail, le parcours physique au guichet reste opérationnel sur présentation des documents physiques ou du numéro de ticket initial.
   - Aucun mécanisme complexe de synchronisation offline bidirectionnelle n'est requis par le CDC (le principe P3 prescrit une plateforme autonome).

---

## 9. Sécurité, Données Personnelles et Cloisonnement

1. **Sécurité des liens de notification (`SEC-12`)** :
   - Tout lien transmis par email, SMS ou WhatsApp a la forme `https://plateforme.cit.ci/tickets/{ticketId}`.
   - **Interdiction formelle** de liens directs vers des fichiers PDF/pièces jointes protégées sans authentification préalable.
   - Si l'utilisateur n'est pas authentifié, redirection vers `/comptes/connexion?returnUrl=...`.
2. **Cloisonnement des notifications** :
   - Un utilisateur (Client ou Agent) ne peut consulter dans son centre de notifications que les notifications qui lui sont nommément adressées.
   - Les notes internes CIT (`MSG-02`) ne génèrent **jamais** de notification visible par le client.
3. **Données de contact & ARTCI (`SEC-06`, `PC-14`)** :
   - Les numéros de téléphone et adresses emails sont issus du profil utilisateur authentifié (`ApplicationUser`).
   - Aucune saisie ad-hoc de numéro tiers non contrôlé n'est permise pour l'envoi de notifications.

---

## 10. Analyse des Scénarios de Recette Contractuels (REC-SCE)

Le scénario contractuel **`REC-SCE-14`** (Section 22 du CDC) couvre expressément :
- *« Notifications sur chaque canal configuré avec lien vers le ticket »*
- *« Continuité digital/présentiel par recherche de ticket au guichet et rendez-vous donnant accès au bureau de facturation »*
- *« Contrôle des habilitations et sécurité des accès »*

---

## 11. Analyse des Points à Clarifier (PC)

| Point | Intitulé | Niveau CDC | Statut pour Spec Kit | Décision d'architecture recommandée |
|:---|:---|:---:|:---:|:---|
| **PC-16** | Souscription services WhatsApp/SMS | 🟠 CRITIQUE | **NON BLOQUANT SPEC** (Bloquant Production) | Définir les abstractions d'envoi + simulateur local en dev/test |
| **PC-17** | Liste initiale des événements et modèles | 🟡 IMPORTANT | **NON BLOQUANT SPEC** (Bloquant Recette) | Modèles et règles initiales éditables en base (templates par défaut) |
| **PC-10** | Statuts de bascule présentielle (IMP-15) | 🟡 IMPORTANT | **NON BLOQUANT SPEC** | Statut standard `EnAttenteRdv` ou `EnAttenteClient` |
| **PC-03** | Responsabilité "Agent de guichet" | 🔴 BLOQUANT | **RÉSOLU** | Porté par les agents habilités des files existantes (Billing / Gate) |
| **PC-25** | Bus / Dispatcher de notifications | Technique | **NON BLOQUANT SPEC** | Service applicatif `NotificationDispatcher` scoped |

---

## 12. Analyse du Découpage Fonctionnel

Trois options de découpage ont été rigoureusement évaluées :

### Option A : Feature 009 Unique (COM + NOT + CON)
- **Périmètre** : 17 exigences (11 Must, 4 Should, 1 Could + transverses).
- **Avantages** : Couvre l'intégralité du scénario REC-SCE-14 en un seul commit.
- **Inconvénients** : Périmètre hétérogène mélangeant passerelles de communication externe, centre de notifications in-app et module d'agenda / rendez-vous guichet. Risque de complexité accrue des tests.

### Option B : Découpage en 2 Features Ciblées (RECOMMANDÉ)
- **Feature 009 — Notifications Multicanales & Centre de Notifications In-App** :
  - *Exigences* : `COM-01` à `COM-06`, `NOT-01` à `NOT-03`, `SEC-12`, `ACR-01`, `ERR-02`.
  - *Must* : 8 | *Should* : 2 | *Could* : 1 (Total : 11 exigences).
  - *Périmètre technique* : Entité `Notification`, `NotificationDispatcher`, `IEmailService`, `ISmsService`, `IWhatsAppService`, centre de notifications portail (`HeaderBar` cloche + panneau), bloc « Mes actions requises » (`ACR-01`) sur le tableau de bord client.
- **Feature 010 — Continuité Digital / Présentiel & Prise de Rendez-vous** :
  - *Exigences* : `CON-01` à `CON-04`, `IMP-15`, `ERR-04`.
  - *Must* : 3 | *Should* : 2 (Total : 5 exigences).
  - *Périmètre technique* : Module `RendezVous` (créneaux, réservation client, confirmation), écran de recherche rapide guichet multi-critères (`CON-02`), documentation du mode dégradé physique.

### Option C : Notifications par canal séparé
- Découpage artificiel (ex. F009 Email, F010 SMS) : Rejeté car non conforme aux principes de cohésion du CDC.

> [!TIP]
> **Recommandation formelle : OPTION B**  
> Lancer la **Feature 009** recentrée sur **Notifications Multicanales & Centre de Notifications In-App** (`COM` + `NOT` + `SEC-12` + `ACR-01` + `ERR-02`), puis enchaîner sur la **Feature 010 — Continuité Digital/Présentiel & RDV** (`CON`).  
> *Note : Si le projet souhaite conserver l'intitulé combiné historique en une seule feature F009, l'Option A reste parfaitement réalisable et spécifiable.*

---

## 13. Risques et Mesures d'Atténuation

| Risque | Description | Impact | Mesure d'atténuation |
|:---|:---|:---:|:---|
| **RSQ-01 : Fournisseurs SMS/WhatsApp absents** | Blocage potentiel si des API tierces réelles sont exigées dès le dev | Moyen | Abstractions d'envoi injectables + implémentation mock / journalisée en BDD |
| **RSQ-02 : Spam / Boucle de notification** | Émission répétée de notifications sur un changement d'état | Élevé | Idempotence des notifications par événement (`EventId` unique) |
| **RSQ-03 : Fuite de données dans les liens** | Tokens ou données sensibles exposés dans les URLs d'emails/SMS | Critique | Liens d'accès au format standard `/tickets/{id}` avec authentification obligatoire (`SEC-12`) |
| **RSQ-04 : Lenteur d'envoi externe bloquant la requête** | Délai réseau d'un tiers ralentissant la validation d'un agent | Élevé | Exécution asynchrone / découplée des envois externes |

---

## 14. Décision Finale et Conditions d'Entrée

- **Périmètre fonctionnel** : Clairement délimité et sans zone d'ombre bloquante.
- **Acteurs et rôles** : 100% définis (`Client`, `Agent`, `Admin Fonctionnel`).
- **Événements déclencheurs** : 100% identifiés et traçables sur le socle `EvenementSocle`.
- **Canaux & Fournisseurs** : Architecture par abstraction évitant toute dépendance rigide à un prestataire externe.
- **Sécurité & Cloisonnement** : Règles `SEC-12` et `SEC-03` prêtes à être appliquées.
- **Aucun PC bloquant pour la spécification.**

---

## 15. Verdict Final

```
================================================================================
VERDICT PREFLIGHT : GO FOR /speckit.specify
================================================================================
FEATURE CANDIDATE : 009 — Notifications Multicanales & Centre In-App (COM / NOT)
                     [+ Continuité Présentielle CON si option regroupée A retenue]
STATUT : ÉLIGIBLE AU LANCEMENT DE SPÉCIFICATION
================================================================================
```
