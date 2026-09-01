# Spécification Fonctionnelle — Feature 009 (Notifications Multicanales & Centre de Notifications In-App)

> **Document** : `specs/009-notifications-multicanales/spec.md`
> **Feature** : F009 — Notifications Multicanales & Centre de Notifications In-App
> **Date** : 31 août 2026 · **Clarifiée le** : 1er septembre 2026
> **Statut** : **CLARIFIÉE** — passe `/speckit.clarify` exécutée (§25, Session 2026-09-01). `/speckit.plan` **non lancé** par ce document.
> **Découpage** : Option B du preflight `docs/13-preflight-feature-009.md`, validée — **CON-01→04 réservées à une Feature 010 distincte** (Continuité Guichet & Rendez-vous).
> **Couverture CDC actuelle** : 134/188 — **la projection après clôture complète de F009 n'est pas acquise et ne doit pas être annoncée comme telle avant clôture réelle** (§27).

---

## 1. Objectif

La Feature 009 implémente le module **Notifications Multicanales & Centre de Notifications In-App**. Elle permet à la plateforme d'informer proactivement le client (et les agents habilités) des événements significatifs du cycle de vie de leurs dossiers, via les canaux prévus par le CDC (portail, e-mail, messagerie mobile), avec un centre de notifications consultable dans le portail (état Lu/Non lu, accès direct au ticket), et un paramétrage des événements déclencheurs, des modèles de messages et des règles de diffusion par l'administrateur fonctionnel.

---

## 2. Périmètre Contractuel (Exigences CDC)

F009 couvre **exclusivement** les familles **COM** et **NOT**. Aucune exigence CON, AVE, IA, ou de hardening global n'est absorbée.

### COM-01 (Must) — Notification proactive multicanale
- **Texte CDC** : *« La plateforme notifie le client via : e-mail, notifications portail, et au moins un canal de messagerie mobile (WhatsApp et/ou SMS) »*
- **Acteur** : Client · **Dépendance** : aucune

### COM-02 (Must) — Lien vers le ticket
- **Texte CDC** : *« Chaque notification comporte un lien invitant le client à se connecter directement à son ticket (accès conditionnel à l'authentification — SEC-12) »*
- **Acteur** : Client · **Dépendance** : SEC-12 [E]

### COM-03 (Must) — Paramétrage des déclencheurs et modèles
- **Texte CDC** : *« Les événements déclencheurs et les modèles de messages sont paramétrables par l'administrateur fonctionnel (par module, par étape, par canal) »*
- **Acteur** : Admin fonctionnel · **Dépendance** : aucune

### COM-04 (Must) — Chat du ticket, canal d'échange de référence
- **Texte CDC** : *« Le chat/commentaires dans le ticket constitue le canal d'échange de référence entre le client et l'agent ; les messages sont horodatés et conservés. (Réaffirmation de SOC-05 dans le contexte des communications — un seul journal de messages par ticket) »*
- **Acteur** : Client · Agents · **Dépendance** : SOC-05 [E]
- **Note de périmètre** : mécanisme **déjà livré par F001** (SOC-05). Vérification faite : aucune feature fermée F001→F008 n'a jamais revendiqué l'identifiant COM-04. F009 la revendique donc **par vérification** du mécanisme existant et de son étanchéité vis-à-vis des notifications (§12), **sans développement d'un second journal de messages**.

### COM-05 (Should) — Préférences de canaux par client
- **Texte CDC** : *« Préférences de canaux par client (activation/désactivation par canal, dans le respect des notifications obligatoires) »*
- **Acteur** : Client · **Dépendance** : COM-01 [D]

### COM-06 (Could) — Canaux des relances automatiques
- **Texte CDC** : *« Relances automatiques paramétrables lorsque le ticket est en attente d'une action client au-delà d'un délai défini. (Précise la dimension canal de CYC-04 — le mécanisme de relances est Must via CYC-04 ; les canaux de notification des relances sont Could via COM-06) »*
- **Acteur** : Admin fonctionnel · **Dépendance** : CYC-04 [D]

### NOT-01 (Must) — État Lu/Non lu et accès au ticket
- **Texte CDC** : *« Chaque notification dispose d'un état Lu / Non lu et permet d'accéder directement au ticket concerné »*
- **Acteur** : Client · Agents · **Dépendance** : COM-01 [D]

### NOT-02 (Must) — Règles de diffusion paramétrables
- **Texte CDC** : *« Les règles de diffusion sont paramétrables par événement et par canal »*
- **Acteur** : Admin fonctionnel · **Dépendance** : COM-03 [E]

### NOT-03 (Should) — Indicateur permanent de non-lus
- **Texte CDC** : *« Un indicateur du nombre de notifications non lues est visible en permanence dans le portail »*
- **Acteur** : Client · Agents · **Dépendance** : NOT-01 [D]

**MUST : 6 (COM-01, COM-02, COM-03, COM-04, NOT-01, NOT-02). SHOULD : 2 (COM-05, NOT-03). COULD : 1 (COM-06). TOTAL : 9 exigences.**

---

## 3. Exigences Transverses — Statut et Impact

Ces quatre exigences sont **couplées** à F009 mais **ne sont pas comptées comme exigences de couverture apportées par F009** (§27) : trois sont des contraintes ou frontières, une relève de la Feature 010.

| Exigence transverse | Statut avant F009 | Impact F009 | Extension nécessaire | Propriétaire fonctionnel |
|:---|:---|:---|:---|:---|
| **SEC-12** (Must) — *« Aucun lien transmis par notification ne donne accès direct à un document protégé sans contrôle préalable d'identité et d'habilitations : le lien conduit au ticket après authentification »* | Principe transverse déjà appliqué par toutes les features fermées (aucun accès non authentifié n'existe dans le portail) ; **jamais exercé jusqu'ici sur un lien envoyé hors du portail**, puisqu'aucun canal externe n'existait | **CONTRAINTE structurante** : tout lien produit par F009 (e-mail, SMS, WhatsApp, notification in-app) doit conduire au ticket via le contrôle d'identité et d'habilitation, jamais à une ressource protégée directement | Aucune extension du modèle de sécurité — application du mécanisme existant au nouveau vecteur (le lien sortant). Le respect de SEC-12 devient un **critère d'acceptation de COM-02** | Sécurité & Navigation (socle F002/F003.5) |
| **ACR-01** (Must) — *« Le tableau de bord client met clairement en évidence les actions attendues de sa part (« Mes actions requises ») »* | **Déjà implémentée en F003.5** (bloc « Mes actions requises » du tableau de bord client) | **FRONTIÈRE conceptuelle à formaliser** (§11) : une action requise est un **état** dérivé du dossier ; une notification est un **événement** daté. F009 ne réimplémente ni ne remplace ACR-01 | **Aucune** — interdiction explicite de créer un second moteur d'actions requises. Une notification **peut** signaler qu'une action est requise, sans se substituer au bloc ACR-01 | Espace Client / UX (F003.5) |
| **ERR-02** (Must) — *« Échec d'envoi d'une notification : nouvelle tentative automatique et traçage ; l'échec d'un canal n'empêche pas les autres »* | Énoncée au CDC, **jamais applicable jusqu'ici** (aucun envoi externe n'existait) | **CONTRAINTE de résilience** devenant effective avec F009 : trois obligations explicites — (1) nouvelle tentative automatique, (2) traçage, (3) isolation inter-canaux | Le comportement est exigé par le CDC ; **aucune stratégie n'est spécifiée** (ni nombre de tentatives, ni délai, ni backoff, ni condition d'arrêt) — cf. CLAR-NOT-02, à ne pas inventer | Moteur de notification (F009) |
| **ERR-04** (Should) — *« Mode dégradé documenté : en cas d'indisponibilité du portail, le parcours physique reste possible ; la reprise s'effectue par le numéro de ticket »* | Énoncée au CDC, non traitée | **HORS PÉRIMÈTRE F009** — dépendance CON-01 [D], relève du parcours physique au guichet | Sans objet pour F009 | **Feature 010** (Continuité Guichet & Rendez-vous) |

---

## 4. COM-01 → COM-06 — Fiches Détaillées

*(Chaque champ non déterminé par le CDC est explicitement marqué « non spécifié » plutôt que comblé par une invention.)*

| Champ | **COM-01** (Must) | **COM-02** (Must) | **COM-03** (Must) |
|:---|:---|:---|:---|
| **Acteur** | Client (destinataire) | Client (destinataire) | Administrateur fonctionnel |
| **Événement** | Tout événement du workflow retenu comme déclencheur (liste paramétrable — COM-03/NOT-02, PC-17) | Émission de toute notification | Acte d'administration (création/modification d'un déclencheur ou d'un modèle) |
| **Émetteur** | Système (plateforme) | Système (plateforme) | Administrateur fonctionnel habilité |
| **Destinataire** | Client concerné par le dossier (§13) | Client concerné | Sans objet (acte de paramétrage) |
| **Contenu** | Information sur l'événement survenu + invitation à consulter — **contenu détaillé non figé par le CDC**, porté par les modèles paramétrables (COM-03) | Un lien conduisant au ticket concerné | Modèle de message par canal + désignation des événements déclencheurs |
| **Canal** | E-mail **et** portail **et** au moins un canal mobile (WhatsApp et/ou SMS) — cf. §9 | Tous les canaux de notification | Sans objet (paramétrage) |
| **Visibilité** | Destinataire uniquement (§14) | Destinataire uniquement | Administrateur fonctionnel habilité uniquement |
| **Déclenchement** | Automatique, à l'occurrence de l'événement métier | Automatique, inclus dans chaque notification émise | Manuel (acte d'administration) |
| **Lecture** | Voir NOT-01 pour le canal portail ; **non spécifié** pour les canaux externes (aucun accusé de lecture exigé) | Le clic sur le lien conduit au ticket après contrôle d'identité (SEC-12) | Sans objet |
| **Historique** | L'événement d'émission est tracé (§16) | Le lien émis fait partie de la notification tracée | Modification de paramétrage tracée (§16) |
| **Audit** | Émission tracée : destinataire, canal, horodatage, résultat (§16) | Sans trace supplémentaire propre | Acte d'administration tracé : auteur, objet, avant/après (§16) |
| **Priorité** | **Must** | **Must** | **Must** |
| **Dépendance** | — | SEC-12 [E] | — |

| Champ | **COM-04** (Must) | **COM-05** (Should) | **COM-06** (Could) |
|:---|:---|:---|:---|
| **Acteur** | Client · Agents habilités | Client | Administrateur fonctionnel (paramètre) · Client (destinataire) |
| **Événement** | Échange dans le fil du ticket | Modification de ses préférences par le client | Ticket en attente d'une action client au-delà du délai défini (CYC-04) |
| **Émetteur** | Client ou agent habilité | Client lui-même | Système (traitement de fond, §17) |
| **Destinataire** | L'autre partie du fil (client ↔ agents habilités) | Sans objet (auto-paramétrage) | Client dont l'action est attendue |
| **Contenu** | Message conversationnel horodaté | Choix d'activation/désactivation par canal | Rappel de l'action attendue — contenu porté par les modèles (COM-03) |
| **Canal** | Fil de messages du ticket (canal de référence) — **jamais un canal de notification** | Sans objet | Canaux configurés pour l'événement de relance |
| **Visibilité** | Client + agents habilités ; **notes internes jamais visibles du client** (MSG-02) | Client lui-même | Client destinataire uniquement |
| **Déclenchement** | Manuel (action utilisateur) | Manuel | Automatique, à l'atteinte du délai paramétré |
| **Lecture** | Conservé et consultable en permanence dans le ticket | Sans objet | Voir NOT-01 pour le canal portail |
| **Historique** | Conservé dans l'historique du ticket (SOC-05, mécanisme F001) | Modification de préférence tracée | Émission tracée comme toute notification |
| **Audit** | Déjà couvert par F001 — **aucun second journal** (§16) | Modification tracée (§16) | Émission tracée (§16) |
| **Priorité** | **Must** | **Should** | **Could** |
| **Dépendance** | SOC-05 [E] | COM-01 [D] | CYC-04 [D] |

---

## 5. NOT-01 → NOT-03 — Fiches Détaillées

| Champ | **NOT-01** (Must) | **NOT-02** (Must) | **NOT-03** (Should) |
|:---|:---|:---|:---|
| **Événement déclencheur** | Émission d'une notification à destination d'un utilisateur | Acte d'administration : définition d'une règle « événement → canaux » | Arrivée d'une notification non lue, ou changement d'état Lu/Non lu |
| **Destinataire** | Utilisateur authentifié destinataire (Client ou Agent) | Sans objet (paramétrage par l'Admin fonctionnel) | Utilisateur authentifié connecté au portail |
| **Canal attendu** | Portail (centre de notifications in-app) | Sans objet — la règle *détermine* les canaux des autres notifications | Portail (indicateur permanent) |
| **Automatique / manuel** | Création **automatique** ; passage à l'état Lu **manuel** (action de l'utilisateur) | **Manuel** (acte d'administration) | **Automatique** (reflet de l'état courant) |
| **Répétition** | Une notification par événement — **aucune répétition n'est prévue par le CDC** (l'insistance relève de COM-06/CYC-04, mécanisme distinct) | Sans objet | Sans objet (indicateur, pas un envoi) |
| **Arrêt** | La notification cesse d'être « non lue » lorsque l'utilisateur la marque comme lue (individuellement ou globalement) | Sans objet | L'indicateur retombe à zéro quand plus aucune notification n'est non lue |
| **Statut Lu / Non lu** | **Exigé explicitement** : `Non lu` à la création, `Lu` après action de l'utilisateur | Sans objet | Consommateur de ce statut : compte les `Non lu` |
| **Lien vers l'objet métier** | **Exigé explicitement** : « permet d'accéder directement au ticket concerné » — soumis à SEC-12 | Sans objet | Non exigé sur l'indicateur lui-même ; l'accès se fait via la liste (NOT-01) |
| **Audit** | Création et changement d'état tracés (§16) | Modification de règle tracée : auteur, règle, avant/après (§16) | Aucun (affichage dérivé, sans état propre) |
| **Priorité** | **Must** | **Must** | **Should** |
| **Dépendance** | COM-01 [D] | COM-03 [E] | NOT-01 [D] |

---

## 6. Centre de Notifications In-App

Le CDC demande explicitement ce centre. **Trois textes du CDC le fondent** — et non un seul :
1. **CDC §14.1, phrase introductive** : *« Le portail intègre un **centre de notifications regroupant les événements adressés à l'utilisateur** (par exemple : document complémentaire demandé, facture disponible, ticket transféré, documents de sortie disponibles, action requise, rendez-vous à confirmer) »* — le centre, son objet (**les événements adressés à l'utilisateur**) et sa nature de **collection** sont littéralement exigés, indépendamment de NOT-01.
2. **NOT-01** (état Lu/Non lu + accès direct au ticket) et **NOT-03** (indicateur permanent de non-lus).
3. **CDC §20, modèle de données minimal** : *« Notification (**canal, modèle, destinataire, statut d'envoi**) »* — quatre attributs exigés.

Le périmètre fonctionnel est donc **sourcé, pas inventé** :

| Capacité | Sourcée par | Retenue dans F009 |
|:---|:---|:---:|
| Liste des notifications de l'utilisateur | **CDC §14.1 (littéral)** : *« centre de notifications **regroupant** les événements adressés à l'utilisateur »* — la collection est exigée en toutes lettres, pas déduite de NOT-01 | ✅ |
| État Lu / Non lu par notification | NOT-01 (exigé littéralement) | ✅ |
| Date de la notification | Principe transverse d'horodatage (SOC-02/SOC-05, socle) | ✅ |
| Type / nature de l'événement | COM-03 (les modèles sont définis « par module, par étape ») · **CDC §20** (*« modèle »* est un attribut de la notification) — le type est nécessaire pour appliquer un modèle | ✅ |
| Titre / contenu | COM-03 (modèles de messages) · CDC §20 (*« modèle »*) | ✅ |
| Lien vers l'objet métier (ticket) | NOT-01 + COM-02 (exigés littéralement) | ✅ |
| Compteur de non-lus | NOT-03 (exigé littéralement) | ✅ |
| Marquer une notification comme lue | NOT-01 (un état Lu/Non lu implique une transition) | ✅ |
| Canal d'origine · destinataire · statut d'envoi | **CDC §20** (attributs exigés du modèle de données minimal) — **le statut d'envoi n'est pas nécessairement affiché à l'utilisateur** ; il est exigé comme donnée (§15) | ✅ (donnée) |
| « Tout marquer comme lu » | **Non exigé par le CDC** — commodité d'usage courante, retenue comme capacité optionnelle sans valeur contractuelle | ⚠️ Optionnelle, non-Must |

**Deux entrées de la liste illustrative du CDC §14.1 appellent une réserve explicite** :
- *« action requise »* — le centre **peut** porter une notification signalant qu'une action est attendue, **sans jamais devenir une seconde liste d'actions requises** (§11, règle ACR-01).
- *« rendez-vous à confirmer »* — **hors périmètre F009** : cet événement n'aura de source qu'après CON-03 (Feature 010, §23).

**Explicitement NON retenu** (absent du CDC, ne doit pas être inventé) : priorité/criticité technique d'une notification, catégories autres que le type d'événement paramétré, archivage automatique, suppression physique d'une notification (contraire au principe de conservation SOC-09), corbeille, épinglage, recherche plein texte.

---

## 7. Canal E-mail — Périmètre Exact

Le CDC impose l'e-mail au niveau du canal (COM-01, `docs/00-analyse-cdc.md` §6.4 : « E-mail | Must »). **Deux textes du CDC nomment en outre l'e-mail pour un événement précis** — SOC-01 et IMP-03 — et constituent donc des obligations d'événement, pas de simples propositions de paramétrage. Le tableau ci-dessous distingue rigoureusement ce qui est **imposé par un texte CDC nommant l'e-mail** de ce qui relève du **paramétrage** (COM-03/NOT-02, PC-17).

| Événement | Destinataire | E-mail obligatoire ? | Contenu minimal | Source CDC |
|:---|:---|:---|:---|:---|
| **Ticket créé — communication du numéro unique (tous modules)** | Client | **OUI — imposé** | Le numéro de ticket unique attribué | **SOC-01** (Must) : *« Toute demande soumise sur la plateforme, quel que soit le module, génère un ticket avec un numéro unique, communiqué au client (portail + e-mail) »* — `docs/cdc_text.txt` §8. Portée **globale**, tous modules |
| Réception des numéros de ticket (soumission IMPORT) | Client | **OUI — imposé** | Le ou les numéros de ticket attribués | **IMP-03** (Must) : *« Le client reçoit ses numéros de ticket sur son profil et par e-mail »* — **instanciation** de SOC-01 dans le module IMPORT, non un texte isolé |
| **Facture disponible** | Client | **OUI — notification imposée** ; **couple canal illustré** par NOT-02 (*« facture disponible → portail + e-mail »*) | Référence de la facture, invitation à consulter | **IMP-11** (Must) : *« édite la facture et la joint au ticket ; le client est notifié »* · CDC §14.1 (liste illustrative du centre de notifications) · NOT-02 (exemple littéral) |
| Demande de complément | Client | **Notification imposée** ; **couple canal illustré** par NOT-02 (*« complément demandé → portail + e-mail + WhatsApp »*) | Motif de la demande | **IMP-05** (Must, *« le client est notifié »*) · CDC §14.1 · NOT-02 (exemple littéral, **pas** une invention PC-17) |
| Transfert de ticket impactant le parcours | Client | **Notification imposée** ; canal **non nommé** par le CDC | Information de réorientation | **TRF-05** (Must, *« le client est automatiquement notifié »*) · CDC §14.1 (*« ticket transféré »*) · canaux : proposition PC-17 |
| Documents de sortie disponibles (BS, BADT, BEE) | Client | **Paramétrable** — canal non nommé | Confirmation de mise à disposition | CDC §14.1 (*« documents de sortie disponibles »*, exemple) · IMP-14 (dépôt des documents, **sans mention de notification**) · canaux : proposition PC-17 |
| Tout autre événement du workflow | Client | **Paramétrable** (par défaut proposé, non imposé) | Défini par le modèle applicable (COM-03) | COM-01 (l'e-mail est un canal Must ; l'affectation événement→canal est paramétrable — COM-03/NOT-02) |
| Décision Avoir/Exonération | Client | **Hors périmètre F009** | — | AVE-03 — Feature 007 (BLOCKED) |
| Étapes clés d'enrôlement | Transporteur/Chauffeur | Paramétrable — ENR-04 nomme *« SMS/WhatsApp prioritaires »*, pas l'e-mail | Étape atteinte | ENR-04 (Should, F008 — canal différé, cf. §24) |

> **Réponse à la question « l'e-mail est-il un canal Must global ou un Must sur certains événements ? » — les deux, à deux niveaux distincts** :
> 1. **Must global au niveau du canal** : l'e-mail doit exister et être servable (COM-01, `00-analyse-cdc.md` §6.4). Une plateforme incapable d'envoyer un e-mail ne satisfait pas COM-01.
> 2. **Must sur événement** pour les seuls événements dont un texte CDC nomme l'e-mail : **SOC-01** (numéro de ticket, tous modules) et **IMP-03** (son instanciation IMPORT). Pour tous les autres, l'affectation événement→e-mail est **paramétrable** (COM-03/NOT-02).

> **Aucune technologie d'envoi n'est choisie ici** : ni SMTP, ni SendGrid, ni Azure Communication Services, ni relais d'entreprise. La spécification décrit **le comportement attendu**, jamais le moyen technique (Constitution, Principe XI).

---

## 8. SMS / WhatsApp — Réexamen de PC-16

**Énoncé PC-16** (`docs/02-points-a-clarifier.md`) : 🟠 CRITIQUE · **Bloque : PRODUCTION** · Source CDC §25 · COM-01 · ENR-04. *« Le CDC §25 signale que l'envoi WhatsApp/SMS suppose la souscription à un service d'envoi et est à chiffrer. »*

**Vérification de la conclusion du preflight contre le CDC** — confirmée sur trois points indépendants :
1. **COM-01 formule un « et/ou »** : *« au moins un canal de messagerie mobile (WhatsApp **et/ou** SMS) »*. La **capacité mobile** est Must ; le **choix du canal mobile** (SMS, WhatsApp, ou les deux) n'est pas imposé par le CDC.
2. **PC-16 bloque la PRODUCTION, pas la spécification ni le plan** — classement officiel du document source, non réinterprété ici.
3. **La recommandation du document source est explicite** : *« Le démarrage sans WhatsApp/SMS est possible (e-mail + notifications portail) ; les canaux mobiles peuvent être ajoutés sans impact sur le workflow — RSQ-04. »*

**Décisions retenues pour F009** :
- Les canaux SMS et WhatsApp sont spécifiés comme **capacités configurables** : leur indisponibilité (souscription non réalisée) ne doit rendre aucun Must de F009 inatteignable.
- **Aucun fournisseur n'est choisi** — ni dans cette spec, ni implicitement.
- **Aucun mécanisme de repli SMS↔WhatsApp n'est inventé** : le CDC ne prévoit aucune substitution automatique entre les deux canaux mobiles.
- La **désactivation d'un canal mobile non souscrit** relève du même paramétrage que tout autre canal (NOT-02), sans traitement d'exception spécifique.

```
FOURNISSEUR SMS      : À DÉCIDER AVANT IMPLEMENTATION/PRODUCTION selon impact réel (PC-16)
FOURNISSEUR WHATSAPP : À DÉCIDER AVANT IMPLEMENTATION/PRODUCTION selon impact réel (PC-16)
FOURNISSEUR EMAIL    : À DÉCIDER AVANT IMPLEMENTATION/PRODUCTION (aucun fournisseur nommé par le CDC)
DÉCIDEUR ATTENDU     : DSI / Finance (fournisseur et budget) — PC-16, « Décision attendue »
```

**Niveau de blocage exact de PC-16 — décomposé par étape** (réponse à CLAR-COM-01) :

| Étape | Bloqué par PC-16 ? | Pourquoi |
|:---|:---:|:---|
| **`/speckit.plan`** | **NON** | La conception d'un canal ne dépend pas du choix de son fournisseur. COM-01 exige une **capacité** mobile, que la conception peut porter sans la souscrire |
| **IMPLEMENTATION** | **NON** | Le comportement attendu (émission, statut d'envoi, traçage, nouvelle tentative, isolation) se code indépendamment du prestataire. **Aucun fournisseur ne doit être codé en dur** (Constitution, Principe XI) |
| **RECETTE** | **OUI, partiellement** | Le volet *« notifications sur chaque canal configuré »* de REC-SCE-14 n'est rejouable **sur un canal mobile** que si ce canal est effectivement configuré. La recette reste **conduisible sur les canaux configurés** (portail + e-mail) sans faire échouer COM-01 |
| **PRODUCTION** | **OUI** | Classement officiel de PC-16 (🟠 CRITIQUE, *« Bloque : PRODUCTION »*), non réinterprété ici. Une mise en production revendiquant COM-01 complète suppose au moins un canal mobile souscrit |

> **Trois confirmations explicites, sans exception** : **aucun fournisseur n'est fixé** (SMS, WhatsApp, e-mail) ; **aucun fallback n'est fixé** (ni SMS↔WhatsApp, ni mobile→e-mail) ; **aucun choix technique n'est arrêté à ce stade** (ni protocole, ni connecteur, ni bibliothèque, ni format d'adressage).

---

## 9. Matrice des Canaux

| Canal | Exigences | Priorité | Obligatoire pour F009 ? | Décision fournisseur nécessaire ? |
|:---|:---|:---:|:---|:---|
| **In-App (portail)** | COM-01, COM-02, NOT-01, NOT-02, NOT-03 | **Must** | **OUI** — support obligatoire du centre de notifications et de l'état Lu/Non lu ; aucun tiers requis | **Non** — capacité interne à la plateforme |
| **E-mail** | COM-01, COM-02, NOT-02 ; IMP-03 (imposé sur un événement) | **Must** | **OUI** — canal Must du CDC (`00-analyse-cdc.md` §6.4) | **Oui**, mais aucun fournisseur nommé par le CDC → à décider avant implémentation/production |
| **SMS** | COM-01 (« et/ou »), NOT-02 ; ENR-04 (Should, F008) | **Must au niveau de la capacité mobile ; choix du canal non imposé** | **NON individuellement** — au moins un canal mobile est attendu, sans que le SMS soit désigné | **Oui** — PC-16, bloque la PRODUCTION |
| **WhatsApp** | COM-01 (« et/ou »), NOT-02 ; ENR-04 (Should, F008) | **Must au niveau de la capacité mobile ; choix du canal non imposé** | **NON individuellement** — même raison | **Oui** — PC-16, bloque la PRODUCTION |

**Canaux explicitement écartés** : **Push mobile** — absent du CDC, non ajouté. Aucun autre canal (courrier postal, appel vocal, fax, webhook client) n'est mentionné par le CDC ni introduit ici.

---

## 10. Déclencheurs Métier

**Principe directeur** : le CDC ne notifie **pas** chaque changement de statut par défaut. COM-01 vise « les événements du workflow » et COM-03/NOT-02 rendent cette liste **paramétrable** ; PC-17 constate explicitement que *« le CDC précise que les événements déclencheurs et modèles de messages sont paramétrables, mais ne fournit pas la liste initiale de configuration »*.

### 10.1 Trois niveaux de source — définitions

La reconstitution exacte des déclencheurs impose de ne pas confondre trois statuts probatoires très différents. Toute ligne de la matrice §10.2 porte l'un de ces trois niveaux :

| Niveau | Définition | Conséquence |
|:---|:---|:---|
| **CDC-EXIGENCE** | Une exigence numérotée du CDC **impose** qu'une notification soit émise sur cet événement | La notification est **obligatoire** ; ne pas l'émettre est un défaut de conformité. Le **canal** peut rester paramétrable si l'exigence ne le nomme pas |
| **CDC-EXEMPLE** | Le CDC **cite** l'événement (ou le couple événement→canaux) à titre d'illustration, introduit par *« par exemple »* — §14.1 et NOT-02 | Source **authentique mais non impérative** : elle démontre l'intention du CDC et fonde un défaut de paramétrage crédible, **sans devenir une règle globale** (Constitution, Principe XV) |
| **PC-17-PROPOSITION** | L'événement ne figure **que** dans la liste de départ proposée par `docs/02-points-a-clarifier.md` (PC-17) ou `docs/13-preflight-feature-009.md` | **Aucune valeur contractuelle** — jeu de départ soumis à validation métier |

> **Correction de fidélité apportée par cette passe de clarification** : la version antérieure du présent §10 classait « Ticket créé », « Facture disponible » et le couple « complément demandé → portail + e-mail + WhatsApp » en *proposition PC-17*. Vérification faite contre `docs/cdc_text.txt`, ces trois lignes sont **sourcées par le CDC lui-même** (SOC-01, IMP-11, NOT-02). Le classement est corrigé ci-dessous.

### 10.2 Matrice des déclencheurs (exploitable par `/speckit.plan`)

| Événement | Source CDC | Niveau de source | Destinataire | Canal | COM/NOT | Obligatoire ? |
|:---|:---|:---|:---|:---|:---|:---|
| **Ticket créé — numéro unique communiqué (tous modules)** | **SOC-01** (Must) : *« …génère un ticket avec un numéro unique, communiqué au client (portail + e-mail) »* | **CDC-EXIGENCE** | Client demandeur | **Portail + E-mail — canaux nommés par le CDC** | COM-01, COM-02 | **OUI** — événement **et** canaux imposés |
| Numéros de ticket attribués (soumission IMPORT) | **IMP-03** (Must) — instanciation de SOC-01 | **CDC-EXIGENCE** | Client demandeur | **Portail + E-mail — canaux nommés** | COM-01, COM-02 | **OUI** — événement **et** canaux imposés |
| **Demande de complément** | **IMP-05** (Must, *« le client est notifié »*) · CDC §14.1 (*« document complémentaire demandé »*) · **NOT-02** (*« complément demandé → portail + e-mail + WhatsApp »*) | **CDC-EXIGENCE** (événement) + **CDC-EXEMPLE** (canaux) | Client du dossier | Portail + E-mail + WhatsApp *(couple illustré par NOT-02 ; paramétrable)* | COM-01, COM-02, NOT-02 | **OUI** pour l'événement ; canaux paramétrables |
| **Facture disponible** | **IMP-11** (Must, *« le client est notifié »*) · CDC §14.1 · **NOT-02** (*« facture disponible → portail + e-mail »*) | **CDC-EXIGENCE** (événement) + **CDC-EXEMPLE** (canaux) | Client du dossier | Portail + E-mail *(couple illustré par NOT-02 ; paramétrable)* | COM-01, COM-02, NOT-02 | **OUI** pour l'événement ; canaux paramétrables |
| **Transfert de ticket impactant le parcours** | **TRF-05** (Must, *« automatiquement notifié »*) · CDC §14.1 (*« ticket transféré »*) | **CDC-EXIGENCE** (événement) ; canaux **non nommés** | Client du dossier | *Proposition PC-17 : Portail + E-mail* | COM-01, COM-02, NOT-02 | **OUI** pour l'événement ; canaux à valider |
| **Décision Avoir / Exonération** | **AVE-03** (Must, *« notifiés au client »*) | **CDC-EXIGENCE** | Client du dossier | Non nommé | COM-01, COM-02 | **OUI au CDC — mais HORS PÉRIMÈTRE F009** : dépend de la Feature 007 (BLOCKED). Aucun événement source n'existe |
| Documents de sortie disponibles (BS, BADT, BEE) | CDC §14.1 (*« documents de sortie disponibles »*) — IMP-14 décrit le dépôt **sans** mentionner de notification | **CDC-EXEMPLE** | Client du dossier | *Proposition PC-17 : Portail + E-mail + WhatsApp* | COM-01, NOT-02 | Non imposé — **fortement suggéré** par §14.1 |
| Action requise sur le dossier | CDC §14.1 (*« action requise »*) | **CDC-EXEMPLE** | Client du dossier | *Non nommé* | COM-01, NOT-02 | Non imposé — **et soumis à la règle de non-duplication ACR-01 (§11)** : une notification peut signaler l'action, elle ne remplace pas le bloc « Mes actions requises » |
| **Mention @Nom dans une note interne** | **MSG-04** (Should, socle) : *« la personne mentionnée reçoit une notification dans la plateforme et, selon paramétrage, un e-mail avec lien vers le ticket »* | **CDC-EXIGENCE** (Should) | **Le collaborateur nommément mentionné** — jamais la file, jamais le client | Portail (nommé) + E-mail (*« selon paramétrage »*) | COM-01, COM-02, NOT-01 | **Should — exigence propriété de F001** (FR-025), **non recomptée** par F009. **Raccordement In-App livré par F009** (Option B, §13.1) ; e-mail conditionné au paramétrage MSG-04 |
| **Escalade sur dépassement de SLA** | **ALE-03** (Must) : *« escalade (notification d'un superviseur…) »* · **ALE-02** (Must) : *« destinataires et canaux paramétrables »* | **CDC-EXIGENCE** (Must) | **Superviseur** du périmètre concerné | Paramétrable (ALE-02) | — | **Must — exigence propriété de F001** (FR-051→053), **non recomptée** par F009. **Aucun raccordement livré dans cette feature** : F009 ne fournit que le **point d'intégration** (Option B, §13.1) |
| Relance pour absence de réponse | **CYC-04** (mécanisme, Must) + **COM-06** (canal, Could) | **CDC-EXIGENCE** (mécanisme) ; canaux **Could** | Client dont l'action est attendue | *Proposition PC-17 : E-mail + WhatsApp* | COM-06 | Mécanisme Must (CYC-04, F001) ; **dimension canal Could** (COM-06, F009) |
| Ticket rejeté (avec motif) | Aucune exigence CDC ne mentionne de notification (SOC-06 impose le motif, pas la notification) ; `docs/00-analyse-cdc.md` §6.9 cite *« Rejeté → motif obligatoire + notification client »* — **analyse dérivée, pas texte CDC** | **PC-17-PROPOSITION** | Client du dossier | *Portail + E-mail* | COM-01, NOT-02 | Non imposé |
| Preuve de paiement reçue | *Proposition PC-17* — IMP-12/IMP-13 ne mentionnent aucune notification | **PC-17-PROPOSITION** | Client du dossier | *Portail + E-mail* | COM-01, NOT-02 | Non imposé |
| Paiement validé | *Proposition PC-17* — IMP-13 ne mentionne aucune notification | **PC-17-PROPOSITION** | Client du dossier | *Portail + E-mail* | COM-01, NOT-02 | Non imposé |
| Ticket clôturé | *Proposition PC-17* — SOC-09 impose l'archivage, **pas** une notification | **PC-17-PROPOSITION** | Client du dossier | *Portail + E-mail* | COM-01, NOT-02 | Non imposé |
| Dossier d'enrôlement validé | **ENR-04** (Should, F008) : *« SMS/WhatsApp prioritaires »* | **CDC-EXIGENCE** (Should, autre feature) | Transporteur / Chauffeur | *SMS + WhatsApp* | COM-01, NOT-02 | **Différée** — F008 a tracé ENR-04 comme non livrée ; **et sans coordonnée disponible** (§22, CLAR-COM-04) |
| Convocation photo / badge | **ENR-04** (Should, F008) · **ENR-03** (Must — trace la convocation, **sans** exiger de notification) | **CDC-EXIGENCE** (Should, autre feature) | Transporteur / Chauffeur | *SMS + WhatsApp* | COM-01, NOT-02 | **Différée** — mêmes réserves |
| Rendez-vous confirmé / à confirmer | CDC §14.1 (*« rendez-vous à confirmer »*) · **NOT-02** (*« rendez-vous → portail + SMS »*) | **CDC-EXEMPLE** | Client | *Portail + SMS* | — | **HORS F009** — dépend de CON-03 (Feature 010). Aucun événement source n'existe avant F010 |

**Lecture obligatoire de cette matrice** :
- **Aucune règle de diffusion n'est figée par cette spécification.** L'ensemble du tableau reste administrable (COM-03, NOT-02).
- **5 événements client relèvent du niveau CDC-EXIGENCE et sont dans le périmètre F009** : ticket créé (SOC-01), numéros de ticket IMPORT (IMP-03), demande de complément (IMP-05), facture disponible (IMP-11), transfert impactant (TRF-05). Un sixième, AVE-03, est sourcé mais hors périmètre (F007 BLOCKED).
- **Seuls SOC-01 et IMP-03 nomment leurs canaux** (portail + e-mail). Pour IMP-05, IMP-11 et TRF-05, l'événement est imposé et le canal est paramétrable — NOT-02 en illustre deux.
- Les lignes **PC-17-PROPOSITION** constituent un **jeu de départ à valider par le métier**, jamais une règle acquise (cf. CLAR-COM-02).
- **Aucun « notifier tout changement de statut » n'est spécifié** : les statuts sont visibles en temps réel par SOC-08, ce qui ne crée aucune obligation de notification.

---

## 11. Frontière ACR-01 ↔ Notification

| Dimension | **ACR-01 — Action requise** (déjà livrée en F003.5) | **Notification** (F009) |
|:---|:---|:---|
| Nature | **État** courant dérivé du dossier | **Événement** daté, survenu à un instant précis |
| Question posée | « Qu'attend-on de moi *maintenant* ? » | « Que s'est-il passé sur mon dossier ? » |
| Cycle de vie | Disparaît quand l'action est accomplie | Persiste après lecture (historique conservé) |
| Source | Calculée à partir de l'état des tickets (SOC-08) | Produite à l'occurrence d'un événement déclencheur |
| Emplacement | Bloc « Mes actions requises » du tableau de bord client | Centre de notifications (NOT-01) + canaux externes |
| État | Pas d'état Lu/Non lu | État Lu/Non lu **exigé** (NOT-01) |

**Règles de non-duplication** :
- F009 **ne recalcule ni ne remplace** le bloc « Mes actions requises » — aucun second moteur d'actions requises n'est créé.
- Un événement rendant une action nécessaire **peut** produire une notification ; les deux vues restent **complémentaires et indépendantes**.
- Marquer une notification comme lue **ne fait jamais disparaître** l'action requise correspondante : seule la réalisation effective de l'action la retire du bloc ACR-01.

---

## 12. Frontière Messages F001 ↔ Notifications

| Dimension | **Message métier** (SOC-05 / COM-04, socle F001) | **Notification** (F009) |
|:---|:---|:---|
| Nature | Contenu **conversationnel** rédigé par un humain | **Signal événementiel** produit par le système |
| Rattachement | Attaché à un ticket, membre de son fil | Adressé à un **utilisateur**, référence un ticket |
| Auteur | Client ou agent habilité | Système (jamais un humain directement) |
| Bidirectionnalité | Oui — le destinataire peut répondre dans le fil | Non — une notification ne se répond pas |
| État | Pas d'état Lu/Non lu (COM-04/SOC-05 n'en prévoient aucun) | État Lu/Non lu **exigé** (NOT-01) |
| Confidentialité | Peut être une **note interne** jamais visible du client (MSG-02) | **Jamais** produite à partir d'une note interne à destination d'un client (§14) |

**Règles de non-confusion** :
- Le fil de messages du ticket reste **le canal d'échange de référence** (COM-04) et **le journal unique** des échanges (SOC-05) — F009 n'en crée pas un second.
- Une notification **peut signaler** qu'un nouveau message est arrivé, sans **être** ce message ni le dupliquer.
- Le CDC traite ces deux concepts dans des exigences distinctes (COM-04 d'une part, NOT-01/NOT-02 d'autre part) : ils ne doivent pas être fusionnés. **Le choix du modèle de données technique n'est pas décidé ici** — il relève de `/speckit.plan`.

---

## 13. Destinataires

Les destinataires sont **toujours dérivés du contexte métier**, jamais saisis librement.

| Type de destinataire | Sourcé par | Portée |
|:---|:---|:---|
| **Utilisateur client** | COM-01, COM-02, NOT-01 (*« Client »*) | Le client concerné par le dossier notifié |
| **Société cliente** | CPT-04 / SEC-01 (cloisonnement société, socle F002) | Les collaborateurs habilités à consulter le dossier, selon leur droit de consultation existant |
| **Agent habilité** | NOT-01, COM-04 (*« Client · Agents »*) | Les agents dont les habilitations couvrent le dossier ou la file concernée |
| **Agent nommément mentionné** | **MSG-04** (Should, socle) : *« la personne mentionnée reçoit une notification dans la plateforme et, selon paramétrage, un e-mail avec lien vers le ticket »* | Le collaborateur mentionné, **individuellement** — seul texte du CDC nommant un destinataire agent d'une notification de type COM/NOT. **Destinataire effectivement servi par F009** (raccordement In-App, §13.1) |
| **Superviseur** | **ALE-03** (Must) : *« escalade (notification d'un superviseur…) »* · **ALE-02** (Must) : *« destinataires et canaux paramétrables »* | Le superviseur du périmètre concerné, désigné **par le paramétrage d'alerte** — mécanisme ALE, propriété de F001. **Destinataire admissible mais non servi par F009 dans cette feature** : point d'intégration seul (§13.1) |
| **Groupe / file** | **Non prévu explicitement par le CDC** comme destinataire de notification — une file est une destination de ticket, pas un destinataire nommé. Le CDC §20 modélise `Notification (canal, modèle, **destinataire**, statut d'envoi)` — destinataire **au singulier** | Non retenu comme destinataire direct ; cf. CLAR-NOT-04 |
| **Administrateur fonctionnel** | COM-03, NOT-02 — **acteur du paramétrage**, pas destinataire désigné de notifications métier | Non retenu comme destinataire de notifications de workflow |

**Interdictions formelles** :
- **Aucune saisie libre** d'une adresse e-mail ou d'un numéro de téléphone destinataire par un utilisateur, pour aucun événement. Toute notification est adressée à une identité connue de la plateforme.
- **Aucun mécanisme de transfert** (« envoyer aussi à… ») permettant de sortir du modèle de cloisonnement.
- Un utilisateur **ne peut pas désigner** un tiers comme destinataire de ses notifications.
- **Aucun broadcast global** : il n'existe aucun événement « notifier tous les agents », « notifier tous les clients », ni « notifier une file ». Le CDC ne prévoit aucun destinataire collectif. **Moindre privilège** : un agent n'est destinataire que lorsqu'un texte CDC ou une règle paramétrée le désigne, jamais par défaut.

### 13.1 Événements notifiant un agent — frontière avec F001 (CLAR-NOT-01 · PC-25)

> **DÉCISION VALIDÉE — Option B (1er septembre 2026).** F009 fournit la **capacité générique de notification adressable** aux clients **et aux agents**, conformément à NOT-01 (*« Client · Agents »*). Sur les deux mécanismes internes sourcés par le CDC, cette capacité est **raccordée pour MSG-04** et **non raccordée pour ALE-03**, qui reçoit un simple point d'intégration.

Deux textes du CDC — **et deux seulement** — nomment une notification à destination d'un acteur interne :

| Source CDC | Événement | Destinataire nommé | Canal nommé | Propriétaire de l'exigence | Statut pour F009 — **Option B** |
|:---|:---|:---|:---|:---|:---|
| **MSG-04** (Should) | Mention `@Nom` dans une note interne | **Le collaborateur mentionné**, individuellement | Portail (nommé) + e-mail *« selon paramétrage »* | **F001** — `FR-025`, couverture recette *« Absente »* | **RACCORDEMENT LIVRÉ PAR F009.** Lorsqu'un collaborateur est mentionné par `@Nom`, le moteur F009 **produit une notification In-App** au collaborateur concerné. **L'e-mail associé reste conditionné au paramétrage prévu par MSG-04.** L'exigence MSG-04 **demeure la propriété de F001** et **n'est pas recomptée** dans les 9 exigences COM/NOT de F009 (§27) — F009 en réalise uniquement le **raccordement notification manquant** |
| **ALE-03** (Must) + **ALE-02** (Must) | Dépassement de SLA → escalade | **Un superviseur** ; ALE-02 rend *« destinataires et canaux »* paramétrables | Paramétrable | **F001** — `FR-051`→`FR-053`, couverture recette REC-SCE-12/13 | **HORS PÉRIMÈTRE DE RACCORDEMENT F009.** F009 fournit **seulement le contrat / point d'intégration** permettant au mécanisme SLA existant de produire **ultérieurement** une notification au superviseur. **Aucun workflow SLA n'est réimplémenté**, aucune règle d'escalade n'est redéfinie, et **aucune notification de supervision n'est émise par F009 dans cette feature** |

**Portée exacte du raccordement MSG-04 livré par F009** :

| Volet | Livré par F009 ? | Précision |
|:---|:---:|:---|
| Détection de la mention `@Nom` dans une note interne | **NON** | Mécanisme F001 (`FR-025`) — F009 en **consomme** l'événement, ne le réimplémente pas |
| Émission d'une **notification In-App** au collaborateur mentionné | **OUI** | Capacité générique NOT-01 appliquée à un destinataire agent |
| État Lu/Non lu, compteur, lien vers le ticket sur cette notification | **OUI** | NOT-01 et NOT-03 s'appliquent sans exception au destinataire agent |
| **E-mail** au collaborateur mentionné | **CONDITIONNEL** | Émis **si et seulement si** le paramétrage prévu par MSG-04 (*« selon paramétrage »*) l'active — F009 fournit le canal, **jamais la décision de l'activer par défaut** |
| Non-réaffectation du ticket sur mention | **NON — et invariant à préserver** | **MSG-05** (Must, F001) : *« une mention n'entraîne pas de réaffectation »*. Le raccordement F009 **ne doit produire aucun effet de workflow** |
| Étanchéité de la note interne vis-à-vis du client | **OUI — invariant renforcé** | **MSG-02** : la note interne n'est jamais visible du client. La notification produite est **exclusivement** destinée au collaborateur mentionné (§14, règle 3) |

**Portée exacte du point d'intégration ALE-03 fourni par F009** :

| Volet | Livré par F009 ? | Précision |
|:---|:---:|:---|
| Contrat permettant à un mécanisme tiers de demander l'émission d'une notification à un destinataire nommé | **OUI** | Point d'intégration générique, **non spécifique au SLA** — il sert MSG-04 comme il servira ALE-03 |
| Détection du dépassement de SLA, règles de seuil, choix du superviseur destinataire | **NON** | Mécanisme F001 (`FR-051`→`FR-053`, ALE-01/02/03) — **non réimplémenté, non modifié** |
| Émission effective d'une notification d'escalade au superviseur | **NON — différé** | Aucune notification de supervision n'est produite par F009. Le raccordement effectif relèvera d'une décision de périmètre ultérieure, en coordination avec **PC-25** |
| Comptage d'ALE-01/02/03 dans la couverture F009 | **NON** | Ces exigences restent acquises par F001 et **ne sont pas recomptées** (§27) |

**Ce que le CDC ne dit pas** (et que F009 n'invente pas) : aucun autre événement agent n'est sourcé. En particulier, **ni** « ticket arrivé dans ma file », **ni** « ticket réaffecté vers moi », **ni** « nouveau message client sur mon ticket », **ni** « SLA bientôt dépassé sur mon ticket » ne sont des événements de notification exigés par le CDC. Ils sont couverts fonctionnellement par les vues temps réel du socle (SUP-01, SUP-02, SOC-04) — une vue n'est pas une notification.

**Point à clarifier rattaché — PC-25** (`docs/02-points-a-clarifier.md`, 🟡 IMPORTANT, source MSG-04 · ALE-01 · SUP-01) : *« Les préférences de canal et les règles de notification des agents ne sont pas décrites »*. Ce point est le propriétaire officiel de CLAR-NOT-01.

> **Divergence de sources signalée, non résolue par F009** : `docs/02-points-a-clarifier.md` classe PC-25 « Bloque : SPECIFICATION » et l'intitule *« Notifications internes CIT (agents) »* ; `docs/13-preflight-feature-009.md` §11 le réintitule *« Bus / Dispatcher de notifications »* et le qualifie de « Technique ». **La source de vérité est `docs/02-points-a-clarifier.md`** (Constitution, Principe II) : PC-25 est un point **métier** sur les règles de notification interne, pas un point d'architecture. Le preflight est ici en écart ; il n'est pas modifié par cette passe, mais son libellé ne doit pas être repris.
>
> **Pourquoi PC-25 ne bloque pas `/speckit.plan` malgré son classement « SPECIFICATION »** : le mécanisme réclamé est entièrement défini par le CDC — NOT-02 rend les règles paramétrables *« par événement et par canal »*, ALE-02 rend *« destinataires et canaux »* paramétrables, et §13 fixe les types de destinataires admissibles. Ce qui manque est la **liste initiale de configuration**, c'est-à-dire une donnée d'administration, pas une règle de conception. Ce raisonnement est identique à celui déjà appliqué par F001, qui a classé PC-25 « NON BLOQUANTE POUR PLAN / À CONFIRMER AVANT IMPLEMENTATION, en coordination avec la future feature Notifications » (`specs/001-socle-tickets-workflows/spec.md`).

---

## 14. Sécurité et Cloisonnement

Règles formalisées (toutes assorties de critères d'acceptation en §20) :

1. **Cloisonnement du destinataire** : une notification n'est consultable, marquable comme lue, et comptée dans l'indicateur **que par son destinataire**. Aucun autre utilisateur ne peut y accéder, y compris un administrateur fonctionnel dans le cadre de son paramétrage.
2. **Aucune fuite inter-sociétés** : un client d'une société ne voit jamais une notification relative au dossier d'une autre société (CPT-04, SEC-01 — principes transverses déjà appliqués).
3. **Étanchéité des notes internes** : une note interne (MSG-02) **ne produit jamais** de notification à destination d'un compte client. Un événement interne (échange entre agents, mention interne) ne peut produire qu'une notification vers un agent habilité.
4. **Liens soumis aux mêmes autorisations serveur (SEC-12)** : le lien contenu dans une notification conduit au ticket ; l'accès est refusé ou différé jusqu'à contrôle d'identité et d'habilitation. **Aucun jeton d'accès contournant les habilitations** n'est introduit, sur aucun canal.
5. **Consultation d'une notification tierce impossible** : la connaissance de l'identifiant d'une notification ne suffit jamais à y accéder (anti-IDOR — principe déjà appliqué par toutes les features fermées).
6. **Contenu minimal et non excessif** : le contenu transmis sur un canal externe (e-mail, SMS, WhatsApp) reste limité à l'information nécessaire pour informer et inviter à consulter. Il ne doit contenir **ni pièce jointe protégée, ni donnée confidentielle du dossier** au-delà de ce que le modèle paramétré exige — le détail se consulte dans le portail, après authentification (SEC-12).

---

## 15. Erreurs d'Envoi — ERR-02 (et ERR-04)

**Texte exact ERR-02 (Must)** : *« Échec d'envoi d'une notification : nouvelle tentative automatique et traçage ; l'échec d'un canal n'empêche pas les autres »*

| Question | Réponse **sourcée** | Statut |
|:---|:---|:---|
| Retry exigé ? | **OUI** — *« nouvelle tentative automatique »* | Exigé par le CDC |
| Journalisation exigée ? | **OUI** — *« et traçage »* | Exigé par le CDC |
| Isolation entre canaux exigée ? | **OUI** — *« l'échec d'un canal n'empêche pas les autres »* | Exigé par le CDC |
| État « en échec » exigé ? | **OUI — SOURCÉ**, et non simplement déduit : le CDC §20 (*« Modèle de données minimal »*) impose de modéliser *« **Notification (canal, modèle, destinataire, statut d'envoi)** »*. Le **statut d'envoi** est donc un attribut exigé, distinct par canal | **Exigé par le CDC** (correction apportée par cette passe de clarification : la version antérieure le classait « Déduit ») |
| Nombre de tentatives ? | **NON SPÉCIFIÉ** par le CDC | À paramétrer — cf. CLAR-NOT-02 |
| Délai / cadence entre tentatives ? | **NON SPÉCIFIÉ** | À paramétrer — cf. CLAR-NOT-02 |
| Stratégie exponentielle / backoff ? | **NON SPÉCIFIÉ — ne pas inventer** | Explicitement écarté de cette spec |
| Condition d'arrêt du retry ? | **NON SPÉCIFIÉ** | À paramétrer — cf. CLAR-NOT-02 |
| Fallback vers un autre canal ? | **NON PRÉVU** par le CDC — l'isolation exigée signifie que les autres canaux ne sont pas empêchés, **pas** qu'un canal en supplée un autre | Explicitement écarté |
| Intervention humaine exigée ? | **NON SPÉCIFIÉ** — aucune exigence de reprise manuelle ni d'alerte à un administrateur | Non retenu |
| Message d'erreur à l'utilisateur final ? | **NON SPÉCIFIÉ** — le CDC n'exige pas d'informer le destinataire de l'échec d'un canal | Non retenu |

**Conséquence structurante** : l'échec d'un canal externe **ne doit jamais faire échouer la transaction métier** qui l'a déclenchée (une validation d'agent reste valide même si l'e-mail part en erreur) — lecture directe de l'obligation d'isolation.

**Ce qui est donc arrêté pour `/speckit.plan`** (comportement, sans valeur inventée) :

| Élément | Statut | Sourcé par |
|:---|:---|:---|
| Une notification porte un **statut d'envoi par canal** | **ARRÊTÉ** | CDC §20 (modèle de données minimal) |
| L'échec d'un canal est **tracé** | **ARRÊTÉ** | ERR-02 (*« et traçage »*) · SOC-13 |
| Une **nouvelle tentative automatique** est engagée après échec | **ARRÊTÉ** | ERR-02 (*« nouvelle tentative automatique »*) |
| L'échec d'un canal **n'empêche pas** les autres canaux | **ARRÊTÉ** | ERR-02 (littéral) |
| L'échec d'un canal **n'invalide pas** la transaction métier | **ARRÊTÉ** (conséquence directe de l'isolation) | ERR-02 |
| Nombre de tentatives · cadence · condition d'arrêt | **PARAMÈTRE, valeur non déterminée** — la capacité de configuration est retenue, **aucune valeur par défaut n'est proposée par cette spec** | Silence du CDC — CLAR-NOT-02 |
| Backoff exponentiel · fallback inter-canaux · alerte administrateur · message d'échec au destinataire | **ÉCARTÉS** — non prévus par le CDC, ne pas introduire | Silence du CDC |

> **Règle de gouvernance appliquée (Constitution, Principe XV)** : le CDC exige une nouvelle tentative **sans en fixer la valeur**. La valeur reste donc **configurable et à confirmer avant implémentation** ; aucune valeur par défaut chiffrée n'est écrite dans cette spécification, et aucune ne doit être introduite par `/speckit.plan` sans validation métier/DSI.
>
> **Couverture de recette d'ERR-02** : `docs/04-matrice-recette.md` ligne 230 indique littéralement *« ERR-02 | Aucun | **Absente** »*. ERR-02 est un Must **rendu effectif par F009** et **sans aucun scénario contractuel** — il reçoit donc un TEST-SPEC obligatoire au même titre que NOT-01 et NOT-02 (cf. §18 bis, `TEST-ERR-02`).

**ERR-04 (Should)** : *« Mode dégradé documenté : en cas d'indisponibilité du portail, le parcours physique reste possible ; la reprise s'effectue par le numéro de ticket »* — dépendance CON-01 [D]. **Hors périmètre F009**, réservée à la **Feature 010**. Aucune disposition de mode dégradé physique n'est spécifiée ici.

---

## 16. Audit

Réutilisation conceptuelle du journal existant (SOC-13, socle F001) — **aucun second journal d'audit n'est créé**.

| Événement à auditer | Exigé par | Retenu |
|:---|:---|:---:|
| Notification créée | SOC-13 (traçabilité des actions du système) · ERR-02 (le traçage suppose un point de départ) | ✅ |
| Notification envoyée sur un canal | **ERR-02** (*« traçage »*) — nécessaire pour distinguer succès et échec | ✅ |
| Échec d'envoi sur un canal | **ERR-02** (littéral) | ✅ |
| Notification lue (changement d'état) | NOT-01 (l'état est une donnée métier dont la transition est traçable) | ✅ |
| Modification d'un modèle de message | COM-03 (acte d'administration) · SOC-13 | ✅ |
| Modification d'une règle de diffusion | NOT-02 (acte d'administration) · SOC-13 | ✅ |
| Modification des préférences client | COM-05 (acte utilisateur ayant un effet sur la diffusion) | ✅ |
| Contenu intégral du message envoyé | **Non exigé** — et potentiellement porteur de données personnelles (Constitution, Principe XIV) | ❌ Non retenu |

---

## 17. Besoins de Traitement de Fond

F009 suppose des traitements non déclenchés par une action utilisateur directe. Ils sont décrits ici **comme besoins fonctionnels uniquement** :

- **Émission différée / asynchrone** : l'envoi sur un canal externe ne doit pas retarder ni bloquer la transaction métier (conséquence de l'isolation ERR-02, §15).
- **Nouvelle tentative après échec** : reprise automatique des envois en échec (ERR-02), selon un paramétrage à définir (CLAR-NOT-02).
- **Relance sur absence de réponse client** : émission d'un rappel au terme du délai d'attente (CYC-04 pour le mécanisme, COM-06 pour la dimension canal).
- **Surveillance des échéances** : si des notifications d'échéance/SLA sont retenues au paramétrage, elles supposent une évaluation périodique des délais.

> **Rectification factuelle (1er septembre 2026)** — la version antérieure de ce paragraphe indiquait que les traitements de fond du socle étaient *« présents dans l'arbre de travail »* mais *« non commités »*. **Vérification Git faite** (`git ls-tree -r HEAD`) : `RelancesAbsenceReponseService`, `SurveillanceSlaService` et `DistributionAutomatiqueService` **sont présents dans `HEAD`**. Ce sont donc des **capacités techniques existantes du socle**, et non des fichiers de travail au statut incertain.
>
> **Ce que ce constat n'implique pas.** Ces services **ne satisfont aucune exigence COM ou NOT** : ils couvrent des besoins voisins (relances CYC-04, surveillance SLA, distribution d'affectation), et **aucun d'eux n'émet de notification**. Leur existence ne réduit ni le périmètre de F009, ni le nombre d'exigences qu'il apporte (§27).
>
> **Aucun composant technique n'est désigné ici comme implémentation retenue.** Leur réutilisation, adaptation ou remplacement relève de la conception arrêtée par `research.md` et `plan.md` — cette spécification ne préjuge d'aucune de ces options.

---

## 18. Scénario Contractuel : REC-SCE-14

> **Intitulé** : Transverse
> **Description exacte (`docs/04-matrice-recette.md`, ligne 45 — non modifiée)** : *« Notifications sur chaque canal configuré avec lien vers le ticket ; continuité digital/présentiel par recherche de ticket au guichet et rendez-vous donnant accès au bureau de facturation ; regroupement des tickets d'une même demande en dossier ; contrôle des habilitations (un client ne voit pas les tickets d'un autre ; un agent ne voit pas les files des autres rôles) ; journal d'audit »*
> **Exigences Must couvertes (texte exact)** : COM-01 · COM-02 · COM-03 · COM-04 · CON-01 · CON-02 · CON-03 · SOC-11 · SOC-13 · CPT-04 · SEC-01 · SEC-02 · SEC-03 · SEC-04 · SEC-05 · SEC-07 · SEC-10 · SEC-11 · SEC-12 · ACR-01
> **Should/Could couvertes** : COM-05 · NOT-03
> **Préconditions (texte exact)** : Canaux configurés · Comptes de test (client A, client B, agent rôle 1, agent rôle 2)
> **Acteurs (texte exact)** : Client · Agent · Superviseur · Admin

| Volet de REC-SCE-14 | COM/NOT concernés | Acteur | Résultat attendu | Couvrable par F009 ? |
|:---|:---|:---|:---|:---|
| *« Notifications sur chaque canal configuré »* | COM-01, NOT-02, COM-05 | Client · Admin | Chaque canal activé pour l'événement reçoit effectivement la notification | ✅ **OUI** |
| *« …avec lien vers le ticket »* | COM-02 (+ contrainte SEC-12) | Client | Le lien conduit au ticket après contrôle d'identité | ✅ **OUI** |
| *« continuité digital/présentiel par recherche de ticket au guichet »* | — (CON-01, CON-02) | Agent Guichet | Reprise du dossier au guichet | ❌ **NON — Feature 010** |
| *« …et rendez-vous donnant accès au bureau de facturation »* | — (CON-03) | Client | Prise de rendez-vous rattachée au ticket | ❌ **NON — Feature 010** |
| *« regroupement des tickets d'une même demande en dossier »* | — (SOC-11) | Client | Dossier regroupant les tickets | ✅ Déjà acquis (F001), non re-livré par F009 |
| *« contrôle des habilitations (un client ne voit pas les tickets d'un autre… ) »* | Appliqué aux notifications (§14) | Client A / Client B / Agents | Aucune fuite inter-comptes ni inter-files | ✅ **OUI** pour le volet notifications |
| *« journal d'audit »* | SOC-13 appliqué aux notifications (§16) | Superviseur / Admin | Émissions, échecs et actes d'administration tracés | ✅ **OUI** pour le volet notifications |

**Conclusion de couverture — REC-SCE-14 : PARTIEL pour F009.**
Le scénario est **intrinsèquement multi-features** : deux de ses volets (recherche guichet, rendez-vous) reposent sur CON-01/CON-02/CON-03, formellement exclues du périmètre. **REC-SCE-14 ne pourra être déclaré entièrement rejoué qu'après la Feature 010.** Aucune étape de ce scénario n'est renommée, renumérotée ou inventée par cette spécification.

### 18.1 Répartition F009 / F010 après clarification

Le CDC **ne numérote aucune étape** dans REC-SCE-14 : il énonce cinq volets séparés par des points-virgules. Les « étapes » ci-dessous sont donc désignées **par leur citation littérale**, jamais par un numéro inventé.

| Étape (citation littérale de `docs/04-matrice-recette.md` ligne 45) | F009 / F010 | Exigence | Statut après clarification |
|:---|:---:|:---|:---|
| *« Notifications sur chaque canal configuré… »* | **F009** | COM-01 · COM-03 · NOT-02 · COM-05 | **CLARIFIÉ** — canaux arrêtés (§9), déclencheurs sourcés et hiérarchisés (§10.2), règles de diffusion paramétrables (NOT-02). **Réserve d'exécution** : le volet « chaque canal configuré » n'est rejouable en recette sur un canal mobile que si PC-16 est tranché (§8) — sinon la recette porte sur les canaux effectivement configurés (portail + e-mail), sans faire échouer COM-01 |
| *« …avec lien vers le ticket »* | **F009** | COM-02 (+ contrainte SEC-12) | **CLARIFIÉ** — le lien conduit au ticket après contrôle d'identité ; critères d'acceptation §20 (US3), y compris le cas de l'objet devenu inaccessible |
| *« continuité digital/présentiel par recherche de ticket au guichet »* | **F010** | CON-01 · CON-02 | **HORS PÉRIMÈTRE F009** — aucune disposition dans cette spec. Non clôturable par F009 |
| *« …et rendez-vous donnant accès au bureau de facturation »* | **F010** | CON-03 | **HORS PÉRIMÈTRE F009** — la notification « rendez-vous confirmé » (CDC §14.1, NOT-02) reste sans événement source tant que CON-03 n'existe pas (§23) |
| *« regroupement des tickets d'une même demande en dossier »* | **F001** (acquis) | SOC-11 | **DÉJÀ ACQUIS** — non re-livré par F009, non recompté (§27) |
| *« contrôle des habilitations (un client ne voit pas les tickets d'un autre ; un agent ne voit pas les files des autres rôles) »* | **F009** pour le volet notifications ; **F001/F002** pour le volet tickets/files | CPT-04 · SEC-01→SEC-05 · SEC-07 · SEC-10→SEC-12 | **CLARIFIÉ pour le volet notifications** — cloisonnement par destinataire et inter-sociétés formalisés (§14) et testables (§20). Le volet tickets/files est acquis par les features fermées |
| *« journal d'audit »* | **F009** pour le volet notifications ; **F001** pour le journal lui-même | SOC-13 | **CLARIFIÉ pour le volet notifications** — événements auditables arrêtés (§16), **aucun second journal créé** |
| Préconditions : *« Canaux configurés »* | **F009** | COM-03 · NOT-02 | **CLARIFIÉ sous réserve** — « canaux configurés » dépend de CLAR-COM-01 (quels canaux mobiles) et CLAR-COM-02 (quels événements). La précondition est **satisfaisable sans canal mobile** (RSQ-04) |
| Préconditions : *« Comptes de test (client A, client B, agent rôle 1, agent rôle 2) »* | **F009** | — | **CLARIFIÉ** — jeu de comptes déjà existant (`docs/09-comptes-test-development.md`, `docs/10-comptes-test-cit.md`) |
| Acteurs : *« Client · Agent · Superviseur · Admin »* | **F009 partiellement** | — | **PARTIEL** — Client et Admin pleinement exercés par F009 ; **Agent effectivement exercé comme destinataire** via le raccordement MSG-04 (§13.1, Option B) ; **Superviseur** n'est exercé par REC-SCE-14 qu'au travers du journal d'audit — les notifications de supervision relèvent d'ALE-03, **non raccordée dans cette feature** (§13.1) |

**Verdict de répartition** : **7 volets sur 10** relèvent de F009 (dont 2 partagés avec les features fermées), **2 relèvent strictement de F010**, **1 est déjà acquis**. **REC-SCE-14 reste donc PARTIEL pour F009 après clarification** — cette passe ne modifie pas ce verdict, elle en précise la frontière.

---

## 18 bis. TEST-SPEC — Scénarios de substitution aux REC-SCE manquants

Trois exigences **Must** exercées ou rendues effectives par F009 n'ont **aucune** couverture de recette contractuelle (constat littéral de `docs/04-matrice-recette.md`), et une exigence **Could** n'est couverte par aucun REC-SCE. Elles reçoivent donc des scénarios TEST-SPEC explicites, **traçables à leur exigence**, qui ne remplacent aucun scénario CDC et n'en modifient aucun.

### TEST-NOT-01 — État Lu / Non lu et accès direct au ticket

- **Exigence tracée** : **NOT-01 (Must)** — *« Chaque notification dispose d'un état Lu / Non lu et permet d'accéder directement au ticket concerné »*
- **Justification** : `docs/04-matrice-recette.md` ligne 201 — *« NOT-01 | Aucun | **Absente** »*
- **Exigences associées exercées** : NOT-03 (Should, indicateur) · COM-02 (lien) · SEC-12 (contrôle d'accès du lien) · CPT-04 / SEC-01 (cloisonnement)

| # | Given | When | Then |
|:--|:---|:---|:---|
| **1 — Nominal** | Un utilisateur authentifié (Client ou Agent habilité) est destinataire de N notifications, dont M non lues | Il consulte son centre de notifications | Les N notifications sont listées, chacune avec son **état Lu/Non lu**, sa **date**, son **type**, son **titre** et un **lien vers le ticket concerné** ; l'indicateur permanent affiche exactement **M** |
| **2 — Marquage** | Une notification à l'état `Non lu` | L'utilisateur la marque comme lue | Son état passe à `Lu`, l'indicateur passe à **M−1**, et la transition est tracée au journal d'audit |
| **3 — Idempotence** | Une notification déjà à l'état `Lu` | L'utilisateur la consulte à nouveau ou tente de la marquer comme lue | Elle reste consultable, son état demeure `Lu`, **l'indicateur n'est pas modifié**, et **aucune erreur n'est présentée** |
| **4 — Accès au ticket** | Une notification référençant le ticket T, destinataire authentifié et habilité sur T | Il active le lien de la notification | Il accède au ticket T ; **aucun accès direct à une pièce protégée n'est servi** (SEC-12) |
| **5 — Non authentifié** | Un visiteur non authentifié | Il tente d'accéder au centre de notifications ou active un lien de notification | L'accès est refusé et il est conduit au contrôle d'identité ; **aucun détail du dossier n'est exposé** |
| **6 — Cloisonnement destinataire (anti-IDOR)** | Une notification adressée à l'utilisateur **B**, dont l'utilisateur **A** connaît l'identifiant | A tente de la consulter ou de la marquer comme lue | L'action est **refusée côté serveur** ; la notification n'apparaît **ni dans la liste de A, ni dans son compteur** ; l'existence de la notification n'est pas divulguée |
| **7 — Cloisonnement inter-sociétés** | Deux clients rattachés à des sociétés différentes | L'un consulte son centre de notifications | **Aucune** notification relative au dossier de l'autre société n'y figure (CPT-04, SEC-01) |
| **8 — Objet devenu inaccessible** | Une notification dont le ticket associé n'est plus accessible au destinataire (habilitation retirée, rattachement modifié) | Il active le lien | L'accès est refusé proprement, **sans divulguer** l'existence, le contenu ni l'appartenance du ticket |

### TEST-NOT-02 — Règles de diffusion paramétrables par événement et par canal

- **Exigence tracée** : **NOT-02 (Must)** — *« Les règles de diffusion sont paramétrables par événement et par canal »*
- **Justification** : `docs/04-matrice-recette.md` ligne 202 — *« NOT-02 | Aucun | **Absente** »*
- **Exigences associées exercées** : COM-03 (Must, modèles et déclencheurs paramétrables) · COM-05 (Should, préférences client)

| # | Given | When | Then |
|:--|:---|:---|:---|
| **1 — Nominal, ajout de canal** | Un administrateur fonctionnel habilité ; un événement E diffusé sur le seul canal Portail | Il ajoute le canal E-mail à la règle de diffusion de E, puis l'événement E survient | La notification est émise **sur les deux canaux**, **sans redéploiement** ni intervention technique |
| **2 — Nominal, retrait de canal** | Un événement E diffusé sur Portail + E-mail | L'administrateur retire E-mail, puis E survient | La notification est émise **sur le seul canal Portail** ; aucun e-mail n'est produit |
| **3 — Non-rétroactivité** | Des notifications déjà émises pour l'événement E | L'administrateur modifie la règle de diffusion de E | Les notifications **déjà émises ne sont pas modifiées** ; la nouvelle règle s'applique **aux événements survenant ensuite** |
| **4 — Granularité par événement** | Deux événements E1 et E2 avec des règles distinctes | E1 puis E2 surviennent | Chacun est diffusé **selon sa propre règle** ; modifier E1 n'affecte pas E2 |
| **5 — Autorisation** | Un utilisateur **sans** habilitation d'administration des notifications | Il tente de consulter ou de modifier le paramétrage des règles de diffusion | L'action est **refusée côté serveur**, indépendamment de ce que l'interface affiche |
| **6 — Traçabilité** | Une règle de diffusion existante | Un administrateur habilité la modifie | L'acte est tracé au journal d'audit avec **auteur, règle concernée, valeur avant / après** (SOC-13) |
| **7 — Préférence client vs notification obligatoire** | Un client ayant désactivé le canal E-mail (COM-05) ; un événement **non obligatoire** et un événement **qualifié d'obligatoire** au paramétrage | Les deux événements surviennent | Le premier **n'est pas** transmis par e-mail ; le second **l'est malgré** la désactivation |
| **8 — Canal non configuré / non souscrit** | Un canal mobile présent au paramétrage mais **non souscrit** (PC-16 non tranché) | Un événement dont la règle référence ce canal survient | Les **autres canaux aboutissent normalement** ; le canal indisponible est **tracé** avec son statut d'envoi ; **aucun Must de F009 n'est rendu inatteignable** |

### TEST-ERR-02 — Échec d'envoi : nouvelle tentative, traçage, isolation

- **Exigence tracée** : **ERR-02 (Must)** — *« Échec d'envoi d'une notification : nouvelle tentative automatique et traçage ; l'échec d'un canal n'empêche pas les autres »*
- **Justification** : `docs/04-matrice-recette.md` ligne 230 — *« ERR-02 | Aucun | **Absente** »*. Exigence Must **rendue effective par F009** (§3), sans aucun scénario contractuel
- **Réserve** : ce scénario vérifie **le comportement exigé**, jamais des valeurs de retry — celles-ci restent ouvertes (CLAR-NOT-02)

| # | Given | When | Then |
|:--|:---|:---|:---|
| **1 — Isolation inter-canaux** | Un événement diffusé sur trois canaux, dont un est indisponible | L'événement survient | Les **deux canaux disponibles aboutissent** ; l'échec du troisième **ne les empêche pas** |
| **2 — Statut d'envoi par canal** | Le même envoi multi-canal | Après traitement | Chaque canal porte un **statut d'envoi distinct** (CDC §20), permettant de distinguer succès et échec canal par canal |
| **3 — Traçage** | Un envoi en échec sur un canal | L'échec survient | L'échec est **tracé** (destinataire, canal, horodatage, résultat) ; **le contenu intégral du message n'est pas journalisé** (§16) |
| **4 — Nouvelle tentative automatique** | Un envoi en échec sur un canal | Le mécanisme de reprise s'exécute | Une **nouvelle tentative est engagée automatiquement**, sans action humaine. *Le nombre, la cadence et la condition d'arrêt sont lus depuis le paramétrage — le test ne fige aucune valeur* |
| **5 — Préservation de la transaction métier** | Un agent valide une étape déclenchant une notification ; le canal externe échoue | La validation est enregistrée | **La transaction métier reste valide et committée** ; l'échec de notification ne la fait **jamais** échouer ni ne la déroule |
| **6 — Absence de fallback** | Un canal mobile en échec, un canal e-mail disponible | L'échec survient | **Aucune substitution automatique** n'est opérée vers un autre canal — l'isolation exigée n'est pas une suppléance (§15) |

### TEST-MSG-04 — Raccordement de la mention `@Nom` au centre de notifications

- **Capacité tracée** : **NOT-01 (Must, F009)** appliquée à un destinataire **agent** — c'est la capacité générique de F009 qui est démontrée ici
- **Exigence raccordée** : **MSG-04 (Should)** — *« la personne mentionnée reçoit une notification dans la plateforme et, selon paramétrage, un e-mail avec lien vers le ticket »*. **MSG-04 demeure la propriété de F001 et n'est pas recomptée dans les 9 exigences de F009** (§27) ; F009 en livre le **raccordement notification manquant** (§13.1, Option B)
- **Justification** : `specs/001-socle-tickets-workflows/spec.md` trace MSG-04 → `FR-025` avec une couverture recette *« Absente »*. Aucun scénario contractuel n'exerce ce raccordement
- **Invariants F001 à ne pas casser** : **MSG-05** (une mention n'entraîne aucune réaffectation) · **MSG-02** (la note interne n'est jamais visible du client)

| # | Given | When | Then |
|:--|:---|:---|:---|
| **1 — Nominal (test exigé)** | Un ticket, un agent auteur, et un collaborateur habilité `C` ; le canal In-App est actif | L'auteur publie une **note interne** mentionnant `@C` | **Une notification In-App est produite pour `C`**, à l'état `Non lu`, portant la date, le type, un titre et **un lien vers le ticket** ; le compteur de non-lus de `C` est incrémenté |
| **2 — E-mail conditionnel** | Le même contexte, le paramétrage e-mail de la mention étant **désactivé**, puis **activé** | La mention est publiée dans chacun des deux cas | Cas désactivé : **aucun e-mail** n'est émis, la notification In-App reste produite. Cas activé : un e-mail est émis **en plus**, avec lien vers le ticket. **F009 n'active jamais ce canal par défaut** |
| **3 — Destinataire unique** | Un ticket suivi par plusieurs agents habilités et par un client | Une note interne mentionne `@C` | **Seul `C`** reçoit la notification. Les autres agents habilités **ne la reçoivent pas** (moindre privilège, §13) |
| **4 — Étanchéité client (MSG-02)** | Le même contexte | La note interne mentionnant `@C` est publiée | **Aucune notification n'est produite vers le client**, sur aucun canal ; le contenu de la note interne n'apparaît dans aucune notification destinée au client |
| **5 — Non-réaffectation (MSG-05)** | Un ticket affecté à l'opérateur `O` | Une note interne mentionne `@C` (≠ `O`) | Le ticket **reste affecté à `O`** ; le raccordement notification **ne produit aucun effet de workflow** |
| **6 — Non-duplication du fil** | La note interne mentionnant `@C` | On examine le ticket et le centre de notifications | La note reste **dans le fil unique du ticket** (SOC-05 / COM-04) ; la notification la **signale sans la dupliquer** (§12) |
| **7 — Cloisonnement de lecture** | La notification produite pour `C` | Un autre agent `A` tente d'y accéder, y compris en connaissant son identifiant | L'accès est **refusé côté serveur** ; elle n'apparaît ni dans la liste ni dans le compteur de `A` |
| **8 — Absence de raccordement ALE-03** | Un dépassement de SLA déclenchant une escalade selon les règles F001 | L'escalade s'exécute | **Aucune notification de supervision n'est produite par F009** dans cette feature ; le mécanisme d'escalade F001 s'exécute normalement et **n'est pas modifié** (§13.1) |

### TEST-COM-06 — Canaux de notification des relances automatiques

- **Exigence tracée** : **COM-06 (Could)** — *« Relances automatiques paramétrables lorsque le ticket est en attente d'une action client au-delà d'un délai défini »* — dimension **canal** de CYC-04
- **Justification** : COM-06 est **absente de REC-SCE-14** et de tout autre scénario contractuel
- **Frontière** : le **mécanisme** de relance est **CYC-04 (Must), propriété de F001**. TEST-COM-06 n'exerce **que la dimension canal** ; il ne re-teste ni le déclenchement, ni la cadence des relances, ni la suggestion de clôture
- **Conditionnalité** : ce scénario n'est exigible que si COM-06 est retenue au périmètre effectif ; si COM-06 est différée (précédent ENR-04/ENR-05 en F008), l'absence de ce test est une **dette tracée**, pas une omission

| # | Given | When | Then |
|:--|:---|:---|:---|
| **1 — Nominal** | Un ticket en attente d'une action client au-delà du délai paramétré (CYC-04) ; des canaux configurés pour l'événement de relance | Le mécanisme de relance de F001 s'exécute | Une notification de rappel est émise **sur les canaux configurés pour cet événement**, et l'émission est **tracée** |
| **2 — Paramétrage des canaux** | L'administrateur modifie les canaux associés à l'événement de relance | Une relance ultérieure s'exécute | Elle emprunte **les nouveaux canaux**, sans redéploiement |
| **3 — Non-substitution au mécanisme CYC-04** | Le mécanisme de relance de F001 | On examine F009 | F009 **ne crée aucun second mécanisme de relance** : il fournit la dimension canal d'un déclenchement produit par F001 |
| **4 — Aucun canal configuré** | L'événement de relance sans aucun canal actif | Le mécanisme de relance s'exécute | Le mécanisme CYC-04 s'exécute **normalement** (suggestion de clôture au terme des relances) ; l'absence de canal de notification **ne le bloque pas** |

**Exigences de F009 sans couverture REC-SCE — TEST-SPEC obligatoire** (constat littéral de `docs/04-matrice-recette.md`) :

| Exigence | Priorité | Couverture recette CDC | Conséquence | TEST-SPEC |
|:---|:---:|:---|:---|:---|
| **NOT-01** | **Must** | *« Aucun \| **Absente** »* (ligne 201) | **TEST-SPEC obligatoire** — aucun scénario contractuel n'exerce l'état Lu/Non lu | **`TEST-NOT-01`** (§18 bis) — 8 cas |
| **NOT-02** | **Must** | *« Aucun \| **Absente** »* (ligne 202) | **TEST-SPEC obligatoire** — aucun scénario contractuel n'exerce le paramétrage des règles de diffusion | **`TEST-NOT-02`** (§18 bis) — 8 cas |
| **ERR-02** | **Must** | *« Aucun \| **Absente** »* (ligne 230) | **TEST-SPEC obligatoire** — Must **rendu effectif par F009** (§3), sans aucun scénario contractuel. *Constat ajouté par la passe de clarification : cette ligne manquait au tableau* | **`TEST-ERR-02`** (§18 bis) — 6 cas |
| **COM-06** | Could | Absent de REC-SCE-14 | TEST-SPEC si l'exigence est retenue au périmètre effectif | **`TEST-COM-06`** (§18 bis) — 4 cas, conditionnel |
| COM-03 | Must | REC-SCE-14 — **Partielle** | Complément TEST-SPEC recommandé | Couvert par `TEST-NOT-02` (cas 1→6) |
| COM-04 | Must | REC-SCE-01 · REC-SCE-14 — **Partielle** | Vérification de non-régression suffisante (mécanisme F001 déjà éprouvé) | Aucun TEST-SPEC nouveau — §20 US6 suffit |
| *(raccordement)* **MSG-04** | Should — **propriété F001** | *« Absente »* (F001, FR-025) | **TEST-SPEC obligatoire** — F009 livre le **raccordement In-App** de la mention `@Nom` (§13.1, Option B) sans recompter l'exigence | **`TEST-MSG-04`** (§18 bis) — 8 cas |

---

## 19. User Stories

- **US1 — Consulter mes notifications** (Must, NOT-01, NOT-03) : en tant qu'utilisateur authentifié (Client ou Agent habilité), je consulte la liste de mes notifications avec leur état Lu/Non lu et un indicateur permanent du nombre de non-lues, afin de suivre l'évolution de mes dossiers.
- **US2 — Être notifié d'un événement métier** (Must, COM-01, COM-02) : en tant que client, je suis notifié sur les canaux configurés lorsqu'un événement significatif survient sur mon dossier, avec un lien me conduisant au ticket concerné.
- **US3 — Accéder au ticket depuis une notification en toute sécurité** (Must, COM-02 + contrainte SEC-12) : en tant que client, le lien reçu me conduit au ticket après contrôle de mon identité, sans jamais exposer une ressource protégée à un tiers non authentifié.
- **US4 — Gérer l'état lu/non lu** (Must, NOT-01, NOT-03) : en tant qu'utilisateur, je marque une notification comme lue et vois l'indicateur se mettre à jour, afin de distinguer ce que j'ai déjà traité.
- **US5 — Administrer les déclencheurs, modèles et règles de diffusion** (Must, COM-03, NOT-02) : en tant qu'administrateur fonctionnel, je définis quels événements notifient, avec quel modèle de message et sur quels canaux, sans développement.
- **US6 — Vérifier l'étanchéité du fil de messages** (Must, COM-04) : en tant que client ou agent, j'échange dans le fil du ticket, qui reste le canal de référence horodaté et conservé, distinct du flux de notifications.
- **US7 — Choisir mes canaux de notification** (Should, COM-05) : en tant que client, j'active ou désactive des canaux pour les notifications non obligatoires, sans pouvoir me soustraire aux notifications obligatoires.
- **US8 — Être relancé en cas d'absence de réponse** (Could, COM-06) : en tant que client, je reçois un rappel sur les canaux configurés lorsque mon dossier attend une action de ma part au-delà du délai défini.
- **US9 — Être notifié lorsqu'un collègue me mentionne** (Must au titre de NOT-01 ; **raccordement de MSG-04, exigence propriété de F001**) : en tant que collaborateur CIT mentionné par `@Nom` dans une note interne, je reçois une notification In-App me conduisant au ticket concerné, afin de ne pas manquer une sollicitation d'un collègue. *Cette User Story démontre la capacité générique de F009 sur un destinataire **agent** ; elle ne fait pas de MSG-04 une exigence de F009 (§13.1, §27).*

---

## 20. Critères d'Acceptation (Given/When/Then)

### US1 / US4 — Centre de notifications, état Lu/Non lu (NOT-01, NOT-03)
- **Nominal** : Given un utilisateur authentifié destinataire de notifications non lues, When il consulte son centre de notifications, Then chacune apparaît avec son état, sa date, son type, son titre et un lien vers le ticket concerné, et l'indicateur affiche le nombre exact de non-lues.
- **Marquage** : Given une notification non lue, When l'utilisateur la marque comme lue, Then son état passe à « Lu » et l'indicateur de non-lues décroît en conséquence.
- **Notification déjà lue** : Given une notification déjà à l'état « Lu », When l'utilisateur la consulte à nouveau ou tente de la marquer comme lue, Then elle reste consultable, son état demeure « Lu », l'indicateur n'est pas modifié, et **aucune erreur n'est présentée**.
- **Non autorisé** : Given un utilisateur non authentifié, When il tente d'accéder au centre de notifications, Then l'accès est refusé et il est invité à s'authentifier.
- **Cloisonnement (autre destinataire)** : Given une notification adressée à l'utilisateur B, When l'utilisateur A tente d'y accéder ou de la marquer comme lue — y compris en en connaissant l'identifiant — Then l'action est refusée côté serveur et la notification n'apparaît jamais dans sa liste ni dans son compteur.
- **Cloisonnement (autre société)** : Given deux clients de sociétés différentes, When l'un consulte son centre de notifications, Then aucune notification relative au dossier de l'autre société n'y figure.

### US2 / US3 — Émission et lien (COM-01, COM-02, SEC-12)
- **Nominal** : Given un événement déclencheur configuré pour un dossier, When l'événement survient, Then une notification est émise vers le destinataire sur chacun des canaux activés pour cet événement, et chacune comporte un lien vers le ticket concerné.
- **Sécurité du lien (SEC-12)** : Given un lien de notification reçu sur un canal externe, When une personne non authentifiée l'ouvre, Then elle est conduite au contrôle d'identité et **aucun détail du dossier ni aucune pièce protégée n'est exposé** avant authentification et vérification des habilitations.
- **Lien vers un objet devenu inaccessible** : Given une notification dont le ticket associé n'est plus accessible au destinataire (habilitation retirée, rattachement modifié), When il ouvre le lien, Then l'accès est refusé proprement avec un message explicite, **sans divulguer** l'existence, le contenu ni l'appartenance du ticket.
- **Canal indisponible (ERR-02)** : Given un événement diffusé sur plusieurs canaux dont l'un est indisponible, When la notification est émise, Then les autres canaux aboutissent normalement, l'échec est tracé, une nouvelle tentative est engagée automatiquement, et **la transaction métier à l'origine de l'événement reste valide**.
- **Donnée interne confidentielle** : Given une note interne échangée entre agents sur un ticket, When elle est publiée, Then **aucune notification n'est émise vers le client**, sur aucun canal.

### US5 — Administration (COM-03, NOT-02)
- **Nominal** : Given un administrateur fonctionnel habilité, When il modifie le modèle de message ou les canaux associés à un événement, Then le paramétrage est enregistré et s'applique aux événements survenant ensuite, sans redéploiement.
- **Autorisation** : Given un utilisateur sans habilitation d'administration des notifications, When il tente d'accéder au paramétrage ou de le modifier, Then l'action est refusée **côté serveur**, indépendamment de ce que l'interface affiche.
- **Traçabilité** : Given une modification de règle de diffusion ou de modèle, When elle est enregistrée, Then l'acte est tracé avec son auteur et la nature du changement.

### US6 — Étanchéité du fil de messages (COM-04)
- **Nominal** : Given un ticket existant, When un client et un agent échangent dans le fil, Then les messages sont horodatés, conservés et consultables dans le ticket.
- **Non-duplication** : Given le fil de messages d'un ticket, When on examine le mécanisme de notification, Then il n'existe **qu'un seul journal de messages par ticket** et les notifications n'en constituent pas une copie.

### US7 — Préférences (COM-05, Should)
- **Nominal** : Given un client, When il désactive un canal pour les notifications non obligatoires, Then les notifications concernées ne lui sont plus transmises sur ce canal.
- **Notification obligatoire** : Given une notification qualifiée d'obligatoire au paramétrage, When elle est émise, Then elle est transmise **malgré** la désactivation du canal par le client.

### US8 — Relances (COM-06, Could)
- **Nominal** : Given un ticket en attente d'une action client au-delà du délai paramétré, When le mécanisme de relance s'exécute, Then une notification de rappel est émise sur les canaux configurés et l'émission est tracée.

### US9 — Raccordement de la mention `@Nom` (NOT-01 appliquée à un agent ; MSG-04 propriété de F001)
- **Nominal** : Given un collaborateur habilité `C` et un ticket, When un agent publie une note interne mentionnant `@C`, Then **une notification In-App est produite pour `C`** à l'état « Non lu », avec date, type, titre et **lien vers le ticket**, et le compteur de non-lus de `C` est incrémenté.
- **E-mail conditionnel** : Given le paramétrage e-mail de la mention désactivé, When la mention est publiée, Then **aucun e-mail n'est émis** et la notification In-App reste produite ; Given ce même paramétrage activé, Then un e-mail est émis **en plus**. F009 **n'active jamais ce canal par défaut**.
- **Destinataire unique** : Given plusieurs agents habilités sur le ticket, When `@C` est mentionné, Then **seul `C`** est notifié — aucun autre agent, aucune file.
- **Étanchéité client (MSG-02)** : Given une note interne mentionnant `@C`, When elle est publiée, Then **aucune notification n'est émise vers le client**, sur aucun canal.
- **Non-réaffectation (MSG-05)** : Given un ticket affecté à l'opérateur `O`, When `@C` est mentionné, Then le ticket **reste affecté à `O`** — le raccordement ne produit aucun effet de workflow.
- **Escalade SLA non raccordée** : Given un dépassement de SLA déclenchant une escalade selon les règles F001, When l'escalade s'exécute, Then **aucune notification de supervision n'est produite par F009** et le mécanisme F001 s'exécute inchangé.

---

## 21. Préférences Utilisateur

**Le CDC prévoit explicitement des préférences** — elles ne sont donc ni inventées, ni omises :

| Dimension | Prévue par le CDC ? | Retenu dans F009 |
|:---|:---|:---|
| Préférence **par canal** (activation/désactivation) | **OUI** — COM-05 (littéral) | ✅ Retenu (Should) |
| Respect des **notifications obligatoires** | **OUI** — COM-05 : *« dans le respect des notifications obligatoires »* | ✅ Retenu — la liste des notifications obligatoires n'étant pas fournie par le CDC, elle relève du paramétrage (CLAR-COM-03) |
| Opt-in / opt-out global | **NON** | ❌ Non inventé |
| Préférence de **fréquence** (digest, regroupement) | **NON** | ❌ Non inventé |
| Préférence de **langue** | **NON** | ❌ Non inventé |
| Préférence par **type d'événement** (au-delà du canal) | **NON** — COM-05 ne parle que de canaux | ❌ Non inventé |
| Plages horaires / « ne pas déranger » | **NON** | ❌ Non inventé |

**Aucun centre de préférences au-delà du choix de canal n'est spécifié.**

### 21.1 Notifications obligatoires — qualification (CLAR-COM-03)

COM-05 nomme la notion (*« dans le respect des notifications obligatoires »*) mais **ne fournit jamais la liste**. Plutôt que d'inventer cette liste, la présente clarification établit **la grille de qualification** qui permettra au métier de la produire, et **classe chaque événement selon ce que le CDC permet d'affirmer** — quatre statuts, jamais un cinquième :

| Statut | Définition | Effet sur COM-05 |
|:---|:---|:---|
| **OBLIGATOIRE** | Une exigence CDC impose la notification **et** nomme son canal | Le client **ne peut pas** désactiver ce canal pour cet événement |
| **OPTIONNELLE** | Le CDC impose la notification mais **ne nomme aucun canal** | La notification est due ; **le choix du canal reste débrayable** par le client, sous réserve qu'au moins un canal reste servi |
| **CONFIGURABLE** | Aucune exigence CDC n'impose la notification ; elle existe si le paramétrage la crée | Entièrement débrayable par le client |
| **NON SPÉCIFIÉE** | Le CDC est silencieux ; **aucune règle n'est écrite ici** | Sans objet tant que le métier n'a pas tranché |

**Application de la grille aux événements de §10.2** (dérivée, pas inventée) :

| Événement | Statut | Justification |
|:---|:---|:---|
| Ticket créé — numéro unique communiqué | **OBLIGATOIRE** | SOC-01 impose la notification **et** nomme les canaux (*« portail + e-mail »*) |
| Numéros de ticket IMPORT | **OBLIGATOIRE** | IMP-03 impose la notification **et** nomme l'e-mail |
| Demande de complément | **OPTIONNELLE** | IMP-05 impose la notification ; le canal n'est pas nommé (NOT-02 n'en donne qu'un exemple) |
| Facture disponible | **OPTIONNELLE** | IMP-11 impose la notification ; le canal n'est pas nommé (NOT-02 n'en donne qu'un exemple) |
| Transfert impactant le parcours | **OPTIONNELLE** | TRF-05 impose la notification ; aucun canal nommé |
| Mention `@Nom` (agent) | **OPTIONNELLE** | MSG-04 nomme le portail comme acquis et l'e-mail comme *« selon paramétrage »*. **Exigence propriété de F001** ; **raccordement In-App livré par F009** (§13.1). Le volet e-mail reste débrayable, conformément au *« selon paramétrage »* du CDC |
| Escalade SLA (superviseur) | **OPTIONNELLE** | ALE-03 impose la notification ; ALE-02 rend destinataires **et** canaux paramétrables. **Exigence propriété de F001** ; **non raccordée par F009** dans cette feature (§13.1) |
| Documents de sortie · action requise | **CONFIGURABLE** | CDC-EXEMPLE (§14.1) : cité, jamais imposé |
| Ticket rejeté · preuve de paiement · paiement validé · ticket clôturé | **CONFIGURABLE** | PC-17-PROPOSITION : aucune exigence CDC |
| Relance absence de réponse | **CONFIGURABLE** | COM-06 est **Could** — la dimension canal est explicitement optionnelle |
| Liste définitive des notifications non débrayables | **NON SPÉCIFIÉE** | COM-05 nomme la notion sans fournir la liste — **arbitrage métier, non comblé ici** |

> **Deux garde-fous, non négociables** :
> 1. **Aucune règle globale n'est déduite d'un exemple.** Les couples cités par NOT-02 (*« facture disponible → portail + e-mail »*, etc.) restent des illustrations : ils fondent un défaut de paramétrage crédible, **jamais une obligation**.
> 2. **Le caractère obligatoire est une propriété du paramétrage**, portée par la règle de diffusion (NOT-02), et non une propriété codée en dur d'un type d'événement. Les deux seules lignes **OBLIGATOIRE** ci-dessus le sont parce qu'un texte CDC nomme leurs canaux — pas parce que la spec en a décidé ainsi.

---

## 22. Données de Contact

**Le CDC ne traite nulle part la collecte, la validation ni l'absence d'une coordonnée de contact.** Le tableau ci-dessous constate donc **ce qui existe réellement dans la plateforme fermée** (F001→F008) — constat d'état, **sans décider d'aucun modèle technique ni proposer de champ nouveau**.

| Donnée | Référentiel porteur constaté | Disponibilité réelle | Statut |
|:---|:---|:---|:---|
| **Adresse e-mail** | **Compte utilisateur** (référentiel des comptes, F002). L'e-mail est **l'identifiant de connexion** ; sa modification relève d'une opération d'identité, hors du contrat de profil personnel | **Toujours présente et unique** pour tout compte existant | **DISPONIBLE — aucun besoin ouvert.** Le canal e-mail est servable pour tout destinataire authentifié |
| **Numéro de téléphone** | **Compte utilisateur** — le profil personnel porte un numéro de téléphone **facultatif, déclaré par l'utilisateur lui-même**, borné en longueur, **sans contrôle de format, sans vérification de possession, et sans obligation de saisie** | **Facultative et non garantie** : rien n'impose ni ne vérifie sa présence pour les comptes existants | **BESOIN DOCUMENTÉ — CLAR-COM-04.** Les canaux mobiles en dépendent intégralement |
| **Coordonnée au niveau de la société cliente** | **Aucun** — le référentiel société (F002) porte le demandeur, les informations déclarées, le profil et l'état ; **il ne porte ni e-mail ni téléphone de société** | Inexistante | **NON DISPONIBLE.** La société **n'est pas** une source de coordonnée ; elle ne l'a jamais été. Notifier « la société » signifie notifier ses collaborateurs habilités, chacun sur **sa propre** coordonnée (§13) |
| **Coordonnée du transporteur / chauffeur** | **Aucun** — l'enrôlement (F008) est porté par le ticket ; **aucune entité transporteur ou chauffeur distincte n'existe**, donc aucun numéro de téléphone de chauffeur | Inexistante | **NON DISPONIBLE — constat structurant.** ENR-04 (*« SMS/WhatsApp prioritaires »* au chauffeur) **n'a aujourd'hui aucune source de coordonnée**, indépendamment de PC-16 |
| **Identifiant WhatsApp** | Aucun | Inexistante | **NON SPÉCIFIÉ PAR LE CDC** — aucune hypothèse n'est faite sur le fait qu'il s'agisse ou non du même numéro que le SMS. Cf. CLAR-COM-04 |

**Règles** :
- Les coordonnées utilisées pour notifier proviennent **exclusivement** du profil de l'utilisateur destinataire authentifié (§13).
- **Aucune saisie ad hoc** de coordonnée de destination n'est permise, pour aucun canal.
- Si une donnée de contact requise est absente pour un destinataire, le canal correspondant ne peut être servi — **le comportement attendu dans ce cas (canal ignoré silencieusement, tracé comme non délivrable, ou signalé) n'est pas spécifié par le CDC** : cf. CLAR-COM-04.
- **Aucun modèle technique n'est décidé ici** : la manière de porter, valider ou étendre ces données relève de `/speckit.plan`. Cette section **ne crée aucun champ** et n'en préconise aucun.

### 22.1 Niveau de blocage réel, par chemin (CLAR-COM-04)

La question « la coordonnée manque-t-elle ? » n'a pas une réponse unique : elle dépend du canal et du destinataire. Le blocage est donc **décomposé**, et non globalisé :

| Chemin | Bloque `/speckit.plan` ? | Bloque IMPLEMENTATION ? | Bloque RECETTE ? | Bloque PRODUCTION ? |
|:---|:---:|:---:|:---:|:---:|
| **Portail in-app** → tout destinataire authentifié | **NON** | **NON** | **NON** | **NON** — aucune coordonnée externe requise |
| **E-mail** → Client ou Agent | **NON** | **NON** | **NON** | **NON** — l'e-mail est garanti par le compte |
| **SMS / WhatsApp** → Client ou Agent | **NON** | **NON** — le mécanisme se conçoit et se code sans la donnée | **OUI si le canal mobile est mis en recette** — sans numéro renseigné, le scénario n'est pas rejouable | **OUI** — cumulé avec PC-16 |
| **SMS / WhatsApp** → Transporteur / Chauffeur (**ENR-04**) | **NON** (hors périmètre F009) | — | — | **OUI, et pour une raison supplémentaire** : aucune donnée de contact n'existe pour ce destinataire. **Ce point est indépendant de PC-16** et relèvera de la feature qui reprendra ENR-04 |
| **Comportement en cas de coordonnée absente** (ignorer / tracer non délivrable / signaler) | **NON** — le CDC §20 impose déjà un *« statut d'envoi »*, ce qui rend le cas **modélisable** sans trancher la sémantique | **OUI** — la sémantique retenue doit être arrêtée avant d'être codée | — | — |

**Réponse consolidée à CLAR-COM-04** : le téléphone provient du **compte utilisateur** et de nulle part ailleurs — ni de la société, ni d'un référentiel tiers, ni d'une saisie au moment de l'envoi. **Il ne bloque ni `/speckit.plan`, ni l'implémentation du moteur de notification** ; il borne **les canaux SMS/WhatsApp** à partir de la recette, et **il bloque ENR-04 pour une raison propre** (absence totale de coordonnée chauffeur) qui ne relève pas de F009.

---

## 23. Frontière avec la Feature 010 (CON)

F009 **ne couvre pas** et ne doit en aucun cas absorber :
- **CON-01** — Reprise du dossier en cours au guichet, sans ressaisie
- **CON-02** — Recherche rapide multicritère au guichet (n° ticket, client, BL, booking)
- **CON-03** — Prise de rendez-vous rattachée au ticket, accès au bureau de facturation
- **CON-04** — Système de file d'attente physique / redirection
- **ERR-04** — Mode dégradé documenté en cas d'indisponibilité du portail (dépendance CON-01)
- Toute forme de **saisie différée**, de **continuité présentiel ↔ digital**, ou de **reprise après indisponibilité**

Ces exigences sont formellement **réservées à la Feature 010 — Continuité Guichet & Rendez-vous**. La notification d'un rendez-vous confirmé (proposition PC-17, §10) est également hors périmètre F009 tant que CON-03 n'est pas livrée : elle n'aurait aucun événement source.

---

## 24. Frontières Supplémentaires

Hors périmètre F009, explicitement :
- **Avoir / Exonération (AVE-01→04)** — Feature 007, en attente d'arbitrage métier.
- **Assistant IA (IA-01→07)** — module distinct ; IA-05 mentionne les canaux de messagerie mais reste hors périmètre.
- **Technologie SMS spécifique** et **fournisseur WhatsApp spécifique** — aucun choix, cf. §8.
- **Fournisseur e-mail spécifique** — aucun choix, cf. §7.
- **Mode offline** et synchronisation bidirectionnelle — non prévus par le CDC (principe P3 : plateforme autonome).
- **Push mobile** — absent du CDC, non ajouté.
- **Refonte des traitements de fond existants** — aucune décision prise ici (§17).
- **Observabilité globale / NFR transverses futurs** — hors périmètre.
- **ENR-04** (notifications d'enrôlement, Should, F008 — tracée comme différée lors de la clôture F008) : F009 fournit la capacité de notification ; **le rattachement effectif d'ENR-04 aux canaux F009 n'est pas revendiqué par cette spécification** et reste une décision de périmètre ultérieure.

---

## 25. Clarifications

### Session 2026-09-01

Passe `/speckit.clarify` — audit des 9 clarifications contre le CDC source (`docs/cdc_text.txt`), la matrice d'exigences, les points à clarifier et la matrice de recette. **Aucune décision métier n'a été prise à la place du métier** ; les résolutions ci-dessous ne font qu'expliciter ce que les textes permettent d'affirmer.

- Q: CLAR-COM-01 — F009 doit-elle supporter au moins un canal mobile, les deux, l'un ou l'autre au choix, ou différer complètement les canaux mobiles ? → A: **Option C — canaux SMS et WhatsApp spécifiés comme capacités configurables, l'un et/ou l'autre**, conformément au *« et/ou »* littéral de COM-01. La capacité mobile est Must ; **le choix du canal servi est une décision d'exploitation**, classée AVANT PRODUCTION (PC-16). Aucun Must In-App/E-mail n'est bloqué par ce point (RSQ-04).
- Q: CLAR-COM-02 — Quels événements produisent effectivement une notification, et sur quels canaux ? → A: **Matrice §10.2 produite**, à trois niveaux de source (CDC-EXIGENCE / CDC-EXEMPLE / PC-17-PROPOSITION). **5 événements client sont CDC-EXIGENCE dans le périmètre F009** (SOC-01, IMP-03, IMP-05, IMP-11, TRF-05) ; **2 seulement nomment leurs canaux** (SOC-01, IMP-03). **Aucun « notifier tout changement de statut » n'est retenu.** La liste initiale complète de paramétrage reste AVANT IMPLEMENTATION (PC-17).
- Q: CLAR-COM-03 — Quelles notifications sont obligatoires, et selon quel critère ? → A: **Grille de qualification à 4 statuts établie (§21.1)** — OBLIGATOIRE / OPTIONNELLE / CONFIGURABLE / NON SPÉCIFIÉE — et appliquée à chaque événement de §10.2. **Seuls SOC-01 et IMP-03 sont OBLIGATOIRE** (le CDC impose l'événement **et** nomme le canal). La liste définitive des non-débrayables reste NON SPÉCIFIÉE, AVANT IMPLEMENTATION.
- Q: CLAR-COM-04 — D'où provient le numéro de téléphone, et que se passe-t-il si une coordonnée manque ? → A: **Du compte utilisateur, et de nulle part ailleurs** (§22) — la société ne porte aucune coordonnée, et **l'enrôlement n'en porte aucune pour le chauffeur**. Le numéro y est **facultatif, auto-déclaré, non vérifié**. **Ne bloque ni le plan ni l'implémentation du moteur** ; borne SMS/WhatsApp à partir de la RECETTE ; bloque ENR-04 pour une raison propre, indépendante de PC-16 (§22.1).
- Q: CLAR-NOT-01 — Quels événements notifient un agent, et lequel ? → A: **OPTION B VALIDÉE.** **Deux textes CDC seulement** nomment un destinataire interne — **MSG-04** (le collaborateur **nommément mentionné**) et **ALE-03/ALE-02** (**un superviseur**). F009 fournit la **capacité générique de notification adressable aux clients et aux agents** (NOT-01). **MSG-04 est effectivement raccordée** : une mention `@Nom` produit une **notification In-App** au collaborateur concerné, l'e-mail restant **conditionné au paramétrage prévu par MSG-04**. **ALE-03 reste hors périmètre de raccordement** : F009 ne fournit que le **contrat / point d'intégration** permettant au mécanisme SLA existant de produire ultérieurement une notification au superviseur, **sans réimplémenter le workflow SLA de F001**. **MSG-04 et ALE-01→03 demeurent propriété de F001 et ne sont pas recomptées** dans les 9 exigences COM/NOT de F009 (§27). **Aucun autre événement agent n'est sourcé** ; aucun broadcast, aucune notification « à tous les agents » ni « à une file ». Test dédié : **`TEST-MSG-04`** (§18 bis) — *Mention `@Nom` → notification In-App du collaborateur mentionné*. Point rattaché : **PC-25**.
- Q: CLAR-NOT-02 — Que fait le système en cas d'échec d'envoi ? → A: **Quatre obligations arrêtées** (§15) : statut d'envoi par canal (**CDC §20 — sourcé, non déduit**), traçage, nouvelle tentative automatique, isolation inter-canaux ; plus une conséquence directe : **la transaction métier reste valide**. **Nombre, cadence et condition d'arrêt restent des paramètres sans valeur** — configurables et à confirmer AVANT IMPLEMENTATION. **Backoff, fallback inter-canaux, alerte administrateur et message d'échec au destinataire sont écartés** (non prévus par le CDC).
- Q: CLAR-NOT-03 — Le CDC impose-t-il une durée de conservation des notifications ? → A: **Non — aucune exigence COM/NOT ne traite la conservation des notifications.** SOC-09 pose l'archivage des **tickets** sans suppression, et PC-14 gouverne la politique de conservation générale sans nommer les notifications. **Aucune suppression ni archivage automatique n'est spécifié ni inventé.** NON BLOQUANTE, à confirmer AVANT PRODUCTION avec PC-14.
- Q: CLAR-NOT-04 — Une notification peut-elle cibler une file, ou seulement un utilisateur ? → A: **Un destinataire nommé uniquement.** Le CDC §20 modélise *« Notification (canal, modèle, **destinataire**, statut d'envoi) »* au **singulier**, et ne nomme jamais une file comme destinataire. Sont admis : l'utilisateur client concerné, les collaborateurs habilités de sa société (chacun individuellement), l'agent nommément mentionné (MSG-04), le superviseur désigné par le paramétrage d'alerte (ALE-02). **Aucun broadcast global n'est créé.** NON BLOQUANTE.
- Q: CLAR-NOT-05 — Que le CDC impose-t-il sur le contenu des modèles ? → A: **Le modèle est un objet exigé** (CDC §20 : *« modèle »* est un attribut de la notification ; §19 le liste parmi les référentiels paramétrables), **paramétrable par module, par étape et par canal** (COM-03), et **chaque notification doit comporter un lien vers le ticket** (COM-02). **Aucun texte, objet, titre, corps, variable, langue ni signature n'est fourni par le CDC** — et aucun n'est inventé ici. Modèles configurables, contenus à fournir AVANT RECETTE.

### 25.1 Audit détaillé des 9 clarifications

| ID | Question exacte | Source CDC | Options envisagées | Décision retenue | Ce qui reste ouvert | Impact COM/NOT | Impact REC-SCE-14 | Statut final |
|:---|:---|:---|:---|:---|:---|:---|:---|:---|
| **CLAR-COM-01** | Quel(s) canal(aux) mobile(s) F009 doit-elle servir ? | **COM-01** : *« au moins un canal de messagerie mobile (WhatsApp **et/ou** SMS) »* · PC-16 (🟠, bloque PRODUCTION) · RSQ-04 | **A.** au moins un canal mobile imposé · **B.** les deux imposés · **C.** l'un et/ou l'autre, configurable · **D.** différer tous les canaux mobiles | **C** — capacité mobile Must, **choix du canal non imposé** ; les deux sont spécifiés comme capacités configurables, sans fournisseur ni fallback | **Quel(s) canal(aux) souscrire, auprès de qui, à quel budget** — décision DSI/Finance | Borne COM-01 ; sans effet sur COM-02→06 et NOT-01→03 | Borne la précondition *« Canaux configurés »* — **satisfaisable sans canal mobile** | **À CONFIRMER AVANT PRODUCTION** |
| **CLAR-COM-02** | Quelle est la liste exacte des événements produisant une notification, et leurs canaux ? | **SOC-01 · IMP-03 · IMP-05 · IMP-11 · TRF-05 · AVE-03 · MSG-04 · ALE-03 · CYC-04** · CDC §14.1 · **NOT-02** (exemples) · COM-03 · PC-17 (🟡, bloque IMPLEMENTATION) | **A.** notifier tout changement de statut · **B.** liste figée dans la spec · **C.** matrice sourcée + liste initiale paramétrable à valider | **C** — matrice §10.2 à trois niveaux de source. **A explicitement écartée** : aucun texte CDC ne l'exige. **B écartée** : COM-03/NOT-02 imposent la paramétrabilité | **La liste initiale de configuration** et les canaux par défaut des événements non nommés par le CDC | Cœur de COM-01, COM-03, NOT-02 ; conditionne COM-05 et COM-06 | Fonde le volet *« notifications sur chaque canal configuré »* | **À CONFIRMER AVANT IMPLEMENTATION** |
| **CLAR-COM-03** | Quelles notifications sont obligatoires (non débrayables) ? | **COM-05** : *« dans le respect des notifications obligatoires »* — notion nommée, **liste jamais fournie** | **A.** tout est débrayable · **B.** liste inventée par la spec · **C.** grille de qualification sourcée + liste à produire par le métier | **C** — grille à 4 statuts (§21.1). **Seuls SOC-01 et IMP-03 sont OBLIGATOIRE**, car le CDC y nomme l'événement **et** le canal. **B écartée** (Constitution, Principe XV) | **La liste définitive des non-débrayables** au-delà des deux cas sourcés | Conditionne **COM-05 (Should)** ; sans effet sur les Must | Touche la colonne Should/Could (COM-05) | **À CONFIRMER AVANT IMPLEMENTATION** |
| **CLAR-COM-04** | D'où provient le téléphone du destinataire, et que faire si la coordonnée manque ? | Aucun texte CDC sur la collecte/validation/absence · COM-01 (canaux mobiles) · **CDC §20** (*« destinataire »* modélisé) | **A.** propriété du compte utilisateur · **B.** propriété de la société · **C.** autre référentiel · **D.** saisie au moment de l'envoi | **A** — **constat vérifié** : le téléphone n'existe que sur le **compte utilisateur** (facultatif, auto-déclaré, non vérifié). **B constatée impossible** (la société ne porte aucune coordonnée). **D interdite** (§13). **Aucun champ n'est créé ici** | **La sémantique du canal non servable** (ignorer / tracer non délivrable / signaler) · la **collecte et la vérification** du numéro · **l'absence totale de coordonnée chauffeur** (ENR-04) | Borne les canaux mobiles de COM-01 ; sans effet sur In-App et e-mail | Borne la précondition *« Canaux configurés »* pour le seul volet mobile | **À CONFIRMER AVANT IMPLEMENTATION** (RECETTE pour le volet mobile ; **PRODUCTION** pour ENR-04) |
| **CLAR-NOT-01** | Quels événements notifient un agent, et quel agent précisément ? | **MSG-04** (Should) : *« la personne mentionnée reçoit une notification… »* · **ALE-03** (Must) : *« notification d'un superviseur »* · **ALE-02** (Must) : *« destinataires et canaux paramétrables »* · **PC-25** (🟡) | **A.** agent assigné · **B.** tous les membres de la file · **C.** tous les habilités · **D.** superviseur · **E.** uniquement les acteurs nommés par le CDC | **E + Option B validée** — **seuls deux destinataires internes sont sourcés** : le collaborateur **nommément mentionné** (MSG-04) et **un superviseur** (ALE-03). **B et C écartées** — moindre privilège : aucun texte CDC ne prévoit de notifier une file ni l'ensemble des habilités. **F009 fournit la capacité générique adressable clients + agents** (NOT-01). **MSG-04 : raccordement In-App livré**, e-mail conditionné au paramétrage MSG-04. **ALE-03 : point d'intégration seul**, aucun workflow SLA réimplémenté. **Les deux exigences restent propriété de F001 et ne sont pas recomptées** (§13.1, §27) | **Les règles de notification interne CIT** (PC-25) : préférences agents, canaux, **raccordement effectif d'ALE-03** et événements additionnels éventuels | **Aucune exigence F009 n'en dépend** — NOT-01 vaut pour tout destinataire authentifié, client ou agent, et est **démontrée sur un destinataire agent** par `TEST-MSG-04` | Renforce l'acteur *« Agent »* du scénario ; l'acteur *« Superviseur »* n'y reste exercé que par le journal d'audit | **RÉSOLUE pour le périmètre F009** (Option B) — reste À CONFIRMER AVANT IMPLEMENTATION pour les seules règles internes PC-25 |
| **CLAR-NOT-02** | Que le CDC impose-t-il réellement en cas d'échec d'envoi ? | **ERR-02** (littéral, cité §15) · **CDC §20** : *« Notification (canal, modèle, destinataire, **statut d'envoi**) »* | **A.** journalisation seule · **B.** retry seul · **C.** retry + traçage + isolation + statut d'envoi · **D.** ajouter backoff/fallback/alerte | **C** — les **quatre** obligations sont sourcées (le statut d'envoi par le CDC §20, **correction apportée** : il était classé « déduit »). **D explicitement écartée** : backoff, fallback inter-canaux, intervention humaine et message d'échec au destinataire ne sont **pas** prévus par le CDC | **Nombre de tentatives, cadence, condition d'arrêt** — **paramètres sans valeur**, à confirmer ; aucune valeur par défaut n'est proposée | Rend ERR-02 effective ; contraint l'émission de COM-01 et la traçabilité §16 | **Aucun** — ERR-02 est elle-même *« Aucun \| Absente »* en recette → **TEST-ERR-02** | **À CONFIRMER AVANT IMPLEMENTATION** (mécanisme **arrêté**, valeurs ouvertes) |
| **CLAR-NOT-03** | Le CDC impose-t-il une durée de conservation des notifications ? | **Aucune exigence COM/NOT** · SOC-09 (archivage des **tickets**, sans suppression) · PC-14 (🟠, conservation générale — **ne nomme pas les notifications**) | **A.** durée fixée par la spec · **B.** suppression automatique après lecture · **C.** aucune suppression, conservation alignée sur le principe transverse | **C** — **le CDC est silencieux ; aucune suppression ni archivage automatique n'est inventé.** A et B écartées (Constitution, Principe XV) | La politique de conservation applicable aux notifications, à traiter avec **PC-14** | Aucun impact sur une exigence COM/NOT | Aucun | **NON BLOQUANTE** — à confirmer **AVANT PRODUCTION** (avec PC-14) |
| **CLAR-NOT-04** | Une notification peut-elle cibler une file, ou seulement un utilisateur ? | **CDC §20** : *« Notification (canal, modèle, **destinataire**, statut d'envoi) »* — **singulier** · COM-01/NOT-01 : *« Client · Agents »* · **MSG-04** (personne mentionnée) · **ALE-02** (destinataires paramétrables) | **A.** utilisateur précis · **B.** membres d'une file · **C.** utilisateur assigné au ticket · **D.** superviseur · **E.** broadcast global | **A**, plus **D** par le paramétrage d'alerte (ALE-02, propriété F001). **B écartée** — une file est une destination de ticket, jamais un destinataire nommé par le CDC. **E écartée** — **aucun broadcast n'est créé.** **C** n'est retenu que si le paramétrage désigne cet utilisateur nommément | Rien de bloquant : à réexaminer **uniquement** si un besoin métier réel de notification de file apparaît | Borne §13 et §14 ; **aucune exigence ne réclame de destinataire collectif** | Renforce le volet *« contrôle des habilitations »* | **NON BLOQUANTE** |
| **CLAR-NOT-05** | Que le CDC impose-t-il sur le contenu des modèles ? | **COM-03** (modèles paramétrables par module/étape/canal) · **COM-02** (lien vers le ticket) · **CDC §20** (*« modèle »* attribut exigé) · **CDC §19** (*« modèles de messages »* parmi les référentiels paramétrables) | **A.** figer des libellés dans la spec · **B.** modèles configurables sans contenu figé · **C.** différer entièrement la notion de modèle | **B** — le **modèle est un objet exigé et paramétrable** ; **le seul contenu imposé est le lien vers le ticket** (COM-02). **Objet, titre, corps, variables, langue et signature ne sont fournis par aucun texte CDC** et ne sont pas inventés. **C écartée** : le modèle est exigé par le CDC §20 | **Les textes eux-mêmes** : objet, titre, corps, jeu de variables, langue, signature — donnée d'administration à produire par le métier | Conditionne COM-03 et NOT-02 en exploitation, **pas leur conception** | Nécessaire pour rejouer *« notifications sur chaque canal configuré »* de façon probante | **À CONFIRMER AVANT RECETTE** |

**Aucune clarification n'est classée BLOQUANTE POUR PLAN.** Les mécanismes exigés (émission, état Lu/Non lu, lien sécurisé, paramétrage par événement et par canal, statut d'envoi, nouvelle tentative, isolation inter-canaux) sont **tous définis par le CDC** ; les points ouverts portent sur des **valeurs de paramétrage**, des **textes de modèles** et des **décisions d'exploitation**, non sur la conception fonctionnelle — un classement déjà appliqué sans blocage aux features F001, F003, F004, F006 et F008.

**Ce que cette passe a corrigé dans la spécification** (vérifications contre `docs/cdc_text.txt`, non des reformulations) :
1. **SOC-01** reconnue comme exigence CDC imposant la notification « ticket créé » **et ses canaux** (portail + e-mail), pour **tous les modules** — la spec ne connaissait qu'IMP-03 et la présentait à tort comme le seul texte nommant l'e-mail pour un événement (§7, §10.2).
2. **IMP-11** reconnue comme source de l'événement « facture disponible », précédemment classé simple proposition PC-17 (§7, §10.2).
3. **CDC §14.1** et les **exemples littéraux de NOT-02** introduits comme niveau de source intermédiaire **CDC-EXEMPLE**, distinct des propositions PC-17 (§10.1).
4. **CDC §20** (*« Notification (canal, modèle, destinataire, statut d'envoi) »*) introduit comme source : le **statut d'envoi** passe de « déduit » à **exigé** (§15), le **modèle** devient un objet exigé (§6, CLAR-NOT-05), et le **destinataire au singulier** fonde le refus du broadcast (§13, CLAR-NOT-04).
5. **MSG-04** et **ALE-03/ALE-02** identifiées comme les deux seules sources CDC d'une notification à un acteur interne, **toutes deux propriété de F001** — frontière formalisée en §13.1, là où la spec déclarait qu'*« aucune exigence ne définit les événements notifiant un agent »*. **Décision Option B validée le 1er septembre 2026** : MSG-04 est **raccordée** au moteur F009 (notification In-App au collaborateur mentionné, e-mail conditionné au paramétrage MSG-04), ALE-03 reçoit un **point d'intégration seul** ; **aucune des deux n'est recomptée** dans la couverture de F009 (§27), et un test dédié `TEST-MSG-04` est ajouté (§18 bis).
6. **PC-25** rattaché à CLAR-NOT-01, avec signalement de la divergence de libellé du preflight `docs/13` (§13.1).
7. **ERR-02** ajoutée à la liste des Must sans couverture de recette (`04-matrice-recette.md` ligne 230), et dotée de **TEST-ERR-02** (§18 bis).

---

## 26. Matrice de Couverture

| Exigence | Priorité | User Story | Critère d'acceptation | REC-SCE-14 | TEST-SPEC | Clarification | Dépendance |
|:---|:---:|:---|:---|:---|:---|:---|:---|
| **COM-01** | Must | US2 | §20 US2 (nominal, canal indisponible, donnée interne) | **Directe** | `TEST-NOT-02` (cas 8) · `TEST-ERR-02` | CLAR-COM-01, CLAR-COM-02, CLAR-COM-04 | — |
| **COM-02** | Must | US2, US3 | §20 US3 (sécurité du lien, objet inaccessible) | **Directe** | `TEST-NOT-01` (cas 4, 5, 8) | CLAR-NOT-05 (le lien est le seul contenu imposé) | SEC-12 [E] |
| **COM-03** | Must | US5 | §20 US5 (nominal, autorisation, traçabilité) | **Partielle** → TEST-SPEC complémentaire | `TEST-NOT-02` (cas 1→6) | CLAR-COM-02, CLAR-NOT-05 | — |
| **COM-04** | Must | US6 | §20 US6 (nominal, non-duplication) | **Partielle** (REC-SCE-01 · REC-SCE-14) | — (non-régression F001) | — | SOC-05 [E] |
| **COM-05** | Should | US7 | §20 US7 (nominal, notification obligatoire) | Directe (colonne Should/Could) | `TEST-NOT-02` (cas 7) | CLAR-COM-03 | COM-01 [D] |
| **COM-06** | Could | US8 | §20 US8 (nominal) | **Aucune** → TEST-SPEC si retenue | **`TEST-COM-06`** (conditionnel) | CLAR-COM-02 | CYC-04 [D] |
| **NOT-01** | Must | US1, US4, **US9** | §20 US1/US4 (nominal, déjà lue, non autorisé, cloisonnements ×2) · §20 US9 (raccordement mention) | **AUCUNE — « Absente »** → **TEST-SPEC obligatoire** | **`TEST-NOT-01`** (8 cas) · **`TEST-MSG-04`** (8 cas, destinataire agent) | CLAR-NOT-01 | COM-01 [D] |
| **NOT-02** | Must | US5 | §20 US5 (nominal, autorisation, traçabilité) | **AUCUNE — « Absente »** → **TEST-SPEC obligatoire** | **`TEST-NOT-02`** (8 cas) | CLAR-COM-02, CLAR-COM-03, CLAR-NOT-01, CLAR-NOT-04 | COM-03 [E] |
| **NOT-03** | Should | US1, US4 | §20 US1/US4 (indicateur, décrément) | Directe (colonne Should/Could) | `TEST-NOT-01` (cas 1, 2, 3, 6) | — | NOT-01 [D] |
| *(transverse)* **ERR-02** | Must | US2 | §20 US2 (canal indisponible) | **AUCUNE — « Absente »** → **TEST-SPEC obligatoire** | **`TEST-ERR-02`** (6 cas) | CLAR-NOT-02 | COM-01 [D] |
| *(raccordement)* **MSG-04** | Should — **propriété F001** | **US9** | §20 US9 (nominal, e-mail conditionnel, destinataire unique, MSG-02, MSG-05) | **AUCUNE — « Absente »** (F001, FR-025) → **TEST-SPEC obligatoire** | **`TEST-MSG-04`** (8 cas) | CLAR-NOT-01 | MSG-02 [E] · MSG-05 [E] |

**Aucune exigence orpheline** : les 9 exigences sont rattachées à au moins une User Story et à un critère d'acceptation.

**Deux lignes ne sont pas des exigences de F009** et figurent ici pour leur seule traçabilité de test :
- **ERR-02** — **contrainte transverse rendue effective par F009** ; testée, **non comptée** dans la couverture apportée par F009 (§3, §27).
- **MSG-04** — **exigence propriété de F001** dont F009 livre le **raccordement notification manquant** (§13.1, Option B) ; testée par `TEST-MSG-04`, **non recomptée** dans les 9 exigences COM/NOT de F009 (§27).

---

## 27. Impact sur la Couverture CDC

- **Couverture certifiée avant F009** : **134 / 188** (≈ 71,3 %)
- **Exigences apportées par le périmètre F009** : **9** (COM-01→06, NOT-01→03), dont 6 Must, 2 Should, 1 Could
- **Non comptées** : SEC-12, ACR-01, ERR-02 (contraintes/frontières, §3) ; ERR-04 et CON-01→04 (Feature 010)
- **Non recomptées — exigences déjà acquises par F001 auxquelles F009 se raccorde** : **MSG-04** (raccordement In-App livré, §13.1) et **ALE-01→03** (point d'intégration seul). **Le total apporté par F009 reste 9**, inchangé par la décision Option B. Livrer le raccordement MSG-04 **n'ajoute aucune exigence à la couverture de F009** ; il rend effectif ce que F001 avait spécifié sans le servir

**Projections — aucune n'est acquise avant clôture réelle** :

| Hypothèse de clôture | Exigences ajoutées | Couverture projetée |
|:---|:---:|:---|
| Les 9 exigences démontrées conformes | +9 | **143 / 188** (≈ 76,1 %) |
| Must + Should démontrés, Could (COM-06) différée | +8 | 142 / 188 (≈ 75,5 %) |
| Must seuls démontrés, Should et Could différées | +6 | 140 / 188 (≈ 74,5 %) |

**Plancher réaliste : 140/188.** Le précédent des features fermées montre que les Should/Could dépendant d'un point à clarifier non résolu sont différées plutôt que forcées (ENR-04 et ENR-05 en F008). COM-05 (Should) dépend de CLAR-COM-03 et COM-06 (Could) de CLAR-COM-02 : leur clôture effective n'est pas garantie à ce stade.

---

*Document clarifié le 1er septembre 2026 (§25, Session 2026-09-01). Aucune décision métier n'a été prise à la place du métier au-delà de ce que le CDC permet d'affirmer directement ; aucun fournisseur, aucune stratégie de reprise chiffrée, aucune liste d'événements imposée, aucun contenu de modèle et aucune préférence utilisateur non prévue par le CDC n'ont été inventés. Les corrections apportées par la passe de clarification sont des **vérifications contre `docs/cdc_text.txt`**, listées en fin de §25.*

*Prochaine étape : `/speckit.plan`. **Ni `/speckit.plan` ni `/speckit.tasks` ne sont lancés par ce document.** Aucune Feature 010 n'est ouverte.*
