# GO / NO-GO — ouverture de la campagne de recette

> **Document** : `docs/66-staging-uat-go-no-go.md`
> **Établi le** : 2026-09-02, sur le commit `b87ad15`
> **Portée** : cette porte décide de l'**ouverture de la campagne de recette**. Elle ne décide pas
> du passage en production — c'est l'objet de `docs/47`.
> **Règle** : **aucun `PASS` sans preuve**. Une ligne sans preuve est `BLOCKED`, jamais `PASS`.

---

## Légende

| Statut | Signification |
|:---|:---|
| `PASS` | Vérifié, preuve disponible et référencée |
| `FAIL` | Vérifié, non conforme |
| `BLOCKED` | Non vérifiable — dépendance externe non levée |
| `N/A` | Sans objet, avec justification |

---

## 1. Ce qui dépend du projet — **prêt**

| # | Critère | Statut | Preuve |
|---:|:---|:---|:---|
| 1.1 | Compilation sans erreur | `PASS` | `dotnet build` — 0 erreur |
| 1.2 | Compilation sans avertissement | `PASS` | 0 avertissement |
| 1.3 | Tests automatisés au vert | `PASS` | **1041 / 1041** |
| 1.4 | Modèle de données aligné sur les migrations | `PASS` | 0 changement en attente |
| 1.5 | Aucun secret dans le dépôt | `PASS` | Contrôle statique |
| 1.6 | Aucune ressource externe chargée | `PASS` | Test automatisé dédié |
| 1.7 | Traçabilité Git rétablie | `PASS` | `docs/53` |
| 1.8 | Plan de recette rédigé | `PASS` | `docs/36` |
| 1.9 | Matrice de traçabilité des exigences | `PASS` | `docs/37` |
| 1.10 | Plan de tests de fumée | `PASS` | `docs/59` — 24 tests |
| 1.11 | Plan de données de recette | `PASS` | `docs/61` |
| 1.12 | Matrice d'accès de recette | `PASS` | `docs/62` — 54 permissions couvertes |
| 1.13 | Checklist d'observabilité | `PASS` | `docs/63` |
| 1.14 | Gestion des anomalies définie | `PASS` | `docs/64` |
| 1.15 | Runbook de déploiement base de données | `PASS` | `docs/57` |
| 1.16 | Script de précontrôle exécutable | `PASS` | `scripts/staging-precheck/` |

**Seize critères, seize `PASS`.** Tout ce qui relève du projet est fait.

---

## 2. Ce qui dépend de l'infrastructure — **bloqué**

| # | Critère | Statut | Détenteur |
|---:|:---|:---|:---|
| 2.1 | Serveur applicatif fourni | `BLOCKED` | Exploitation |
| 2.2 | Runtime .NET 10 installé | `BLOCKED` | Exploitation |
| 2.3 | Instance SQL Server dédiée | `BLOCKED` | Exploitation |
| 2.4 | **Répertoire de stockage documentaire persistant** | `BLOCKED` | Exploitation |
| 2.5 | Certificat TLS et nom de domaine | `BLOCKED` | Exploitation |
| 2.6 | Antivirus opérationnel | `BLOCKED` | Exploitation |
| 2.7 | Flux réseau ouverts | `BLOCKED` | Exploitation |
| 2.8 | Sauvegarde de la base en place | `BLOCKED` | Exploitation |
| 2.9 | Journalisation des requêtes du serveur d'hébergement | `BLOCKED` | Exploitation — `docs/63 § 2.2` |
| 2.10 | `precheck.ps1` exécuté sans échec | `BLOCKED` | Nécessite 2.1 à 2.7 |

---

## 3. Ce qui dépend d'une décision DSI — **bloqué**

| # | Décision | Statut | Référence |
|---:|:---|:---|:---|
| 3.1 | Option de déploiement retenue | `BLOCKED` | `PC-13` — `docs/58` |
| 3.2 | Chaîne de connexion fournie | `BLOCKED` | Secret — `docs/55` |
| 3.3 | Compte administrateur initial défini | `BLOCKED` | Secret — `docs/55` |
| 3.3b | **`Notifications:Reessai` arbitré** | `BLOCKED` | **Empêche le démarrage** — `docs/55 § 1` |
| 3.4 | Durée d'expiration de session arbitrée | `BLOCKED` | Repli technique en place |
| 3.5 | Validité d'invitation arbitrée | `BLOCKED` | Repli technique en place |
| 3.6 | Extensions et taille de fichier validées | `BLOCKED` | Défauts en place |
| 3.7 | **Profil de charge cible** | `BLOCKED` | `PC-01` — bloque NFR-04 |
| 3.8 | Heures ouvrées et fenêtre d'observation | `BLOCKED` | Bloque NFR-03 |
| 3.9 | Autorisation de générer de la charge | `BLOCKED` | Bloque la campagne F016 |
| 3.10 | Rétention des journaux | `BLOCKED` | `docs/63` |

---

## 4. Ce qui restera hors d'atteinte en recette

Ces exigences ne pourront pas être validées, quelle que soit la qualité de l'environnement.

| Domaine | Exigences | Motif | Statut |
|:---|:---|:---|:---|
| Canal e-mail | COM-01 et dépendantes | Aucun fournisseur | `BLOCKED_PROVIDER` — `PC-16` |
| Canal SMS / WhatsApp | ENR-04 | Aucun fournisseur | `BLOCKED_PROVIDER` — `PC-16` |
| Assistant conversationnel | IA-01 à IA-07 | Aucun fournisseur de modèle | `BLOCKED_DSI` — `PC-18` |
| Hébergement souverain | NFR-09 | Décision d'hébergement | `BLOCKED_DSI` — `docs/43` |

**Ces limites doivent être annoncées avant l'ouverture de la campagne**, pas découvertes par un
testeur qui cherchera en vain le bouton d'un assistant qui n'existe pas. Le plan de recette
(`docs/36`) les marque explicitement.

---

## 5. Réserve technique portée à la connaissance de la DSI

| Constat | Effet en recette |
|:---|:---|
| La purge de rétention n'est déclenchée par aucun code applicatif — `GestionnaireRetentionService` est enregistré mais sans appelant | Le scénario de rétention de `docs/61 § 4` ne s'observera pas spontanément |
| La durée des requêtes n'est journalisée nulle part | Une lenteur signalée après coup sera inanalysable sans journal d'hébergement |

Ces deux points sont détaillés dans `docs/67`. **Aucun n'est bloquant pour ouvrir la campagne**,
mais tous deux doivent être connus avant, non découverts pendant.

---

## 6. Verdict

### `STAGING / UAT GO-NO-GO = NO-GO`

**Motif : aucun environnement de recette n'existe.**

Ce `NO-GO` ne porte aucun reproche au produit. Sa cause est unique et externe :

| Origine du blocage | Nombre de critères |
|:---|---:|
| Projet | **0** |
| Infrastructure | 10 |
| Décision DSI | 10 |

**Aucun critère bloquant ne relève du projet.**

### Conditions de levée

| # | Condition | Effet |
|---:|:---|:---|
| 1 | Option de déploiement arbitrée | Débloque 2.1 à 2.2 |
| 2 | Environnement fourni et flux ouverts | Débloque 2.3 à 2.9 |
| 3 | Secrets fournis hors dépôt | Débloque 3.2 et 3.3 |
| 4 | `precheck.ps1` sans échec | Débloque 2.10 |
| 5 | Migrations appliquées — `docs/57` | Prépare le déploiement |
| 6 | Tests de fumée bloquants au vert — `docs/59` | **Autorise l'ouverture de la campagne** |

Les conditions 4 à 6 sont exécutables **le jour même** où l'environnement est livré. Les conditions
1 à 3 n'attendent qu'une décision.

### Ce qui reste possible dès la levée

Les paramètres du § 3.7 à 3.9 ne bloquent que la **certification F016**, pas la campagne
fonctionnelle. La recette de `docs/36` peut donc commencer sans eux — et **trois exigences Must
resteront `NOT EXECUTED`** tant qu'ils ne seront pas fournis.

---

## 7. Statut du document

Cette porte est **ouverte et non franchie**. Elle sera réévaluée à la livraison de l'environnement,
sur preuves, et non sur déclaration.
