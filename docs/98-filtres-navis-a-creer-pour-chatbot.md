# Filtres Navis N4 à créer pour le chatbot (Universal Query API)

> **Document** : `docs/98-filtres-navis-a-creer-pour-chatbot.md`
> **Établi le** : 2026-09-23
> **Pour qui** : l'administrateur Navis N4 côté CIT — chaque ligne ci-dessous est un filtre à créer
> et enregistrer dans l'UI N4 (avec le compte `n4api`), pour que l'Universal Query API puisse
> l'exécuter à distance.
>
> **Rappel du mécanisme** : l'API ne permet pas d'écrire une requête libre — elle exécute un filtre
> **déjà enregistré** dans N4, identifié par son nom exact (`filtername=...`). Le connecteur YABA
> appelle ensuite ce nom, en lecture seule, avec les paramètres listés ci-dessous.

---

## Comment créer un filtre (rappel express)

1. Se connecter à N4 **avec le compte `n4api`** (celui qui portera le privilège *Universal Query
   API Access - Allow* — l'API ne voit que les filtres créés par ce compte).
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
| **Colonnes à inclure** | Numéro conteneur, Statut/Category, Emplacement (bloc/rangée/niveau), Navire, Voyage, Sens (Import/Export), Date de déchargement (ou date de gate-in export), Ligne maritime |
| **Répond à** | « Où est mon conteneur ABCU1234567 ? », « Mon conteneur est-il déchargé ? » |
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
