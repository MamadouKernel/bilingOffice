# Registre des décisions DSI en attente

> **Document** : `docs/49-dsi-decision-register.md`
> **Établi le** : 2026-09-02, sur le commit `d80a126`
> **Objet** : centraliser les décisions qui conditionnent les **14 exigences non certifiées**.
> Aucune de ces questions ne peut être tranchée par l'équipe de développement.

---

## 1. Pourquoi ce registre

Le projet est à **174 / 188 (92,55 %)**. Les 14 exigences restantes ne sont pas bloquées par du
développement : **10 sur 14 attendent une décision ou un environnement**, pas du code.

Répartition des causes réelles :

| Cause | Exigences |
|:---|---:|
| Environnement de mesure indisponible | 3 |
| Décision DSI en attente | 9 |
| Infrastructure de sauvegarde absente | 1 |
| Contrat fournisseur absent | 1 |

**Correction importante par rapport à `docs/32`** : ces 14 exigences comptent **10 Must et
4 Should** au sens du CDC — et non une majorité de Could. Le détail figure au § 2 de
`docs/33-post-f017-global-cdc-matrix.md`.

---

## 2. Décisions attendues

### DEC-01 — Volumétrie opérationnelle cible

| Champ | Valeur |
|:---|:---|
| **Point à clarifier** | **PC-01** — Volumétrie opérationnelle cible (niveau : bloquant) |
| **Source CDC** | § 25 · NFR-05 |
| **Exigence impactée** | `NFR-05` (**Must**) — et, indirectement, `NFR-04` |
| **Statut** | `BLOCKED_DSI` — OPEN |

**Question.** Quels sont les ordres de grandeur cibles : utilisateurs simultanés en pointe,
tickets créés par jour, documents déposés par jour, taille moyenne et maximale d'un document,
horizon de croissance retenu ?

**Pourquoi c'est nécessaire.** Sans volumétrie, un tir de charge mesure un profil arbitraire :
le résultat ne prouve rien contre l'exigence. NFR-05 ne peut être ni spécifiée ni certifiée, et
la campagne NFR-04 elle-même perd sa valeur probante.

**Impact si absence.** `NFR-05` reste non certifiée. La certification `NFR-04` devient
contestable. Le dimensionnement de l'infrastructure ne peut pas être argumenté.

**Options.** (a) Mesure sur l'existant guichet ; (b) estimation métier avec marge ; (c) profil
provisoire explicitement qualifié de provisoire, à reconfirmer après six mois.

**Recommandation technique.** L'option (a) — les volumes du guichet physique actuel sont la
meilleure approximation disponible et sont observables. **Ceci reste une recommandation, non une
décision.**

**Décideur attendu** : DSI, avec le métier Billing Office.

---

### DEC-02 — Choix d'hébergement et conformité Groupe

| Champ | Valeur |
|:---|:---|
| **Point à clarifier** | **PC-13** — Choix d'hébergement (niveau : critique) |
| **Source CDC** | § 25 · NFR-09 · § 23 |
| **Exigence impactée** | `NFR-09` (**Should**) |
| **Statut** | `BLOCKED_DSI` — OPEN |

**Question.** Quelle cible d'hébergement : on-premise CIT, infrastructure AGL / Groupe, ou cloud ?
Quels standards Groupe s'imposent en matière de sécurité, de sauvegarde, de supervision et de
localisation des données ?

**Pourquoi c'est nécessaire.** NFR-09 est une exigence d'**hébergement**, pas de développement.
Elle est intégralement organisationnelle.

**Impact si absence.** `NFR-09` non certifiée. Le déploiement Staging reste impossible à cadrer,
ce qui bloque en cascade la certification environnementale F016.

**Options.** (a) On-premise CIT ; (b) datacenter Groupe / AGL ; (c) cloud souverain ; (d) cloud
public. Chacune emporte des conséquences distinctes sur la souveraineté des données et sur
l'exploitation.

**Recommandation technique.** Aucune. Ce choix relève de la politique du Groupe et non d'un
critère technique — toute préférence exprimée ici serait déplacée.

**Décideur attendu** : DSI / Direction des Systèmes d'Information Groupe.

---

### DEC-03 — Fournisseur SMS / WhatsApp

| Champ | Valeur |
|:---|:---|
| **Point à clarifier** | **PC-16** — Souscription aux services WhatsApp/SMS |
| **Exigence impactée** | `ENR-04` (**Should**) |
| **Statut** | `BLOCKED_PROVIDER` — OPEN |

**Question.** Quel fournisseur est contractualisé, pour quels canaux, avec quels quotas, quel
coût unitaire, quel SLA et quelles obligations réglementaires (ARTCI, RGPD) ?

**Pourquoi c'est nécessaire.** Le socle de notification livré en F009 dispose déjà de
l'abstraction de canal ; il ne manque que le **raccordement à un fournisseur réel**, qui suppose
un contrat, des identifiants et un environnement de test.

**Impact si absence.** `ENR-04` non certifiée. Le chauffeur n'est pas convoqué par SMS ou
WhatsApp — le canal In-App reste seul opérant.

**Options.** (a) Agrégateur international ; (b) opérateur local ivoirien ; (c) WhatsApp Business
API via un partenaire agréé ; (d) report de l'exigence.

**Recommandation technique.** Aucun fournisseur ne peut être recommandé sans les critères de coût,
de couverture et de conformité, qui ne relèvent pas de la technique. Le détail des informations à
recueillir figure dans `docs/43`.

**Décideur attendu** : DSI et Direction Achats.

---

### DEC-04 — Assistant conversationnel : cadrage et gouvernance

| Champ | Valeur |
|:---|:---|
| **Point à clarifier** | **PC-18** — Définition fonctionnelle de l'assistant IA et conditions de déploiement |
| **Source CDC** | IA-01 à IA-07 |
| **Exigences impactées** | `IA-01`, `IA-02`, `IA-03`, `IA-06`, `IA-07` (**Must**) · `IA-04`, `IA-05` (**Should**) |
| **Statut** | `BLOCKED_DSI` — OPEN |

**Question.** Quel modèle de langage, hébergé où, avec quelles garanties de confidentialité sur
les données client ? Quel budget ? Quelle politique de conservation des conversations ? Quel
niveau d'intervention humaine obligatoire ?

**Point d'attention majeur.** `docs/32` classe ces sept exigences en **Could** et leur attribue
des descriptions qui ne figurent pas au CDC. **Le texte contractuel en fait cinq Must et deux
Should**, portant toutes sur un **unique assistant conversationnel**. C'est le poste de dette le
plus important du reste à faire, et il est aujourd'hui sous-évalué dans les documents de pilotage.

**Impact si absence.** Sept exigences non certifiées, dont **cinq Must**. Le projet ne peut
atteindre 188 / 188 sans cet arbitrage.

**Options.** (a) Modèle auto-hébergé on-premise ; (b) cloud privé Groupe ; (c) service
d'entreprise avec engagement de non-réutilisation des données ; (d) API externe ; (e) renoncement
formel, à acter contractuellement.

**Recommandation technique.** Les exigences imposent un fonctionnement **en français**, une base
de connaissances **administrable sans développement**, la **citation des sources** et une
**escalade humaine sans perte de contexte**. Ces contraintes orientent vers une architecture de
type recherche augmentée sur base documentaire maîtrisée, plus que vers un modèle génératif libre.
**Ceci éclaire le choix, ne le remplace pas.** Le cadrage complet figure dans `docs/44`.

**Décideur attendu** : DSI, Direction Juridique (données personnelles), Direction Générale (budget).

---

### DEC-05 — Environnement Staging / UAT

| Champ | Valeur |
|:---|:---|
| **Exigences impactées** | `NFR-03`, `NFR-04`, `NFR-10` (**Must**) |
| **Statut** | `BLOCKED_ENVIRONMENT` — OPEN |

**Question.** Quand un environnement représentatif sera-t-il disponible, avec quelle URL, quel
certificat TLS, quelle base de recette, et quelle autorisation de générer de la charge ?

**Pourquoi c'est nécessaire.** Ces trois exigences sont **techniquement livrées** : sondes,
métrologie, mode maintenance et script de charge existent et sont vérifiés dans le dépôt. Elles
se démontrent par la **mesure d'un système déployé**, jamais par du code.

**Impact si absence.** Trois exigences Must restent non certifiées alors qu'aucun développement
ne manque. C'est le blocage au meilleur rapport valeur / effort du projet.

**Recommandation technique.** Prioriser cet environnement : il débloque trois exigences sans une
ligne de code. La procédure complète est prête — `docs/37`.

**Décideur attendu** : DSI / Exploitation.

---

### DEC-06 — Sauvegarde, restauration et plan de reprise

| Champ | Valeur |
|:---|:---|
| **Exigence impactée** | `NFR-06` (**Must**) |
| **Statut** | `BLOCKED_ENVIRONMENT` — OPEN |

**Question.** Quels RPO et RTO sont visés ? Quelle fréquence, quelle rétention, quel chiffrement,
quelle localisation des sauvegardes ? Qui exécute et qui contrôle ? À quelle périodicité la
restauration est-elle testée ?

**Pourquoi c'est nécessaire.** NFR-06 exige des sauvegardes régulières **et une procédure de
restauration**. Une procédure jamais exécutée ne prouve rien : l'exigence ne peut être certifiée
que par un test de restauration réel.

**Impact si absence.** `NFR-06` non certifiée. Risque de perte de données non couvert.

**Recommandation technique.** Le périmètre à sauvegarder ne se limite pas à la base : le stockage
documentaire en fait partie, et une sauvegarde de l'un sans l'autre produit un système incohérent
à la restauration. Détail dans `docs/40`.

**Décideur attendu** : DSI / Exploitation.

---

## 3. Suivi

| ID | Sujet | PC | Exigences | Statut | Date réponse | Décision finale |
|:---|:---|:---|:---|:---|:---|:---|
| DEC-01 | Volumétrie cible | PC-01 | NFR-05 | OPEN | — | — |
| DEC-02 | Hébergement | PC-13 | NFR-09 | OPEN | — | — |
| DEC-03 | Fournisseur SMS/WhatsApp | PC-16 | ENR-04 | OPEN | — | — |
| DEC-04 | Assistant conversationnel | PC-18 | IA-01..07 | OPEN | — | — |
| DEC-05 | Environnement Staging | — | NFR-03, NFR-04, NFR-10 | OPEN | — | — |
| DEC-06 | Sauvegarde / PRA | — | NFR-06 | OPEN | — | — |

---

## 4. Ordre de traitement suggéré

| Rang | Décision | Gain | Effort DSI |
|---:|:---|---:|:---|
| 1 | **DEC-05** — Staging | **+3 exigences Must** | Fourniture d'un environnement |
| 2 | **DEC-04** — Assistant IA | **+7 exigences, dont 5 Must** | Arbitrage de fond |
| 3 | DEC-01 — Volumétrie | +1 Must | Atelier métier |
| 4 | DEC-06 — Sauvegarde / PRA | +1 Must | Procédure d'exploitation |
| 5 | DEC-02 — Hébergement | +1 Should | Décision Groupe |
| 6 | DEC-03 — Fournisseur | +1 Should | Contractualisation |

DEC-05 vient en tête parce qu'elle ne demande **aucun arbitrage de fond** : uniquement la mise à
disposition d'un environnement. DEC-04 vient ensuite parce qu'elle porte le plus gros volume
d'exigences contractuellement contraignantes — et parce qu'elle a jusqu'ici été sous-évaluée.

---

## 5. Réserve

Les recommandations de ce document sont des **éclairages techniques**. Aucune ne constitue une
décision, et aucune ne doit être reprise comme telle dans un document contractuel sans validation
explicite du décideur désigné.
