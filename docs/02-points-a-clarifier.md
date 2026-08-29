# Points à Clarifier et Décisions Métier Restant à Prendre
## Plateforme Digitale CIT — v1.0

> **Document** : `docs/02-points-a-clarifier.md`  
> **Référence CDC** : CIT-CIV-DSI-RFP-XXXX v1.0 — 12 août 2026  
> **Révisé le** : 28 août 2026 (révision post-audit — nouveaux points PC-24 à PC-26 ajoutés, PC-02/PC-13/PC-18 reformulés, risques ajoutés)  
> **Statut** : Pré-analyse — document à soumettre au métier pour validation

---

## Mode d'emploi

Ce document regroupe :

1. **Les ambiguïtés et lacunes** du cahier des charges identifiées lors de l'analyse
2. **Les décisions métier** restant à prendre explicitement mentionnées par le CDC (section 25)
3. Les points d'architecture et d'acquisition dont la réponse conditionne la conception

Chaque point est renseigné avec :

| Champ | Description |
|-------|-------------|
| **Niveau** | BLOQUANT · CRITIQUE · IMPORTANT · SOUHAITABLE |
| **Bloque** | SPECIFICATION · ARCHITECTURE · IMPLEMENTATION · PRODUCTION |
| **Source CDC** | Section et/ou référence d'exigence concernée |
| **Décision attendue** | Qui doit décider et comment |
| **Recommandation** | Observation éventuelle de l'analyste (ne constitue pas une exigence) |

**Niveaux :**
- 🔴 **BLOQUANT** : doit être tranché avant tout début de spécification / conception du composant concerné
- 🟠 **CRITIQUE** : doit être tranché avant la phase de conception détaillée
- 🟡 **IMPORTANT** : doit être tranché avant le début de la réalisation
- 🟢 **SOUHAITABLE** : peut être tranché pendant la réalisation, n'est pas bloquant

---

## Catégorie 1 — Volumétrie et dimensionnement

### PC-01 — Volumétrie opérationnelle cible

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🔴 BLOQUANT |
| **Bloque** | ARCHITECTURE |
| **Source CDC** | §25 · NFR-05 |
| **Exigences** | NFR-05 |

**Description** : Le CDC §25 demande de confirmer les données de volumétrie lors de la phase de cadrage. Elles sont nécessaires pour dimensionner l'infrastructure, le stockage, les connexions simultanées et les SLA techniques.

**Questions à poser au métier :**
- Combien de tickets par jour par module (Billing Office Import, Export, Customer Complaint, Enrôlement) ?
- Quelle est la taille moyenne et maximale des pièces jointes (BL, facture, preuve de paiement, déclaration) ?
- Quel est le nombre d'utilisateurs simultanés attendu en heure de pointe (côté client et côté agent) ?
- Quel est le volume de tickets archivés à conserver (durée de conservation × volume journalier) ?
- Quelle est la saisonnalité des flux (pics d'activité, escales, campagnes agricoles export) ?

**Décision attendue** : Atelier de cadrage avec équipes Billing Office, Gate et DSI. Fourniture d'une volumétrie cible validée.

**Risque associé** : RSQ-05 (sous-estimation de la volumétrie documentaire).

---

### PC-02 — Latence et fréquence de rafraîchissement de la supervision temps réel

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🟠 CRITIQUE |
| **Bloque** | ARCHITECTURE |
| **Source CDC** | SUP-01 · SUP-02 · SUP-03 · ALE-01 |

**Description** : Le CDC exige une « vue temps réel » et des alertes instantanées, mais ne précise pas la latence acceptable ni la fréquence de rafraîchissement.

**Questions à poser au métier :**
- Quelle est la latence maximale acceptable pour la supervision temps réel (ex. 5 secondes, 30 secondes, 1 minute) ?
- Les alertes doivent-elles être déclenchées en quasi-instantané ou un rafraîchissement à intervalle régulier est-il acceptable ?
- La vue superviseur doit-elle se mettre à jour automatiquement sans action de l'utilisateur ?

**Décision attendue** : Le métier définit une latence maximale acceptable. Cette valeur conditionnera le choix d'architecture côté serveur (à décider lors de la conception technique, après validation métier).

---

## Catégorie 2 — Rôles et habilitations

### PC-03 — Rôles non définis dans la section 6 du CDC

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🔴 BLOQUANT |
| **Bloque** | SPECIFICATION |
| **Source CDC** | §6 · IMP-13 · AVE-02 · AVE-03 · CON-01 · CON-02 |

**Description** : Trois responsabilités fonctionnelles sont mentionnées dans les exigences mais n'existent pas en tant que rôle applicatif distinct dans la liste officielle (section 6).

| Responsabilité | Référence | Action concernée | Statut actuel |
|----------------|-----------|-----------------|---------------|
| **Caisse/Finance** | IMP-13 | Valide la preuve de paiement avant passage au Contrôle Documentaire | ROLE_TO_CONFIRM |
| **Agent de guichet** | CON-01, CON-02 | Reprend un dossier au guichet via le numéro de ticket | FUNCTIONAL_RESPONSIBILITY |
| **Intervenant Avoir/Exo** | AVE-02, AVE-03 | Instruit et décide des demandes d'avoir et d'exonération | ROLE_TO_CONFIRM |

> **Note CDC §25** : « La validation des preuves de paiement (rôle Caisse/Finance) est confirmée par le métier et figée dans le document. » Le rôle est donc confirmé comme responsabilité ; son mapping sur un profil applicatif distinct reste à confirmer.

**Questions à poser :**
- Le rôle Caisse/Finance est-il un rôle applicatif propre avec sa file, ou est-ce une action assignée à un agent appartenant à la file Facturation ?
- La responsabilité « Agent de guichet » est-elle assurée par un des rôles DEFINED_ROLE existants (ex. Pilote Manifeste, Agent Gate) ou est-ce un rôle applicatif distinct ?
- Qui instruit les demandes d'avoir et d'exonération (Finance, Facturation, un rôle dédié) ?
- Les workflows Avoir/Exonération font-ils intervenir des personnes extérieures à la liste de la section 6 ?

**Décision attendue** : Atelier avec direction métier pour finaliser le modèle RBAC complet.

---

### PC-04 — Droits croisés des superviseurs

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🟠 CRITIQUE |
| **Bloque** | SPECIFICATION |
| **Source CDC** | SUP-04 · DIS-02 · MAS-01 |

**Description** : Le CDC mentionne que le superviseur dispose d'un « périmètre » mais ne précise pas si le périmètre est limité à un service/module ou est global.

**Questions à poser :**
- Combien de niveaux hiérarchiques de supervision sont prévus (superviseur par file, par service, global) ?
- Le périmètre du superviseur est-il limité aux modules ou files de son propre service ?
- Un superviseur peut-il intervenir sur les files d'un autre service (ex. superviseur Billing Office intervenant dans la file Customer Complaint) ?
- L'administrateur fonctionnel a-t-il aussi des droits de supervision ?

**Décision attendue** : Définition du modèle de supervision par la direction métier.

---

### PC-05 — Habilitation des agents en lecture croisée (traitement parallèle IES)

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🟡 IMPORTANT |
| **Bloque** | SPECIFICATION |
| **Source CDC** | IMP-40 · SEC-02 |

**Description** : Pour le traitement parallèle IES, deux rôles agissent sur le même ticket avec des champs réservés distincts. Les règles précises de cloisonnement au niveau du champ ne sont pas détaillées.

**Questions à poser :**
- Quels champs spécifiques sont réservés au Pilote Manifeste et lesquels au Pilote Contrôle Documentaire dans le cadre du traitement parallèle IES ?
- Le Pilote Manifeste peut-il voir (en lecture) les champs renseignés par le Pilote CD (et vice versa) ?
- Ces règles de cloisonnement s'appliquent-elles dans d'autres cas que le traitement IES ?

**Décision attendue** : Atelier technique avec les Pilotes Manifeste et CD pour modéliser les champs réservés.

---

## Catégorie 3 — Workflow Billing Office

### PC-06 — Comportement post-rejet définitif

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🟠 CRITIQUE |
| **Bloque** | SPECIFICATION |
| **Source CDC** | SOC-06 · IMP-04 · EXP-02 |

**Description** : Le CDC précise qu'un agent peut rejeter un ticket avec motif obligatoire, mais ne décrit pas le comportement post-rejet.

**Questions à poser :**
- Après un rejet du Pilote Manifeste ou du Back Office, le ticket est-il automatiquement clôturé ou passe-t-il dans un état permettant au client de corriger et re-soumettre ?
- Si le client peut corriger, crée-t-il un nouveau ticket ou modifie-t-il le ticket existant ?
- Combien de tentatives de correction sont autorisées avant clôture définitive ?
- Le ticket rejeté est-il archivé ou le client peut-il le supprimer ?

**Décision attendue** : Décision métier sur le cycle de vie post-rejet.

---

### PC-07 — Matrice des transitions autorisées entre files

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🔴 BLOQUANT |
| **Bloque** | SPECIFICATION |
| **Source CDC** | §9.4 · TRF-06 |

**Description** : Le CDC §9.4 fournit une illustration de la matrice des transitions et précise explicitement que « les valeurs sont à définir avec le métier ».

**Action requise** : Atelier avec les responsables métiers pour valider :
- La liste exhaustive des files de traitement (y compris files pour Avoir/Exo, Caisse/Finance, Enrôlement si nouvelles files nécessaires)
- Pour chaque file source, les destinations autorisées
- Les rôles habilités à initier chaque type de transfert
- Les motifs de transfert disponibles par transition

**Décision attendue** : Matrice complète validée par les responsables des services concernés.

---

### PC-08 — Liste exacte des pièces exigées par étape et par parcours

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🔴 BLOQUANT |
| **Bloque** | SPECIFICATION |
| **Source CDC** | §25 · SOC-07 · IMP-01 · EXP-01 · ENR-01 |

**Description** : Le CDC §25 demande de valider la liste exacte des pièces exigées avec les responsables métiers. SOC-07 précise que les types de pièces sont paramétrables par étape.

**Action requise** : Validation avec les responsables métiers de :
- La liste des pièces exigées à la soumission import (par type de client)
- La liste des pièces exigées à la soumission export (par type de client)
- Les pièces exigées pour un dossier d'enrôlement transporteur
- Les pièces exigées pour une demande d'avoir et d'exonération
- Les justificatifs exigés pour la création d'un compte client (par type)

**Décision attendue** : Listes validées par les équipes Billing Office et Gate.

---

### PC-09 — Règle d'unicité sur les bookings export (UNI-04)

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🟡 IMPORTANT |
| **Bloque** | SPECIFICATION |
| **Source CDC** | UNI-04 (Should) |

**Description** : UNI-04 indique qu'une règle d'unicité sur le numéro de booking « pourra être mise en œuvre après validation métier » pour l'export.

**Questions à poser :**
- Faut-il implémenter UNI-04 dès le premier incrément ou dans un incrément ultérieur ?
- Y a-t-il des cas légitimes où un même numéro de booking peut faire l'objet de plusieurs demandes actives simultanées ?
- Les règles d'unicité (BL import, booking export) doivent-elles s'appliquer par société cliente ou globalement ?

**Décision attendue** : Décision métier sur le périmètre d'UNI-04.

---

### PC-10 — Alternative présentielle (IMP-15) : acteurs et statuts

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🟡 IMPORTANT |
| **Bloque** | SPECIFICATION |
| **Source CDC** | IMP-15 (Should) · CON-01-03 |

**Description** : IMP-15 décrit que le client peut choisir de finaliser physiquement au Billing Office. Le processus de bascule n'est pas détaillé.

**Questions à poser :**
- Qui peut déclencher la bascule vers le présentiel : le client seulement ou aussi un agent ?
- Quels statuts le ticket prend-il lors de la bascule (ex. « En attente RDV », « Finalisé en présentiel ») ?
- Le parcours physique utilise-t-il les mêmes files de traitement que le parcours digital ?
- Quelles actions restent réalisables en ligne après la prise de RDV ?

**Décision attendue** : Atelier avec l'équipe Billing Office pour définir les statuts et les règles de bascule.

---

### PC-11 — Simulation de proforma : rattachement à un ticket (SIM-08)

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🟡 IMPORTANT |
| **Bloque** | SPECIFICATION |
| **Source CDC** | SIM-08 (Should) |

**Description** : SIM-08 indique que le client peut rattacher une simulation sauvegardée à un ticket de parcours. Le mécanisme de rattachement n'est pas précisé.

**Questions à poser :**
- Le rattachement est-il un simple lien (référence) entre la simulation et le ticket, ou implique-t-il que les données de la simulation pré-remplissent des champs du ticket ?
- Une simulation peut-elle être rattachée à plusieurs tickets, ou à un seul ?
- Quelle est la valeur du rattachement d'un point de vue métier (preuve, pré-remplissage, archivage) ?

**Décision attendue** : Décision métier sur la valeur et le mécanisme du rattachement.

---

## Catégorie 4 — SLA

### PC-12 — Valeurs cibles des SLA par module/étape/file

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🟠 CRITIQUE |
| **Bloque** | IMPLEMENTATION |
| **Source CDC** | §25 · SLA-01 · SLA-02 · SLA-05 |

**Description** : Le CDC §25 précise que les valeurs cibles des SLA seront déterminées avec le métier. Elles sont nécessaires pour configurer le paramétrage initial et pour concevoir les tests de recette.

**Action requise** : Atelier avec les responsables métiers pour définir :
- Le délai cible de prise en charge par file
- Le délai cible de traitement par étape
- Les seuils d'alerte (50%, 80%) et d'escalade par module
- La définition des « heures ouvrées » de chaque service (Billing Office, Gate, Support)
- Les règles de calcul du SLA pendant les jours non ouvrés (jours fériés, week-ends)

**Décision attendue** : Tableau des SLA cibles validé par les chefs de service.

---

## Catégorie 5 — Infrastructure et déploiement

### PC-13 — Choix d'hébergement

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🟠 CRITIQUE |
| **Bloque** | ARCHITECTURE |
| **Source CDC** | §25 · NFR-09 · §23 |

**Description** : Le CDC §25 et NFR-09 laissent ouverte la question de l'hébergement selon le scénario de réalisation retenu.

**Questions à poser :**
- L'hébergement sera-t-il sur-site (infrastructure CIT/AGL) ou dans un environnement cloud ?
- Des contraintes de souveraineté des données s'appliquent-elles (données à héberger en Côte d'Ivoire ou dans la zone CEDEAO) ?
- Quelles sont les contraintes de performance réseau pour les utilisateurs mobiles (clients via smartphone) ?
- Y a-t-il une infrastructure existante à réutiliser (serveurs, bases de données, annuaire LDAP/AD pour les agents) ?

**Décision attendue** : Décision DSI/Direction sur le scénario de réalisation et le choix d'hébergement. Cette décision conditionne l'architecture technique.

---

### PC-14 — Politique de conservation et d'archivage

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🟠 CRITIQUE |
| **Bloque** | ARCHITECTURE |
| **Source CDC** | §25 · SOC-09 · SEC-06 |

**Description** : Le CDC §25 demande une validation de la politique de conservation. L'archivage automatique post-clôture est requis (SOC-09), mais les durées légales ne sont pas précisées.

**Questions à poser :**
- Quelle est la durée légale de conservation des tickets et documents en Côte d'Ivoire (droit commercial, réglementation portuaire) ?
- Les archives doivent-elles être stockées dans la même base de données ou dans un stockage froid séparé ?
- Y a-t-il une politique de purge (suppression définitive au-delà d'une durée) ou une conservation permanente ?
- Les données personnelles des chauffeurs (enrôlement) ont-elles une durée de conservation spécifique (ARTCI) ?
- Quelle est la durée de conservation du journal d'audit ?

**Décision attendue** : Validation juridique et DSI sur les durées de conservation.

**Risque associé** : RSQ-05 (sous-estimation de la volumétrie documentaire).

---

### PC-15 — Workflow Avoir/Exonération : circuit de validation interne Finance

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🟠 CRITIQUE |
| **Bloque** | SPECIFICATION |
| **Source CDC** | §25 · AVE-02 |

**Description** : Le CDC §25 confirme que le statut exact du workflow Avoir/Exonération est « à formaliser avec la Finance ».

**Action requise** : Atelier avec la direction Finance pour définir :
- Les étapes du workflow de validation d'un avoir (intervenants, décisions, délais)
- Les étapes du workflow d'exonération (identique ou différent ?)
- Les niveaux d'autorisation (seuil de montant déclenchant un niveau de validation supplémentaire ?)
- Les documents à joindre (mandat, décision interne, etc.)
- Les statuts spécifiques à ces workflows

**Décision attendue** : Workflow formalisé par la direction Finance avant conception détaillée.

---

## Catégorie 6 — Notifications et canaux

### PC-16 — Souscription aux services WhatsApp/SMS

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🟠 CRITIQUE |
| **Bloque** | PRODUCTION |
| **Source CDC** | §25 · COM-01 · ENR-04 |

**Description** : Le CDC §25 signale que l'envoi WhatsApp/SMS suppose la souscription à un service d'envoi et est à chiffrer.

**Questions à poser :**
- Quel type de fournisseur de messagerie mobile est envisagé dans le contexte ivoirien ?
- Les notifications WhatsApp seront-elles via l'API officielle WhatsApp Business (nécessite validation par le fournisseur) ou via un opérateur/agrégateur local ?
- L'envoi SMS doit-il couvrir uniquement les numéros ivoiriens (+225) ou aussi des numéros internationaux ?
- Quel budget d'exploitation est prévu pour les envois (nombre de messages/mois estimé) ?

**Recommandation** : Le démarrage sans WhatsApp/SMS est possible (e-mail + notifications portail) ; les canaux mobiles peuvent être ajoutés sans impact sur le workflow — RSQ-04.

**Décision attendue** : Décision DSI/Finance sur le fournisseur et le budget.

---

### PC-17 — Événements déclencheurs de notifications : liste initiale

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🟡 IMPORTANT |
| **Bloque** | IMPLEMENTATION |
| **Source CDC** | COM-03 · NOT-02 |

**Description** : Le CDC précise que les événements déclencheurs et modèles de messages sont paramétrables, mais ne fournit pas la liste initiale de configuration.

**Action requise** : Validation avec le métier de la liste des événements déclencheurs par module et par canal pour le paramétrage initial.

**Liste de départ proposée (à valider, non exhaustive) :**

| Événement | Canal(ux) suggéré(s) |
|-----------|---------------------|
| Ticket créé (confirmation) | Portail + E-mail |
| Complément demandé | Portail + E-mail + WhatsApp |
| Ticket rejeté (avec motif) | Portail + E-mail |
| Facture disponible | Portail + E-mail |
| Preuve de paiement reçue | Portail + E-mail |
| Paiement validé | Portail + E-mail |
| Documents finaux disponibles (BS, BADT, BEE) | Portail + E-mail + WhatsApp |
| Ticket clôturé | Portail + E-mail |
| Ticket transféré (impact parcours) | Portail + E-mail |
| Relance absence de réponse | E-mail + WhatsApp |
| Rendez-vous confirmé | Portail + SMS |
| Dossier enrôlement validé | SMS + WhatsApp |
| Convocation photo/badge | SMS + WhatsApp |

**Décision attendue** : Liste validée par les équipes métier lors du paramétrage initial.

---

## Catégorie 7 — Assistant IA

### PC-18 — Définition fonctionnelle de l'assistant IA et conditions de déploiement

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🟡 IMPORTANT |
| **Bloque** | SPECIFICATION |
| **Source CDC** | IA-01 à IA-07 |

**Description** : Le CDC spécifie les fonctionnalités de l'assistant IA (réponses en français, orientation, escalade, base de connaissances administrable) mais ne précise pas les conditions de déploiement et de gouvernance.

**Questions à poser :**
- Les données de la base de connaissances (FAQ, procédures) peuvent-elles transiter via un service externe, ou doivent-elles rester sur l'infrastructure de CIT (contraintes de souveraineté) ?
- Quelle langue est prioritaire pour l'assistant IA (français standard ou français ivoirien avec des termes métier locaux) ?
- L'assistant IA doit-il être disponible 24h/24 (implications sur la disponibilité de l'infrastructure) ?
- Qui est responsable de l'administration de la base de connaissances (DSI, équipe métier) ?

**Recommandation** : Le choix de la solution technique (nature du moteur IA) sera décidé lors de la phase d'architecture, après validation des réponses aux questions ci-dessus.

**Décision attendue** : Décision métier/DSI sur les contraintes de souveraineté et de gouvernance.

---

## Catégorie 8 — Autres décisions métier

### PC-19 — Nombre maximal de BL par soumission import (IMP-01)

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🟡 IMPORTANT |
| **Bloque** | IMPLEMENTATION |
| **Source CDC** | IMP-01 |

**Description** : IMP-01 précise « jusqu'à 5 BL par demande ». Cette limite est-elle une contrainte métier figée ou une valeur configurable par défaut ?

**Questions à poser :**
- La limite de 5 BL par soumission est-elle une contrainte métier validée ou une valeur par défaut à paramétrer ?
- Des opérateurs soumettent-ils régulièrement plus de 5 BL simultanément ?

**Décision attendue** : Décision Billing Office sur le caractère figé ou paramétrable de cette limite.

---

### PC-20 — Clôture manuelle pour absence de réponse : rôle habilité (CYC-04)

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🟡 IMPORTANT |
| **Bloque** | SPECIFICATION |
| **Source CDC** | CYC-04 · CYC-05 |

**Description** : Le CDC précise que la clôture pour absence de réponse reste « une décision manuelle d'un agent habilité ». Le profil exact de cet agent n'est pas précisé.

**Questions à poser :**
- Quel rôle a l'habilitation pour clôturer un ticket pour absence de réponse (le Pilote de la file, le Superviseur, tout agent de la file) ?
- Cette habilitation est-elle la même pour tous les modules ?

**Décision attendue** : Décision métier par module sur les rôles habilités.

---

### PC-21 — Dossier multi-BL : comportement en cas d'échec partiel (UNI-01)

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🟡 IMPORTANT |
| **Bloque** | SPECIFICATION |
| **Source CDC** | SOC-11 · IMP-02 · UNI-01 · UNI-03 |

**Description** : Lors d'une soumission de plusieurs BL, si l'un d'eux est bloqué par la règle d'unicité, le comportement attendu n'est pas précisé.

**Questions à poser :**
- Si un BL parmi une soumission multi-BL est bloqué par la règle d'unicité (UNI-01), la soumission est-elle entièrement bloquée ou les autres tickets sont-ils créés normalement ?
- Le client est-il informé BL par BL du résultat de la vérification d'unicité ?

**Décision attendue** : Décision Billing Office sur le comportement en cas d'échec partiel.

---

### PC-22 — Gestion de l'assistant IA dans plusieurs langues

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🟢 SOUHAITABLE |
| **Bloque** | IMPLEMENTATION |
| **Source CDC** | IA-01 · NFR-07 |

**Description** : NFR-07 indique que l'interface devrait être disponible en anglais (Should). IA-01 spécifie les réponses de l'assistant en français uniquement.

**Questions à poser :**
- L'assistant IA doit-il répondre en anglais si le client interagit en anglais ?
- La base de connaissances doit-elle être traduite en anglais ?

**Décision attendue** : Décision métier sur le périmètre linguistique de l'assistant.

---

### PC-23 — Critères de recherche de tickets par profil

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🟢 SOUHAITABLE |
| **Bloque** | SPECIFICATION |
| **Source CDC** | CON-02 · SUP-04 |

**Description** : La recherche de tickets est mentionnée à plusieurs niveaux (guichet, superviseur) sans préciser les champs de recherche exhaustifs par profil.

**Action requise** : Valider la liste des critères de recherche par profil (client, agent, superviseur, admin) :

| Critère | Client | Agent | Superviseur | Admin |
|---------|--------|-------|-------------|-------|
| N° de ticket | Oui | Oui | Oui | Oui |
| N° de BL / booking | Oui | Oui | Oui | Oui |
| Nom / N° compte client | Non | Oui | Oui | Oui |
| Statut | Oui | Oui | Oui | Oui |
| Date de création / clôture | Oui | Oui | Oui | Oui |
| Opérateur assigné | Non | Non | Oui | Oui |
| File courante | Non | Oui | Oui | Oui |
| Module | Oui | Oui | Oui | Oui |

**Décision attendue** : Validation de la matrice de recherche par profil lors de la conception détaillée.

---

### PC-24 — Cycle de vie du dossier lorsque tous ses tickets sont clôturés ou annulés

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🟡 IMPORTANT |
| **Bloque** | SPECIFICATION |
| **Source CDC** | SOC-11 · CYC-01-03 · SOC-09 |

**Description** : Le CDC crée un « dossier » regroupant les tickets d'une même demande (SOC-11). Si tous les tickets d'un dossier sont clôturés ou annulés, le comportement du dossier n'est pas précisé.

**Questions à poser :**
- Lorsque tous les tickets d'un dossier sont clôturés ou annulés, le dossier est-il automatiquement archivé ? Reste-t-il consultable ?
- Un dossier peut-il rester ouvert si certains de ses tickets sont annulés mais d'autres sont encore actifs ?
- Le numéro de dossier peut-il être réutilisé pour une nouvelle demande, ou est-il définitivement associé aux tickets originaux ?
- Le client peut-il consulter un dossier clôturé avec l'ensemble de ses tickets archivés ?

**Décision attendue** : Décision métier sur le cycle de vie du dossier à l'issue du traitement de ses tickets.

---

### PC-25 — Notifications internes CIT (agents)

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🟡 IMPORTANT |
| **Bloque** | SPECIFICATION |
| **Source CDC** | MSG-04 · ALE-01 · SUP-01 |

**Description** : Les points à clarifier existants traitent uniquement des notifications clients. Le CDC mentionne aussi des notifications à destination des agents (MSG-04 : notification par mention ; ALE-01 : alertes de supervision). Les préférences de canal et les règles de notification des agents ne sont pas décrites.

**Questions à poser :**
- Quels événements déclenchent une notification à un agent (ex. ticket réaffecté, SLA bientôt dépassé dans sa file, mention @Nom dans une note interne) ?
- Les notifications aux agents se font-elles uniquement dans le portail, ou aussi par e-mail professionnel CIT ?
- Un agent peut-il configurer ses préférences de notification ?
- Les alertes SLA à destination du superviseur sont-elles configurables (canal, destinataires) ?

**Décision attendue** : Décision métier/DSI sur les règles de notification interne CIT.

---

### PC-26 — Expérience client IES : navigation entre le portail CIT et la plateforme IES

| Champ | Valeur |
|-------|--------|
| **Niveau** | 🟠 CRITIQUE |
| **Bloque** | SPECIFICATION |
| **Source CDC** | IMP-40 · IMP-42 · EXP-04 |

**Description** : Pour les clients IES, le portail CIT est utilisé pour le dépôt et le suivi du ticket, mais la facturation, le paiement et les documents finaux passent par la plateforme IES (IMP-42). Le CDC précise que le portail CIT fonctionne de manière autonome, sans interface automatisée avec IES. Aucun point ne précise comment le client IES navigue entre les deux plateformes.

**Questions à poser :**
- Lorsque le ticket portail CIT est prêt pour la finalisation IES (jointure terminée), comment le client IES est-il orienté vers la plateforme IES ? Le portail CIT affiche-t-il un lien, un message, ou rien ?
- Existe-t-il un mécanisme d'authentification unique (SSO) entre les deux plateformes, ou le client doit-il se connecter séparément à chaque plateforme ?
- Le statut du ticket portail CIT est-il mis à jour manuellement par un agent après que le client a finalisé sur IES, ou est-ce le client lui-même qui indique la finalisation ?

**Décision attendue** : Décision DSI/IES sur le mécanisme de transition entre les deux plateformes.

---

## Risques identifiés par le CDC (§26)

> **Nature** : Les éléments suivants sont les **risques projet** définis par le CDC (§26). Ce ne sont pas des décisions à prendre mais des facteurs de risque à prendre en compte lors de la planification.

| # | Risque | Mesure de maîtrise (CDC) | Points à clarifier associés |
|---|--------|--------------------------|------------------------------|
| RSQ-01 | Adoption insuffisante par les clients | Communication, simplicité du portail, pilote avec clients volontaires | PC-18 (IA) |
| RSQ-02 | Ressaisie et charge agents | Ergonomie back-office optimisée, architecture API ouverte (NFR-08) | PC-13 (hébergement) |
| RSQ-03 | Divergence ticket / réalité systèmes (IPAKI, IRIS) | Procédures opératoires claires, contrôles de cohérence en recette | PC-07 (transitions) |
| RSQ-04 | Retard de paramétrage des canaux WhatsApp/SMS | Démarrage possible avec e-mail + portail ; canaux mobiles ajoutables sans impact workflow | PC-16 (souscription canaux) |
| RSQ-05 | Sous-estimation de la volumétrie documentaire | Dimensionnement validé en cadrage ; politique de purge/archivage | PC-01 (volumétrie), PC-14 (conservation) |
| RSQ-06 | Dépendance à un éditeur (scénario C) | Exigence de réversibilité contractuelle — export complet des données | Réversibilité §23.1 |

---

## Synthèse par niveau

### 🔴 Points BLOQUANTS (4)

| # | Point | Composant bloqué |
|---|-------|-----------------|
| PC-01 | Volumétrie opérationnelle | Architecture infrastructure |
| PC-03 | Rôles manquants (Caisse/Finance, Agent guichet, Intervenant Avoir/Exo) | Modèle RBAC + files de traitement |
| PC-07 | Matrice des transitions entre files | Moteur de workflow |
| PC-08 | Liste des pièces exigées par étape | GED + formulaires client |

### 🟠 Points CRITIQUES (9)

| # | Point | Composant concerné |
|---|-------|-------------------|
| PC-02 | Latence supervision temps réel | Architecture technique |
| PC-04 | Droits croisés des superviseurs | Modèle RBAC |
| PC-06 | Comportement post-rejet | Workflow tickets |
| PC-12 | Valeurs cibles des SLA | Moteur SLA |
| PC-13 | Choix d'hébergement | Infrastructure |
| PC-14 | Politique de conservation | GED + base de données |
| PC-15 | Workflow Avoir/Exonération | Module Billing Office |
| PC-16 | Souscription canaux SMS/WhatsApp | Module notifications |
| PC-26 | Expérience client IES entre les deux plateformes | Specification IMPORT/EXPORT IES |

### 🟡 Points IMPORTANTS (10)

| # | Point | Composant concerné |
|---|-------|-------------------|
| PC-05 | Cloisonnement champs traitement IES | Modèle de données / RBAC |
| PC-09 | Unicité booking export (UNI-04) | Module EXPORT |
| PC-10 | Bascule digital/présentiel (IMP-15) | Workflow Billing Office |
| PC-11 | Rattachement simulation → ticket (SIM-08) | Module Proforma |
| PC-17 | Événements déclencheurs de notifications | Module notifications |
| PC-19 | Limite BL par soumission (IMP-01) | Formulaire IMPORT |
| PC-20 | Clôture pour absence de réponse : rôle habilité | Workflow CYC-04 |
| PC-21 | Comportement soumission multi-BL en cas d'échec partiel | Règle UNI-01 |
| PC-24 | Cycle de vie du dossier (clôture de tous les tickets) | Modèle de données |
| PC-25 | Notifications internes CIT (agents) | Module notifications |

### 🟢 Points SOUHAITABLES (3)

| # | Point | Composant concerné |
|---|-------|-------------------|
| PC-18 | Assistant IA : conditions de déploiement | Module IA |
| PC-22 | Langues de l'assistant IA | Module IA |
| PC-23 | Critères de recherche de tickets par profil | Interface agent/superviseur |

---

## Récapitulatif

| Niveau | Nombre |
|--------|--------|
| 🔴 BLOQUANTS | 4 |
| 🟠 CRITIQUES | 9 |
| 🟡 IMPORTANTS | 10 |
| 🟢 SOUHAITABLES | 3 |
| **Total** | **26** |

---

*Document de pré-analyse — aucune modification des exigences du CDC ne résulte de ce document. Les réponses fournies par le métier seront intégrées dans la matrice des exigences et dans les spécifications Spec Kit.*
