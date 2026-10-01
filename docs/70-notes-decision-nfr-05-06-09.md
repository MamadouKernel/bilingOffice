# 70 — Notes de décision NFR-05 / NFR-06 / NFR-09

> **Objet.** Ces trois exigences du CDC v3 n'ont **aucune implémentation logicielle** : elles
> relèvent de décisions et de procédures d'exploitation (volumétrie, sauvegarde, hébergement).
> Ce document fournit, pour chacune, une **note de décision prête à valider par la DSI**. Tant
> que la décision n'est pas signée et la procédure exécutée sur l'environnement réel, le ticket
> ClickUp correspondant reste légitimement en **« à faire »**.
>
> Readiness technique associée : [41-backup-restore-pra-readiness.md](41-backup-restore-pra-readiness.md),
> [42-nfr05-load-test-readiness.md](42-nfr05-load-test-readiness.md),
> [43-nfr09-hosting-compliance-readiness.md](43-nfr09-hosting-compliance-readiness.md).

---

## NFR-05 — Volumétrie dimensionnante

**Exigence CDC.** Volumétrie dimensionnante à confirmer en phase de cadrage : tickets/jour,
taille des pièces jointes, nombre d'utilisateurs concurrents, rétention.

**Pourquoi ce n'est pas du code.** Ce sont des **chiffres métier** que seule la direction
métier/DSI peut confirmer ; ils conditionnent le dimensionnement infra, pas le logiciel.

**Hypothèses à faire valider (à remplacer par les chiffres réels CIT) :**

| Paramètre | Hypothèse de cadrage | À confirmer par |
|---|---|---|
| Tickets créés / jour | ___ (ex. 500) | Métier |
| Utilisateurs concurrents (pointe) | ___ (ex. 80) | Métier |
| Taille moyenne d'une pièce jointe | ___ (ex. 2 Mo) | Métier |
| Taille max d'une pièce jointe | ___ (ex. 20 Mo) | Métier / DSI |
| Rétention des documents | ___ (ex. 5 ans) | Conformité |
| Croissance annuelle estimée | ___ % | Métier |

**Décision attendue.** Valider ces chiffres → ils alimentent le test de charge (doc 42) et le
dimensionnement de l'hébergement (NFR-09). **Critère de clôture :** chiffres signés + test de
charge exécuté montrant la tenue (<3 s, cf. NFR-04) à la volumétrie cible.

---

## NFR-06 — Sauvegardes et restauration

**Exigence CDC.** Sauvegardes régulières (données **et** documents) et **procédure de
restauration testée**.

**Pourquoi ce n'est pas du code.** La plateforme ne gère pas ses propres sauvegardes : c'est une
responsabilité d'**exploitation** (SGBD + stockage documentaire + ordonnanceur).

**Note de décision à valider :**

| Élément | Proposition | À valider par |
|---|---|---|
| Fréquence sauvegarde base | Complète quotidienne + journaux (PITR) | DSI |
| Fréquence sauvegarde documents | Quotidienne (stockage objet versionné) | DSI |
| Rétention des sauvegardes | 30 j glissants + 1 mensuelle sur 12 mois | Conformité |
| RPO cible (perte max) | ≤ 24 h (ou ≤ 15 min si PITR) | Métier |
| RTO cible (reprise max) | ≤ 4 h | Métier |
| Lieu de stockage | Hors-site / autre zone | DSI |
| **Test de restauration** | Exercice documenté, **≥ 1×/trimestre** | DSI |

**Critère de clôture.** Procédure écrite **et** un exercice de restauration réussi, tracé (date,
périmètre, RPO/RTO constatés). Voir runbook doc 41.

---

## NFR-09 — Hébergement conforme aux standards du groupe

**Exigence CDC.** Hébergement conforme aux standards du groupe ; le choix (infrastructure
CIT/AGL ou cloud) sera arrêté en phase de cadrage.

**Pourquoi ce n'est pas du code.** C'est une **décision d'architecture/infra** (où et comment la
plateforme tourne), prise par la DSI groupe.

**Grille de décision à arbitrer :**

| Critère | Option A — Infra CIT/AGL | Option B — Cloud | Poids |
|---|---|---|---|
| Conformité standards groupe | ___ | ___ | Must |
| Souveraineté / localisation des données | ___ | ___ | Must |
| Coût (CAPEX vs OPEX) | ___ | ___ | Should |
| Élasticité / scale-out (cf. Redis multi-nœuds livré) | ___ | ___ | Should |
| Délai de mise à disposition | ___ | ___ | Should |
| Compétences d'exploitation disponibles | ___ | ___ | Could |

**Décision attendue.** DSI arrête l'option, documente la cible (zones, réseau, TLS, secrets) et
l'aligne sur les docs 54–58 (inventaire, matrice de conf., flux réseau, options de déploiement).
**Critère de clôture :** option signée + environnement provisionné conforme.

---

## Synthèse

| Exigence | Nature | Bloqueur | Clôture quand |
|---|---|---|---|
| NFR-05 | Chiffrage métier | Chiffres non confirmés | Chiffres signés + test de charge OK |
| NFR-06 | Procédure exploitation | Pas d'exercice de restauration | Procédure + restauration testée tracée |
| NFR-09 | Décision infra | Choix d'hébergement non arrêté | Option signée + env. provisionné |

Aucune de ces trois ne requiert de développement applicatif. Le rôle de l'équipe logicielle se
limite à fournir les éléments techniques (volumétrie applicative, points de sauvegarde, besoins
d'hébergement), déjà couverts par les docs 41/42/43.
