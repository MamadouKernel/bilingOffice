# Rapport d'achèvement et de préparation projet — post-F017

> ⚠️ **CORRECTION DU 2026-09-03, mise à jour le 2026-09-04 — le score CDC cité dans ce document est périmé.**
> L'audit indépendant `docs/88-audit-conformite-cdc.md` a confronté au code huit exigences que la
> matrice `docs/33` déclarait `CERTIFIED` : aucune n'était entièrement satisfaite. **Sept ont été
> corrigées et testées le 2026-09-04** (`SLA-01`, `SLA-02`, `SLA-05`, `ALE-02`, `ALE-03`, `SOC-07`,
> `COM-03`) ; `COM-01` reste ouverte faute de fournisseur de messagerie arbitré.
> **Le score courant est 173 / 188 (92,02 %)**, et non 174 / 188. Le reste de ce document conserve
> sa valeur ; seul le chiffre est à lire à travers cette correction.


> **Document** : `docs/52-project-completion-readiness-report.md`
> **Établi le** : 2026-09-02, sur le commit `d80a126`
> **Objet** : répondre à cinq questions — où en est le projet, ce qui est terminé, ce qui reste
> faisable sans la DSI, ce qui est réellement bloqué, et ce qu'il faut obtenir pour atteindre
> 188 / 188.

---

## 1. Baseline technique — vérifiée indépendamment

| Contrôle | Constaté | Annoncé | Écart |
|:---|:---|:---|:---|
| HEAD | `d80a126` | `d80a126` | — |
| Worktree | propre | propre | — |
| Build | SUCCESS | SUCCESS | — |
| Avertissements | **0** | 0 | — |
| Tests | **1041 / 1041** | 1041 | — |
| — Domain | 158 / 158 | 158 | — |
| — Application | 560 / 560 | 560 | — |
| — Infrastructure | 94 / 94 | 94 | — |
| — Web | 229 / 229 | 229 | — |
| Pending model changes | **0** | 0 | — |
| Migrations | 25 | — | — |

**Aucune divergence.** La baseline annoncée est intégralement confirmée.

---

## 2. Baseline CDC

| Statut | Nombre | Part |
|:---|---:|---:|
| `CERTIFIED` | **174** | 92,55 % |
| `IMPLEMENTED_NOT_ENVIRONMENT_PROVEN` | 3 | 1,60 % |
| Bloquées (`BLOCKED_DSI`, `BLOCKED_INFRA`, `BLOCKED_PROVIDER`) | 11 | 5,85 % |
| **Total** | **188** | 100 % |

Vérification : 174 + 3 + 11 = 188. **174 / 188 confirmé.**

### 2.1 Correction de la caractérisation du reste à faire

Trois erreurs factuelles de `docs/32` ont été corrigées et tracées :

| Élément | Valeur erronée | Valeur contractuelle |
|:---|:---|:---|
| `NFR-05` | Should | **Must** |
| `NFR-09` | Must | **Should** |
| `IA-01`..`IA-07` — priorité | « Could » (7) | **5 Must + 2 Should** |
| `IA-01`..`IA-07` — objet | 7 cas d'usage inventés | **Un assistant conversationnel** |

**Répartition réelle des 14 restantes : 10 Must, 4 Should.** Le pourcentage est inchangé ; la
nature de la dette l'est profondément.

`docs/02-points-a-clarifier.md` décrivait correctement l'assistant : **l'erreur avait été
introduite dans le document de clôture F017**, elle n'était pas héritée de l'analyse initiale.

---

## 3. Sécurité

| Sévérité | Nombre |
|:---|---:|
| `CRITICAL` | **0** |
| `HIGH` | **0** |
| `MEDIUM` | 4 |
| `LOW` | 1 |

Points solides : cloisonnement des couches sans exception, aucun secret ni URL externe, en-têtes
de sécurité complets, autorisation vérifiée côté serveur, API cloisonnée par société, journal
d'audit en ajout seul.

Les quatre points moyens — politique d'autorisation de repli absente, page morte routable,
logique dupliquée sur sept fichiers, horloge contournée dans Tarification — sont documentés dans
`docs/35`. **Aucun n'a été corrigé** : chacun touche une surface produit ou une signature
publique, hors du périmètre « correction locale incontestable ».

---

## 4. Recette UAT

| Élément | État |
|:---|:---|
| Plan de recette | **Prêt** — 24 scénarios, `docs/36` |
| Traçabilité | **Prête** — `docs/37` |
| Exigences citées par un test automatisé | **153 / 188** |
| Exigences sans citation d'identifiant | **35** — couvertes par scénario UAT |
| Environnement | **Absent** |

**Bloquant unique : `BLOCKED_ENVIRONMENT`.**

---

## 5. Environnement

| Élément | État |
|:---|:---|
| Staging / UAT | **Inexistant** |
| Certification F016 | **`NOT EXECUTED`** — instrumentation complète, campagne prête |
| Sauvegarde et PRA | **`NOT_PROVEN`** — aucun test de restauration |
| Tests de charge | **Non lançables** — profil non arrêté |

---

## 6. Décisions DSI en attente

| Réf. | Sujet | PC | Exigences | Statut |
|:---|:---|:---|:---|:---|
| DEC-01 | Volumétrie cible | PC-01 | NFR-05 (**Must**) | OPEN |
| DEC-02 | Hébergement | PC-13 | NFR-09 (Should) | OPEN |
| DEC-03 | Fournisseur SMS/WhatsApp | PC-16 | ENR-04 (Should) | OPEN |
| DEC-04 | Assistant conversationnel | PC-18 | IA-01..07 (**5 Must**, 2 Should) | OPEN |
| DEC-05 | Environnement Staging | — | NFR-03, NFR-04, NFR-10 (**Must**) | OPEN |
| DEC-06 | Sauvegarde et PRA | — | NFR-06 (**Must**) | OPEN |

---

## 7. Les cinq questions

### 7.1 Où en est réellement le projet ?

À **174 / 188 (92,55 %)**, avec une base technique saine : build propre, 1041 tests verts, schéma
synchronisé, architecture cloisonnée, aucune anomalie critique ni haute.

Le produit est **fonctionnellement recevable**. Il n'a jamais été déployé ailleurs que sur un
poste de développement.

### 7.2 Qu'est-ce qui est terminé ?

174 exigences certifiées, couvrant l'intégralité des modules métier : socle de tickets, Billing
Office import et export, réclamations, avoirs et exonérations, simulation de proforma, enrôlement
des transporteurs, notifications, guichet, brouillons, API, internationalisation, sécurité des
échanges, rétention réglementaire.

Trois exigences supplémentaires sont **techniquement livrées** mais non prouvées : NFR-03, NFR-04,
NFR-10.

### 7.3 Qu'est-ce qui reste faisable sans la DSI ?

| Action | Faisable |
|:---|:---|
| Corriger les 4 anomalies moyennes | **Oui** — avec recette associée |
| Remettre `docs/` et `specs/` sous version | **Oui** — décision de gouvernance, `docs/34` |
| Étendre le script de charge aux surfaces identifiées | **Oui** — sans les seuils |
| Compléter dictionnaire de données, documentation d'API, notes de version | **Oui** |
| Analyser les dépendances vulnérables | **Oui** |
| Préparer les textes de notification chauffeur | **Oui** |
| Définir les événements déclencheurs ENR-04 | **Oui** — relève du métier |

**Rien de tout cela n'augmente le compteur CDC.** Ce sont des travaux de qualité et de
préparation.

### 7.4 Qu'est-ce qui est réellement bloqué ?

Les 14 exigences restantes, **sans exception**. Aucune n'est bloquée par du développement :

- 3 attendent un environnement à mesurer ;
- 9 attendent un arbitrage DSI ;
- 1 attend une infrastructure de sauvegarde ;
- 1 attend un contrat fournisseur.

### 7.5 Que faut-il obtenir pour atteindre 188 / 188 ?

Six décisions, dans cet ordre de rendement.

---

## 8. Roadmap vers 188 / 188

| Rang | Levier | Exigences | Gain | Nature du travail restant |
|---:|:---|:---|---:|:---|
| 1 | **Environnement Staging** (DEC-05) | NFR-03, NFR-04, NFR-10 | **+3** | Campagne de mesure — **aucun code** |
| 2 | **Assistant conversationnel** (DEC-04) | IA-01 à IA-07 | **+7** | Arbitrage, puis développement complet |
| 3 | **Volumétrie** (DEC-01) | NFR-05 | +1 | Campagne de charge — aucun code |
| 4 | **Sauvegarde et PRA** (DEC-06) | NFR-06 | +1 | Procédure et test de restauration |
| 5 | **Hébergement** (DEC-02) | NFR-09 | +1 | Homologation |
| 6 | **Fournisseur** (DEC-03) | ENR-04 | +1 | Adaptateur, puis recette |

**Vérification arithmétique** : 174 + 3 + 7 + 1 + 1 + 1 + 1 = **188**. ✓

### Progression attendue

| Étape | CDC |
|:---|:---|
| Aujourd'hui | 174 / 188 — 92,55 % |
| Après DEC-05 | 177 / 188 — 94,15 % |
| Après DEC-01 | 178 / 188 — 94,68 % |
| Après DEC-06 | 179 / 188 — 95,21 % |
| Après DEC-02 | 180 / 188 — 95,74 % |
| Après DEC-03 | 181 / 188 — 96,28 % |
| Après DEC-04 | **188 / 188 — 100 %** |

**Le seul poste comportant un développement significatif est DEC-04.** Les six autres exigences
se débloquent par mesure, procédure ou contrat.

---

## 9. Verdict

### `UAT_READY_WITH_EXTERNAL_BLOCKERS`

**Prêt** : l'application compile sans avertissement, 1041 tests passent, aucune anomalie critique
ni haute, plan de recette et runbooks rédigés.

**Bloqué** : aucun environnement de recette n'existe. C'est le blocage unique et déterminant.

**À savoir avant de lancer la recette** : le reste à faire est plus contraignant que les documents
de pilotage ne l'indiquaient — **10 Must**, dont sept portant sur un assistant conversationnel
jamais cadré.

---

## 10. Action recommandée

**Obtenir l'environnement Staging.** Seul levier qui débloque trois exigences Must sans une ligne
de code ; procédure, scripts et critères sont prêts (`docs/38`).

**En parallèle, ouvrir l'arbitrage sur l'assistant conversationnel** : sept exigences, le plus
long à instruire, et le plus sous-évalué jusqu'ici.

---

## 11. Réserves de méthode

**Sur les 174 certifiées.** Leur statut est repris des rapports de clôture. Cet audit n'a pas
rejoué la démonstration exigence par exigence. Ont été vérifiés indépendamment : le comptage, les
priorités relues dans le texte contractuel, le rattachement aux features et aux tests. Les erreurs
trouvées l'ont été par cette vérification.

**Sur la traçabilité.** 153 exigences sur 188 sont citées par un test. Les 35 autres ne sont pas
nécessairement non testées, mais leur preuve n'est **pas opposable par recherche**.

**Sur la version des documents.** `docs/` et `specs/` ne sont plus suivis par Git. Les
spécifications de F016 et F017 n'ont **jamais** été versionnées. C'est le point le plus urgent
qui soit entièrement de notre ressort — `docs/34`.
