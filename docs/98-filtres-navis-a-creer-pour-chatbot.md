# Filtres Navis N4 à créer pour le chatbot (Universal Query API)

> **Document** : `docs/98-filtres-navis-a-creer-pour-chatbot.md`
> **Établi le** : 2026-09-23
> **Pour qui** : l'administrateur Navis N4 côté CIT — chaque ligne ci-dessous est un filtre à créer
> et enregistrer dans l'UI N4, avec un compte de service dédié au chatbot (voir « Un compte de
> service par usage » ci-dessous), pour que l'Universal Query API puisse l'exécuter à distance.
>
> **Rappel du mécanisme** : l'API ne permet pas d'écrire une requête libre — elle exécute un filtre
> **déjà enregistré** dans N4, identifié par son nom exact (`filtername=...`). Le connecteur YABA
> appelle ensuite ce nom, en lecture seule, avec les paramètres listés ci-dessous.

---

## Correspondance avec les questions fréquentes réelles — lecture seule uniquement

CIT a fourni les **30 catégories réelles** de requêtes utilisateurs les plus fréquentes
(`REQUETE VBS.xlsx`). **Règle absolue : le chatbot ne fait que de la lecture.** Aucune requête,
Navis ou OMNIO, n'écrit ni ne déclenche une action — c'est un principe de conception, pas une
limite technique contournable. Le tableau ci-dessous indique, pour chaque catégorie réelle,
l'**application** interrogée (`Navis` ou `OMNIO`), le **filtre**, et son **paramètre**. Les 3
catégories qui ne peuvent être servies par aucune lecture (ni Navis, ni OMNIO) sont marquées
**Aucune (lecture impossible)**.

| Catégorie réelle (FAQ) | Application | Filtre | Paramètre |
|:---|:---|:---|:---|
| Statut conteneurs | **Navis** | #1 `UNIT_STATUS_BY_NUMBER` | N° conteneur |
| TC Introuvable | **Navis** | #1 `UNIT_STATUS_BY_NUMBER` | N° conteneur |
| Position Conteneurs | **Navis** | #1 / #7 `UNIT_YARD_LOCATION` | N° conteneur |
| TC inaccessible | **Navis** | #2 `UNIT_HOLDS_BY_NUMBER` | N° conteneur |
| Débarquement conteneur dangereux | **Navis** | #1, colonnes IMDG | N° conteneur |
| Visibilité BL sur OMNIO | **OMNIO** (lecture du reflet Navis côté OMNIO) — à défaut, **Navis** #3 directement | Équivalent lecture de `UNITS_BY_BL_NUMBER` côté OMNIO, si exposé | N° de BL |
| Nomination | **Navis** | #4 `UNITS_BY_BOOKING_NUMBER` | N° de booking |
| Situation Navire CIT | **Navis** | #5 `VESSEL_VISIT_BY_VOYAGE` | N° de voyage |
| Attente au chargement | **Navis** | #6 `GATE_TRANSACTIONS_BY_UNIT` | N° conteneur |
| Embouteillage Sortie | **Navis**, si tracé | #6 `GATE_TRANSACTIONS_BY_UNIT` | N° conteneur |
| Assignation | **Navis** | #6 ou #12 `TRANSPORT_ORDER_STATUS` | N° conteneur / réf. mission |
| Prise de rendez-vous | **Navis** (VBS) | #9 `APPOINTMENT_STATUS` | N° conteneur / réf. RDV |
| Annulation rendez-vous | **Navis** (VBS) — *lecture du statut d'annulation, pas l'action d'annuler* | #9 `APPOINTMENT_STATUS` | Réf. RDV |
| Demande de créneau disponible | **Navis** (VBS) — *lecture des créneaux disponibles, pas la réservation* | #9 `APPOINTMENT_STATUS` | Date / type d'opération |
| Indisponibilité VBS | **Navis** (VBS), si statut système exposé en lecture — sinon incident IT | #9, à confirmer | — |
| Problème VGM | **Navis** | #10 `UNIT_VGM_STATUS` | N° conteneur |
| COPARN reçu/non reçu | **Navis** (EDI) | #11 `EDI_MESSAGE_STATUS` | N° de BL/booking |
| Mission conteneurs | **Navis** | #12 `TRANSPORT_ORDER_STATUS` | N° conteneur / réf. mission |
| Annulation mission | **Navis** — *lecture du statut annulé, pas l'action d'annuler* | #12 `TRANSPORT_ORDER_STATUS` | Réf. mission |
| Transfert conteneur HINO/CIT | **Navis**, si tracé | #13 `TERMINAL_TRANSFER_STATUS` | N° conteneur |
| Creation Compte OMNIO | **Aucune (lecture impossible)** | — | — |
| Activation compte OMNIO | **Aucune (lecture impossible)** | — | — |
| Anomalie OMNIO | **Aucune (lecture impossible)** | — | — |
| Autorisation acces BL IES | Ni Navis ni OMNIO — **YABA** (module IES, décision métier) | — | — |
| Actualisation BADT | **YABA** (déposé dans le ticket, IMP-14) | — | — |
| BADT expiré | **YABA** | — | — |
| Impression BEE | **YABA** (déposé dans le ticket, EXP-03) | — | — |
| RTG en panne | Ni Navis ni OMNIO — incident opérationnel | Escalade humaine | — |
| Probleme de reseau | Ni Navis ni OMNIO — incident IT | Escalade humaine | — |
| Autre | — | Toujours escalade humaine (IA-03) | — |

### Pourquoi les 3 catégories OMNIO sont marquées « Aucune (lecture impossible) »

| Catégorie | Nature réelle | Pourquoi aucune lecture ne peut y répondre |
|:---|:---|:---|
| Creation Compte OMNIO | **Action d'écriture** (créer un compte) | Le chatbot ne peut consulter que l'état d'un compte **déjà existant** — pas en créer un. |
| Activation compte OMNIO | **Action d'écriture** (changer un état) | Même raison — activer modifie une donnée, ce n'est pas une lecture. |
| Anomalie OMNIO | **Signalement d'incident**, pas une donnée interrogeable | Le client décrit un problème qu'aucune requête, même en lecture, ne peut « retourner » — il n'y a rien à lire, il y a quelque chose à corriger. |

Ces trois-là ne deviennent pas des filtres, quelle que soit l'application visée : ce sont, par
nature, des demandes qui appellent une **action** ou une **intervention humaine**, jamais une
lecture. Le chatbot doit les détecter et escalader directement (IA-03, IA-06) — éventuellement
avec une réponse procédurale de premier niveau si CIT le souhaite (« pour créer un compte OMNIO,
voici la marche à suivre : … », gérée par la base de connaissances IA-04, pas par une requête).

**Sur `Visibilité BL sur OMNIO`** : c'est la seule catégorie où OMNIO apparaît comme lecture
possible — mais je n'ai aucune documentation sur l'API OMNIO (contrairement à Navis, dont j'ai les
deux PDF). Je ne peux donc pas savoir si OMNIO expose sa propre requête en lecture seule, ou s'il
faut simplement lire cette même donnée côté Navis (filtre #3, déjà prévu). **Si vous avez une doc
API OMNIO équivalente aux PDF Navis, envoyez-la** — je ferai le même travail dessus.

---

## Un compte de service par usage, pas un compte partagé

**L'Universal Query API ne voit que les filtres créés par le compte qui les a enregistrés.**
Un filtre créé par un utilisateur A est invisible d'un utilisateur B, même si B a aussi le
privilège *Universal Query API Access*. Deux conséquences pratiques :

- **Créer un compte de service dédié par intégration**, pas un unique `n4api` partagé entre tous
  les usages présents et futurs. Concrètement pour CIT : un compte pour le chatbot (ex.
  `n4api-chatbot`), distinct de tout compte déjà utilisé — ou à utiliser demain — pour un autre
  usage (reporting, une autre intégration). C'est aussi une bonne pratique de sécurité : on peut
  couper l'accès du chatbot sans toucher aux autres intégrations, et l'historique d'audit N4
  distingue qui a fait quoi.
- **Tous les filtres du chatbot doivent être créés avec ce même compte de service** — si un
  administrateur les crée avec son propre compte personnel, le chatbot n'y aura jamais accès. Les
  8 filtres ci-dessous doivent donc tous être enregistrés en étant connecté avec le compte de
  service retenu, pas avec un compte nominatif.
- Si vous prévoyez plusieurs personas/scopes côté chatbot à terme (par exemple un jeu de filtres
  pour un usage donné et un jeu plus restreint pour un autre), la même logique s'applique : autant
  de comptes de service que de périmètres de visibilité distincts souhaités.

## Comment créer un filtre (rappel express)

1. Se connecter à N4 **avec le compte de service dédié au chatbot** (celui qui portera le
   privilège *Universal Query API Access - Allow* — l'API ne voit que les filtres créés par ce
   compte, voir ci-dessus).
2. Ouvrir l'écran de liste correspondant (colonne « Écran N4 » ci-dessous).
3. Ajouter les colonnes listées (colonne « Colonnes à inclure »).
4. Définir un prédicat de recherche paramétré sur le champ indiqué (colonne « Paramètre »), en
   cochant *Value Required* si le paramètre doit être obligatoire.
5. Enregistrer le filtre sous le **nom exact** indiqué en gras (colonne « Nom du filtre ») — ce nom
   est celui que le code du chatbot enverra tel quel dans `filtername=`.

---

## Filtres à créer

### 1. `UNIT_STATUS_BY_NUMBER` — Statut d'un conteneur par son numéro

| | |
|:---|:---|
| **Écran N4** | Units (Yard / Equipment) |
| **Paramètre** | Numéro de conteneur (`UNIT_NBR` ou équivalent — champ exact à confirmer selon votre version) |
| **Colonnes à inclure** | Numéro conteneur, Statut/Category, Emplacement (bloc/rangée/niveau), Navire, Voyage, Sens (Import/Export), Date de déchargement (ou date de gate-in export), Ligne maritime, **Statut dangereux / classe IMDG** |
| **Répond à** | « Où est mon conteneur ABCU1234567 ? », « Mon conteneur est-il déchargé ? », « Mon conteneur dangereux est-il débarqué ? » |
| **Priorité** | **Haute** — c'est la question client la plus fréquente (IA-01, IA-02) |

### 2. `UNIT_HOLDS_BY_NUMBER` — Blocages (holds) actifs sur un conteneur

| | |
|:---|:---|
| **Écran N4** | Units → onglet Holds, ou vue Holds dédiée |
| **Paramètre** | Numéro de conteneur |
| **Colonnes à inclure** | Numéro conteneur, Type de hold (douane, facturation, ligne, etc.), Statut du hold (actif/levé), Raison, Date de pose |
| **Répond à** | « Pourquoi je ne peux pas récupérer mon conteneur ? » — la cause n°1 de blocage à la sortie |
| **Priorité** | **Haute** — évite au client de venir physiquement pour rien |

### 3. `UNITS_BY_BL_NUMBER` — Conteneurs rattachés à un BL

| | |
|:---|:---|
| **Écran N4** | Units, filtré sur le champ BL |
| **Paramètre** | Numéro de BL |
| **Colonnes à inclure** | Numéro conteneur, Statut, Emplacement, Poids, Taille/Type |
| **Répond à** | « Quels conteneurs sont sur mon BL X ? » (utile quand un client a soumis plusieurs conteneurs sous un même BL sur YABA — SOC-11) |
| **Priorité** | Haute |

### 4. `UNITS_BY_BOOKING_NUMBER` — Conteneurs rattachés à un booking (export)

| | |
|:---|:---|
| **Écran N4** | Units, filtré sur le champ Booking |
| **Paramètre** | Numéro de booking |
| **Colonnes à inclure** | Numéro conteneur, Statut, Date de gate-in, Navire, Voyage, Date de coupure (cut-off) |
| **Répond à** | « Mes conteneurs export sont-ils bien réceptionnés pour le booking Y ? » |
| **Priorité** | Haute (miroir export du filtre #3) |

### 5. `VESSEL_VISIT_BY_VOYAGE` — Statut d'escale navire

| | |
|:---|:---|
| **Écran N4** | Vessel Visit / Vessel Screen |
| **Paramètre** | Numéro de voyage (ou nom du navire + sens) |
| **Colonnes à inclure** | Nom du navire, Voyage, ETA, ATA (arrivée réelle), ETD, ATD, Statut de l'escale (annoncée/en cours/terminée), Quai |
| **Répond à** | « Quand arrive le navire de mon conteneur ? », « Le navire est-il déjà à quai ? » |
| **Priorité** | Moyenne-Haute |

### 6. `GATE_TRANSACTIONS_BY_UNIT` — Historique des passages en gate d'un conteneur

| | |
|:---|:---|
| **Écran N4** | Gate Transactions / Truck Visit |
| **Paramètre** | Numéro de conteneur |
| **Colonnes à inclure** | Date/heure de transaction, Type (gate-in/gate-out), Numéro de camion, Chauffeur, Statut de la transaction |
| **Répond à** | « Mon conteneur est-il déjà sorti du terminal ? » — recoupe le module Enrôlement Transporteur (ENR-01..05) |
| **Priorité** | Moyenne |

### 7. `UNIT_YARD_LOCATION` — Emplacement précis en cour

| | |
|:---|:---|
| **Écran N4** | Yard Block / Yard View |
| **Paramètre** | Numéro de conteneur |
| **Colonnes à inclure** | Bloc, Rangée, Baie, Niveau, Date d'entrée en cour |
| **Répond à** | Complète le filtre #1 si vous voulez une réponse plus précise sur la localisation physique |
| **Priorité** | Basse (optionnel — souvent déjà couvert par #1) |

### 8. `UNIT_DAYS_IN_YARD` — Durée de séjour et statut de stationnement

| | |
|:---|:---|
| **Écran N4** | Units, avec calcul de durée ou vue dédiée démurrage |
| **Paramètre** | Numéro de conteneur |
| **Colonnes à inclure** | Date d'entrée, Date du jour (ou date de calcul), Nombre de jours de séjour, Statut franchise (dans ou hors franchise si Navis le calcule) |
| **Répond à** | « Depuis combien de temps mon conteneur est-il en cour ? » — utile en complément de la simulation de proforma YABA (SIM-01..11), qui calcule déjà le stationnement côté YABA mais sans connaître la date réelle de fin de séjour tant que le conteneur n'est pas sorti |
| **Priorité** | Basse-Moyenne |

### 9. `APPOINTMENT_STATUS` — Rendez-vous / créneaux camion (VBS)

| | |
|:---|:---|
| **Écran N4** | Écran de rendez-vous/appointments du Gate (VBS) |
| **Paramètre** | Numéro de conteneur, ou référence de rendez-vous, ou immatriculation camion selon ce qui est disponible |
| **Colonnes à inclure** | Référence du rendez-vous, Créneau (date/heure), Statut (confirmé/annulé/réalisé), Type d'opération (pickup/drop-off), Conteneur associé |
| **Répond à** | « Prise de rendez-vous », « Annulation rendez-vous », « Demande de créneau disponible », « Indisponibilité VBS » |
| **Priorité** | **Haute** — 4 des 30 catégories réelles en dépendent directement |
| **Point d'attention** | Le Gate API décrit des opérations dédiées (`create-appointment`, `cancel-appointment`, `request-pickup-appointments`) distinctes de l'Universal Query — vérifiez avec votre admin Navis si un filtre de **consultation** de créneaux existe sur une vue Table dédiée, ou si cette donnée n'est accessible que via le Gate API lui-même (auquel cas le connecteur devra utiliser ce second protocole, pas seulement `NavisUniversalQueryClient`). Je préciserai le client une fois cette réponse connue. |

### 10. `UNIT_VGM_STATUS` — Statut VGM (poids brut vérifié)

| | |
|:---|:---|
| **Écran N4** | Units, colonnes VGM |
| **Paramètre** | Numéro de conteneur |
| **Colonnes à inclure** | Numéro conteneur, VGM reçu (oui/non), Poids VGM, Date de réception, Méthode de pesée si tracée |
| **Répond à** | « Problème VGM » |
| **Priorité** | Moyenne |

### 11. `EDI_MESSAGE_STATUS` — Statut des messages EDI (COPARN)

| | |
|:---|:---|
| **Écran N4** | Vue des messages EDI entrants, si exposée en table (à confirmer avec votre admin Navis — l'existence de cette vue dépend de votre configuration EDI) |
| **Paramètre** | Numéro de BL ou de booking |
| **Colonnes à inclure** | Type de message (COPARN...), Date de réception, Statut (reçu/rejeté/en attente), Motif de rejet le cas échéant |
| **Répond à** | « COPARN reçu/non reçu » |
| **Priorité** | Moyenne |

### 12. `TRANSPORT_ORDER_STATUS` — Statut d'une mission conteneur

| | |
|:---|:---|
| **Écran N4** | Transport Order (voir Part 11 — ASC Automated Equipment Database Interface de la doc Navis) |
| **Paramètre** | Numéro de conteneur, ou référence de mission |
| **Colonnes à inclure** | Référence mission, Type (pickup/dropoff/move), Statut (en attente/en cours/terminée/annulée), Équipement assigné |
| **Répond à** | « Mission conteneurs », « Annulation mission », « Assignation » |
| **Priorité** | Moyenne |

### 13. `TERMINAL_TRANSFER_STATUS` — Transfert inter-site (HINO/CIT)

| | |
|:---|:---|
| **Écran N4** | À confirmer avec votre admin Navis — dépend de la manière dont le transfert HINO/CIT est modélisé dans votre instance (mouvement de cour, ou déplacement entre deux facilities/complexId distincts) |
| **Paramètre** | Numéro de conteneur |
| **Colonnes à inclure** | Statut du transfert, Site d'origine, Site de destination, Date |
| **Répond à** | « Transfert conteneur HINO/CIT » |
| **Priorité** | Basse-Moyenne — dépend de la fréquence réelle de ce cas |

---

## Filtres volontairement écartés à ce stade

- **Rien côté facturation/paiement** : c'est déjà entièrement porté par YABA (Billing Office
  virtuel) et ne doit pas être dédoublé côté Navis — risque de désynchronisation entre les deux
  sources de vérité.
- **Rien en écriture** (holds, confirmations) : l'Universal Query API est strictement en lecture
  seule par conception ; toute action réelle reste hors périmètre du chatbot.

---

## Une fois les filtres créés

Communiquez-moi, pour chacun :
1. Le **nom exact** sous lequel vous l'avez enregistré (s'il diffère du nom proposé ci-dessus).
2. Le **nom réel du champ paramétré** tel qu'affiché dans le prédicat N4 (ex. `UNIT_NBR`, `CNTR_NO`… selon votre configuration — ça varie d'une instance à l'autre).
3. Les **coordonnées de portée** de votre instance (`operatorId`, `complexId`, `facilityId`, `yardId`).
4. L'**URL et le port** du serveur N4 joignables depuis l'environnement YABA, et si un accès réseau spécifique (VPN, règle firewall) est nécessaire.

Avec ces éléments, je branche le client `NavisUniversalQueryClient` déjà esquissé sur vos vrais
filtres et je peux commencer à tester des appels réels.
